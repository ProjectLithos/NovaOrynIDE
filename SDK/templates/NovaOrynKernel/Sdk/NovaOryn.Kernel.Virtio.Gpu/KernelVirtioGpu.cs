using System;
using NovaOryn.Kernel.AddressSpace;
using NovaOryn.Kernel.Drivers;
using NovaOryn.Kernel.Graphics;
using NovaOryn.Kernel.Heap;
using NovaOryn.Kernel.Memory;
using NovaOryn.Kernel.Pci;
using NovaOryn.Kernel.Time;

namespace NovaOryn.Kernel.Virtio.Gpu;

/// <summary>Implements the modern VirtIO GPU 2D command set as NovaOryn's first driver-owned graphics adapter.</summary>
public static unsafe class KernelVirtioGpu
{
    private const UInt16 VirtioVendorId=0x1AF4,ModernGpuDeviceId=0x1050,TransitionalGpuDeviceId=0x1010;
    private const Byte VendorCapabilityId=0x09,CommonConfigurationType=1,NotifyConfigurationType=2,IsrConfigurationType=3,DeviceConfigurationType=4;
    private const UInt64 FeatureVersion1=1UL<<32,SynchronousTimeoutNanoseconds=1000000000UL;
    private const UInt16 DescriptorNext=1,DescriptorWrite=2;
    private const UInt32 CommandGetDisplayInfo=0x0100U,CommandResourceCreate2D=0x0101U,CommandResourceUnref=0x0102U,CommandSetScanout=0x0103U,CommandResourceFlush=0x0104U,CommandTransferToHost2D=0x0105U,CommandResourceAttachBacking=0x0106U;
    private const UInt32 ResponseOkNoData=0x1100U,ResponseOkDisplayInfo=0x1101U,FormatB8G8R8X8Unorm=2U;
    private const UInt32 MaximumDimension=8192U;

    private struct QueueRecord { internal UInt16 Index,Size,LastUsed,Ready;internal UInt32 NotifyOffset;internal UInt64 AllocationToken,AllocationPages,PhysicalBase,VirtualBase,AvailableOffset,UsedOffset; }
    private struct DeviceRecord
    {
        internal Byte Used,Started;internal UInt16 Segment;internal Byte Bus,PciDevice,Function;internal UInt32 DeviceHandle,Scanout,ResourceId,Width,Height,Pitch;internal UInt64 Common,Notify,Isr,DeviceConfig,NotifyMultiplier,DeviceFeatures,NegotiatedFeatures;internal QueueRecord Control;
        internal UInt64 FrameToken,FramePages,FramePhysical,FrameVirtual,FrameBytes;internal UInt32 GraphicsDisplay;
    }
    private static DeviceRecord* _devices;private static KernelHeapAllocation _allocation;private static UInt32 _capacity,_count,_displayCount;private static Boolean _initialized;private static KernelDriverHandle _driver;

    /// <summary>Registers the VirtIO GPU PCI driver, binds GPU device type 16, and initializes scan-out resources.</summary>
    public static Boolean Initialize()
    {
        if(_initialized)return true;if(!KernelPci.IsInitialized()||!KernelDrivers.IsInitialized()||!KernelHeap.IsInitialized()||!KernelGraphics.IsInitialized())return false;
        if(!AllocateRecords(4U,out _allocation,out _devices))return false;_capacity=4U;
        KernelDriverMatchRule rule=new(KernelDeviceBus.Pci,true,VirtioVendorId,true,0,false,0U,0U);KernelDriverCallbacks callbacks=new(&Probe,&Start,&Stop,&Remove,&Interrupt);KernelDriverCapabilityDeclaration declaration=new(KernelDriverCapability.Mmio|KernelDriverCapability.Dma|KernelDriverCapability.PciConfig);if(!KernelDrivers.RegisterDriver("VirtIO GPU",rule,callbacks,declaration,out _driver))return false;
        UInt32 pciCount=KernelPci.GetDeviceCount();for(UInt32 i=0;i<pciCount;i++){if(!KernelPci.TryGetDevice(i,out PciDeviceInfo pci)||!IsGpu(pci))continue;if(KernelDrivers.TryGetDevice(pci.DeviceHandle,out _,out _,out KernelDriverHandle bound)&&bound.Value!=0U)continue;if(KernelDrivers.TryBindDevice(pci.DeviceHandle,out KernelDriverHandle driver)&&driver.Value==_driver.Value)KernelDrivers.StartDevice(pci.DeviceHandle);}
        _initialized=true;return true;
    }
    /// <summary>Gets whether the VirtIO GPU driver family is installed.</summary>
    public static Boolean IsInitialized()=>_initialized;
    /// <summary>Reports current VirtIO GPU controller and display counts.</summary>
    public static VirtioGpuCapabilities GetCapabilities()=>new(_initialized,_count,_displayCount,true,true);
    /// <summary>Gets one started VirtIO GPU controller by zero-based index.</summary>
    public static Boolean TryGetController(UInt32 index,out VirtioGpuInfo info){info=default;if(index>=_count)return false;UInt32 found=0;for(UInt32 i=0;i<_capacity;i++){DeviceRecord* r=_devices+i;if(r->Used==0)continue;if(found++==index){info=Info(r);return true;}}return false;}
    /// <summary>Presents a rectangle for a specific VirtIO GPU device.</summary>
    public static Boolean Present(KernelDeviceHandle device,UInt32 x,UInt32 y,UInt32 width,UInt32 height){if(!TryRecord(device,out DeviceRecord* r)||r->Started==0)return false;return PresentRecord(r,x,y,width,height);}
    /// <summary>Changes the 2D resource and scan-out dimensions for a specific VirtIO GPU device.</summary>
    public static Boolean SetMode(KernelDeviceHandle device,UInt32 width,UInt32 height){if(!TryRecord(device,out DeviceRecord* r)||r->Started==0)return false;return ChangeMode(r,width,height);}

    private static Boolean Probe(KernelDriverDeviceContext* context){if(context==null||context->Identifier.VendorId!=VirtioVendorId||!KernelPci.TryGetDevice(context->Device,out PciDeviceInfo pci)||!IsGpu(pci))return false;return TryFindTransportCapability(pci.Location,CommonConfigurationType,out _);}
    private static Boolean Start(KernelDriverDeviceContext* context)
    {
        if(context==null||!KernelPci.TryGetDevice(context->Device,out PciDeviceInfo pci)||!IsGpu(pci)||!EnablePci(pci.Location))return false;Int32 slot=Free();if(slot<0){if(!Grow())return false;slot=Free();if(slot<0)return false;}DeviceRecord* r=_devices+slot;Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));r->Used=1;r->DeviceHandle=context->Device.Value;r->Segment=pci.Location.Segment;r->Bus=pci.Location.Bus;r->PciDevice=pci.Location.Device;r->Function=pci.Location.Function;r->ResourceId=(UInt32)slot+1U;
        if(!InitializeTransport(r,pci.Location)||!SetupQueue(r,&r->Control,0,64)){ReleaseResources(r);Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));return false;}
        if(!GetPreferredMode(r,out UInt32 width,out UInt32 height,out UInt32 scanout)){SetStatus(r,0x80);ReleaseResources(r);Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));return false;}r->Scanout=scanout;
        if(!CreateScanout(r,width,height)){SetStatus(r,0x80);ReleaseResources(r);Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));return false;}
        SetStatus(r,(Byte)(Read8(r->Common+20)|4U));r->Started=1;
        KernelGraphicsMode mode=new(r->Width,r->Height,r->Pitch,KernelGraphicsPixelFormat.BlueGreenRedReserved8);KernelGraphicsFramebuffer framebuffer=new(r->FramePhysical,r->FrameVirtual,r->FrameBytes,mode);KernelGraphicsCallbacks graphicsCallbacks=new(&GraphicsPresent,&GraphicsSetMode);if(!KernelGraphics.RegisterDisplay(context->Device,KernelGraphicsTargetKind.VirtioGpu,framebuffer,graphicsCallbacks,true,false,out KernelGraphicsDisplayHandle display)){SetStatus(r,0);ReleaseResources(r);Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));return false;}r->GraphicsDisplay=display.Value;_count++;_displayCount++;return true;
    }
    private static Boolean Stop(KernelDriverDeviceContext* context){if(context==null||!TryRecord(context->Device,out DeviceRecord* r))return false;SetStatus(r,0);r->Started=0;return true;}
    private static Boolean Remove(KernelDriverDeviceContext* context){if(context==null||!TryRecord(context->Device,out DeviceRecord* r))return false;ReleaseResources(r);Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));if(_count!=0)_count--;if(_displayCount!=0)_displayCount--;return true;}
    private static Boolean Interrupt(KernelDriverDeviceContext* context,UInt64 cookie)=>context!=null&&TryRecord(context->Device,out _);

    private static Boolean GraphicsPresent(KernelGraphicsDisplayHandle display,UInt32 x,UInt32 y,UInt32 width,UInt32 height){if(!TryDisplay(display,out DeviceRecord* r)||r->Started==0)return false;return PresentRecord(r,x,y,width,height);}
    private static Boolean GraphicsSetMode(KernelGraphicsDisplayHandle display,KernelGraphicsMode mode){if(!TryDisplay(display,out DeviceRecord* r)||r->Started==0||mode.PixelFormat!=KernelGraphicsPixelFormat.BlueGreenRedReserved8||mode.PixelsPerScanLine!=mode.Width)return false;return ChangeMode(r,mode.Width,mode.Height);}

    private static Boolean ChangeMode(DeviceRecord* r,UInt32 width,UInt32 height)
    {
        if(width==0U||height==0U||width>MaximumDimension||height>MaximumDimension)return false;if(width>UInt64.MaxValue/4UL/height)return false;
        UInt64 oldToken=r->FrameToken,oldPages=r->FramePages,oldPhysical=r->FramePhysical;UInt32 oldWidth=r->Width,oldHeight=r->Height,oldPitch=r->Pitch,oldResource=r->ResourceId;UInt64 oldVirtual=r->FrameVirtual,oldBytes=r->FrameBytes;
        UInt32 nextResource=oldResource+0x10000U;if(nextResource==0U)nextResource=oldResource+1U;r->ResourceId=nextResource;r->FrameToken=0;r->FramePages=0;r->FramePhysical=0;r->FrameVirtual=0;r->FrameBytes=0;
        if(!CreateScanout(r,width,height)){r->ResourceId=oldResource;r->FrameToken=oldToken;r->FramePages=oldPages;r->FramePhysical=oldPhysical;r->FrameVirtual=oldVirtual;r->FrameBytes=oldBytes;r->Width=oldWidth;r->Height=oldHeight;r->Pitch=oldPitch;return false;}
        Boolean cleanup=UnrefResourceId(r,oldResource);if(oldToken!=0)cleanup=ReleaseDma(oldToken,oldPhysical,oldPages)&cleanup;KernelGraphicsMode mode=new(r->Width,r->Height,r->Pitch,KernelGraphicsPixelFormat.BlueGreenRedReserved8);KernelGraphicsFramebuffer fb=new(r->FramePhysical,r->FrameVirtual,r->FrameBytes,mode);return KernelGraphics.UpdateFramebuffer(new KernelGraphicsDisplayHandle(r->GraphicsDisplay),fb)&&cleanup;
    }

    private static Boolean CreateScanout(DeviceRecord* r,UInt32 width,UInt32 height)
    {
        UInt64 bytes=(UInt64)width*height*4UL;if(!AllocateDma(bytes,out r->FrameToken,out r->FramePages,out r->FramePhysical,out r->FrameVirtual))return false;r->FrameBytes=bytes;r->Width=width;r->Height=height;r->Pitch=width;Clear((Byte*)(nuint)r->FrameVirtual,r->FramePages*4096UL);
        if(!Create2D(r)||!AttachBacking(r)||!SetScanout(r)){ReleaseDma(r->FrameToken,r->FramePhysical,r->FramePages);r->FrameToken=0;return false;}return PresentRecord(r,0,0,width,height);
    }
    private static Boolean PresentRecord(DeviceRecord* r,UInt32 x,UInt32 y,UInt32 width,UInt32 height){if(width==0U||height==0U||x>=r->Width||y>=r->Height||width>r->Width-x||height>r->Height-y)return false;return TransferToHost(r,x,y,width,height)&&Flush(r,x,y,width,height);}

    private static Boolean GetPreferredMode(DeviceRecord* r,out UInt32 width,out UInt32 height,out UInt32 scanout)
    {
        width=0;height=0;scanout=0;Byte* request=stackalloc Byte[24];Byte* response=stackalloc Byte[408];Clear(request,24);Clear(response,408);Write32((UInt64)(nuint)request,CommandGetDisplayInfo);UInt32 responseType;if(!Command(r,request,24,response,408,out responseType)||responseType!=ResponseOkDisplayInfo)return false;
        for(UInt32 i=0;i<16U;i++){UInt64 p=(UInt64)(nuint)response+24UL+(UInt64)i*24UL;UInt32 w=Read32(p+8),h=Read32(p+12),enabled=Read32(p+16);if(enabled==0U||w==0U||h==0U)continue;width=w;height=h;scanout=i;return true;}return false;
    }
    private static Boolean Create2D(DeviceRecord* r){Byte* request=stackalloc Byte[40];Byte* response=stackalloc Byte[24];Clear(request,40);Clear(response,24);Write32((UInt64)(nuint)request,CommandResourceCreate2D);Write32((UInt64)(nuint)request+24,r->ResourceId);Write32((UInt64)(nuint)request+28,FormatB8G8R8X8Unorm);Write32((UInt64)(nuint)request+32,r->Width);Write32((UInt64)(nuint)request+36,r->Height);return CommandOk(r,request,40,response,24);}
    private static Boolean AttachBacking(DeviceRecord* r){Byte* request=stackalloc Byte[48];Byte* response=stackalloc Byte[24];Clear(request,48);Clear(response,24);Write32((UInt64)(nuint)request,CommandResourceAttachBacking);Write32((UInt64)(nuint)request+24,r->ResourceId);Write32((UInt64)(nuint)request+28,1U);Write64((UInt64)(nuint)request+32,r->FramePhysical);Write32((UInt64)(nuint)request+40,(UInt32)r->FrameBytes);return CommandOk(r,request,48,response,24);}
    private static Boolean SetScanout(DeviceRecord* r){Byte* request=stackalloc Byte[48];Byte* response=stackalloc Byte[24];Clear(request,48);Clear(response,24);Write32((UInt64)(nuint)request,CommandSetScanout);Write32((UInt64)(nuint)request+32,r->Width);Write32((UInt64)(nuint)request+36,r->Height);Write32((UInt64)(nuint)request+40,r->Scanout);Write32((UInt64)(nuint)request+44,r->ResourceId);return CommandOk(r,request,48,response,24);}
    private static Boolean TransferToHost(DeviceRecord* r,UInt32 x,UInt32 y,UInt32 width,UInt32 height){Byte* request=stackalloc Byte[56];Byte* response=stackalloc Byte[24];Clear(request,56);Clear(response,24);Write32((UInt64)(nuint)request,CommandTransferToHost2D);Write32((UInt64)(nuint)request+24,x);Write32((UInt64)(nuint)request+28,y);Write32((UInt64)(nuint)request+32,width);Write32((UInt64)(nuint)request+36,height);Write64((UInt64)(nuint)request+40,((UInt64)y*r->Pitch+x)*4UL);Write32((UInt64)(nuint)request+48,r->ResourceId);return CommandOk(r,request,56,response,24);}
    private static Boolean Flush(DeviceRecord* r,UInt32 x,UInt32 y,UInt32 width,UInt32 height){Byte* request=stackalloc Byte[48];Byte* response=stackalloc Byte[24];Clear(request,48);Clear(response,24);Write32((UInt64)(nuint)request,CommandResourceFlush);Write32((UInt64)(nuint)request+24,x);Write32((UInt64)(nuint)request+28,y);Write32((UInt64)(nuint)request+32,width);Write32((UInt64)(nuint)request+36,height);Write32((UInt64)(nuint)request+40,r->ResourceId);return CommandOk(r,request,48,response,24);}
    private static Boolean UnrefResourceId(DeviceRecord* r,UInt32 resourceId){Byte* request=stackalloc Byte[32];Byte* response=stackalloc Byte[24];Clear(request,32);Clear(response,24);Write32((UInt64)(nuint)request,CommandResourceUnref);Write32((UInt64)(nuint)request+24,resourceId);return CommandOk(r,request,32,response,24);}
    private static Boolean CommandOk(DeviceRecord* r,Byte* request,UInt32 requestBytes,Byte* response,UInt32 responseBytes){UInt32 type;return Command(r,request,requestBytes,response,responseBytes,out type)&&type==ResponseOkNoData;}
    private static Boolean Command(DeviceRecord* r,Byte* request,UInt32 requestBytes,Byte* response,UInt32 responseBytes,out UInt32 responseType)
    {
        responseType=0;UInt64 total=(UInt64)requestBytes+responseBytes;if(!AllocateDma(total,out UInt64 token,out UInt64 pages,out UInt64 physical,out UInt64 virtualAddress))return false;Byte* dma=(Byte*)(nuint)virtualAddress;Copy(request,dma,requestBytes);Clear(dma+requestBytes,responseBytes);SetDescriptor(&r->Control,0,physical,requestBytes,DescriptorNext,1);SetDescriptor(&r->Control,1,physical+requestBytes,responseBytes,DescriptorWrite,0);Boolean ok=SubmitAndWait(r,&r->Control,0,out UInt32 used)&&used<=responseBytes;if(ok){Copy(dma+requestBytes,response,responseBytes);responseType=Read32((UInt64)(nuint)response);}ReleaseDma(token,physical,pages);return ok;
    }

    private static Boolean InitializeTransport(DeviceRecord* r,PciLocation location)
    {
        if(!MapTransportCapability(location,CommonConfigurationType,out r->Common,out _)||!MapTransportCapability(location,NotifyConfigurationType,out r->Notify,out UInt16 notifyCap))return false;MapTransportCapability(location,IsrConfigurationType,out r->Isr,out _);MapTransportCapability(location,DeviceConfigurationType,out r->DeviceConfig,out _);if(!KernelPci.TryRead32(location,(UInt16)(notifyCap+16U),out UInt32 multiplier)||multiplier==0U)return false;r->NotifyMultiplier=multiplier;
        Write8(r->Common+20,0);Write16(r->Common+16,0xFFFF);Write8(r->Common+20,1);Write8(r->Common+20,3);Write32(r->Common,0);UInt64 features=Read32(r->Common+4);Write32(r->Common,1);features|=(UInt64)Read32(r->Common+4)<<32;r->DeviceFeatures=features;if((features&FeatureVersion1)==0UL)return false;r->NegotiatedFeatures=features&FeatureVersion1;Write32(r->Common+8,0);Write32(r->Common+12,(UInt32)r->NegotiatedFeatures);Write32(r->Common+8,1);Write32(r->Common+12,(UInt32)(r->NegotiatedFeatures>>32));Write8(r->Common+20,(Byte)(Read8(r->Common+20)|8U));return (Read8(r->Common+20)&8U)!=0;
    }
    private static Boolean SetupQueue(DeviceRecord* r,QueueRecord* q,UInt16 queueIndex,UInt16 requested)
    {
        Write16(r->Common+22,queueIndex);UInt16 maximum=Read16(r->Common+24);UInt16 size=SelectQueueSize(maximum,requested);if(size==0U)return false;Write16(r->Common+24,size);UInt16 notifyOffset=Read16(r->Common+30);UInt64 descriptors=(UInt64)size*16UL,available=6UL+(UInt64)size*2UL,usedOffset=(descriptors+available+3UL)&~3UL,total=usedOffset+6UL+(UInt64)size*8UL;if(!AllocateDma(total,out q->AllocationToken,out q->AllocationPages,out q->PhysicalBase,out q->VirtualBase))return false;q->Index=queueIndex;q->Size=size;q->AvailableOffset=descriptors;q->UsedOffset=usedOffset;q->NotifyOffset=notifyOffset;Write64(r->Common+32,q->PhysicalBase);Write64(r->Common+40,q->PhysicalBase+q->AvailableOffset);Write64(r->Common+48,q->PhysicalBase+q->UsedOffset);Write16(r->Common+28,1);q->Ready=1;return true;
    }
    private static UInt16 SelectQueueSize(UInt16 maximum,UInt16 requested){UInt16 limit=maximum<requested?maximum:requested;if(limit==0U)return 0;UInt16 result=1;while(result<=limit/2U)result=(UInt16)(result*2U);return result;}
    private static Boolean SubmitAndWait(DeviceRecord* r,QueueRecord* q,UInt16 head,out UInt32 length){length=0;if(!Submit(r,q,head)||!KernelTime.TryCreateDeadline(SynchronousTimeoutNanoseconds,out UInt64 deadline))return false;while(!TryConsumeUsed(q,out _,out length)){if(KernelTime.HasReached(deadline))return false;}return true;}
    private static Boolean Submit(DeviceRecord* r,QueueRecord* q,UInt16 head){if(q->Ready==0||head>=q->Size)return false;Byte* available=(Byte*)(nuint)(q->VirtualBase+q->AvailableOffset);UInt16 index=Read16((UInt64)(nuint)(available+2));*(UInt16*)(available+4+(UInt64)(index%q->Size)*2UL)=head;Write16((UInt64)(nuint)(available+2),(UInt16)(index+1));Write16(r->Notify+(UInt64)q->NotifyOffset*r->NotifyMultiplier,q->Index);return true;}
    private static Boolean TryConsumeUsed(QueueRecord* q,out UInt32 id,out UInt32 length){id=0;length=0;if(q->Ready==0)return false;Byte* used=(Byte*)(nuint)(q->VirtualBase+q->UsedOffset);UInt16 current=Read16((UInt64)(nuint)(used+2));if(q->LastUsed==current)return false;UInt64 element=4UL+(UInt64)(q->LastUsed%q->Size)*8UL;id=Read32((UInt64)(nuint)(used+element));length=Read32((UInt64)(nuint)(used+element+4));q->LastUsed++;return true;}
    private static Boolean SetDescriptor(QueueRecord* q,UInt16 index,UInt64 address,UInt32 length,UInt16 flags,UInt16 next){if(q->Ready==0||index>=q->Size)return false;UInt64 d=q->VirtualBase+(UInt64)index*16UL;Write64(d,address);Write32(d+8,length);Write16(d+12,flags);Write16(d+14,next);return true;}

    private static Boolean EnablePci(PciLocation location)
    {
        if(!KernelPci.TryRead16(location,0x04,out UInt16 command))return false;
        // VirtIO BAR access needs PCI Memory Space and virtqueue DMA needs Bus Master.
        return KernelPci.TryWrite16(location,0x04,(UInt16)(command|0x0006U));
    }

    private static Boolean IsGpu(PciDeviceInfo pci)=>pci.VendorId==VirtioVendorId&&(pci.DeviceId==ModernGpuDeviceId||(pci.DeviceId==TransitionalGpuDeviceId&&pci.SubsystemId==16U));
    private static Boolean MapTransportCapability(PciLocation location,Byte configurationType,out UInt64 virtualAddress,out UInt16 capabilityOffset){virtualAddress=0;capabilityOffset=0;if(!TryFindTransportCapability(location,configurationType,out PciCapabilityInfo capability))return false;if(!KernelPci.TryRead8(location,(UInt16)(capability.Offset+2),out Byte capabilityLength)||capabilityLength<(configurationType==NotifyConfigurationType?20U:16U))return false;if(!KernelPci.TryRead8(location,(UInt16)(capability.Offset+4),out Byte barIndex)||!KernelPci.TryRead32(location,(UInt16)(capability.Offset+8),out UInt32 offset)||!KernelPci.TryRead32(location,(UInt16)(capability.Offset+12),out UInt32 length)||length==0U)return false;if(!KernelPci.TryGetBar(location,barIndex,out PciBarInfo bar)||bar.Type==PciBarType.Io||offset>bar.Length||length>bar.Length-offset)return false;if(!KernelPci.TryMapMmio(bar.Address+offset,length,out virtualAddress))return false;capabilityOffset=capability.Offset;return true;}
    private static Boolean TryFindTransportCapability(PciLocation location,Byte configurationType,out PciCapabilityInfo found){found=default;for(UInt32 i=0;i<48U;i++){if(!KernelPci.TryGetCapability(location,i,out PciCapabilityInfo capability))return false;if(capability.Id!=VendorCapabilityId)continue;if(KernelPci.TryRead8(location,(UInt16)(capability.Offset+3),out Byte type)&&type==configurationType){found=capability;return true;}}return false;}
    private static Boolean AllocateDma(UInt64 bytes,out UInt64 token,out UInt64 pages,out UInt64 physical,out UInt64 virtualAddress){token=0;pages=0;physical=0;virtualAddress=0;if(bytes==0||bytes>UInt64.MaxValue-4095UL)return false;pages=(bytes+4095UL)/4096UL;if(!KernelPhysicalMemory.TryAllocate(pages,1,out KernelPhysicalAllocation allocation))return false;if(!KernelAddressSpace.TryPhysicalToDirectMap(allocation.StartAddress,out virtualAddress)){KernelPhysicalMemory.TryRelease(allocation);return false;}token=allocation.Token;physical=allocation.StartAddress;Clear((Byte*)(nuint)virtualAddress,pages*4096UL);return true;}
    private static Boolean ReleaseDma(UInt64 token,UInt64 physical,UInt64 pages)=>token==0?true:KernelPhysicalMemory.TryRelease(new KernelPhysicalAllocation(token,physical,pages));
    private static Boolean ReleaseResources(DeviceRecord* r){Boolean ok=true;if(r->Control.AllocationToken!=0)ok=ReleaseDma(r->Control.AllocationToken,r->Control.PhysicalBase,r->Control.AllocationPages)&ok;if(r->FrameToken!=0)ok=ReleaseDma(r->FrameToken,r->FramePhysical,r->FramePages)&ok;return ok;}
    private static Boolean SetStatus(DeviceRecord* r,Byte status){if(r->Common==0)return false;Write8(r->Common+20,status);return true;}
    private static VirtioGpuInfo Info(DeviceRecord* r)=>new(new KernelDeviceHandle(r->DeviceHandle),new KernelGraphicsDisplayHandle(r->GraphicsDisplay),new KernelGraphicsMode(r->Width,r->Height,r->Pitch,KernelGraphicsPixelFormat.BlueGreenRedReserved8),r->Scanout,r->Started!=0);
    private static Boolean TryRecord(KernelDeviceHandle device,out DeviceRecord* record){record=null;if(device.Value==0||_devices==null)return false;for(UInt32 i=0;i<_capacity;i++){DeviceRecord* r=_devices+i;if(r->Used!=0&&r->DeviceHandle==device.Value){record=r;return true;}}return false;}
    private static Boolean TryDisplay(KernelGraphicsDisplayHandle display,out DeviceRecord* record){record=null;if(display.Value==0||_devices==null)return false;for(UInt32 i=0;i<_capacity;i++){DeviceRecord* r=_devices+i;if(r->Used!=0&&r->GraphicsDisplay==display.Value){record=r;return true;}}return false;}
    private static Int32 Free(){for(Int32 i=0;i<(Int32)_capacity;i++)if((_devices+i)->Used==0)return i;return -1;}
    private static Boolean AllocateRecords(UInt32 capacity,out KernelHeapAllocation allocation,out DeviceRecord* pointer){allocation=default;pointer=null;if(!KernelHeap.TryAllocate((UInt64)capacity*(UInt64)sizeof(DeviceRecord),64U,true,out allocation))return false;pointer=(DeviceRecord*)(nuint)allocation.Address;return true;}
    private static Boolean Grow(){UInt32 next=_capacity>=0x40000000U?UInt32.MaxValue:_capacity*2U;if(next<=_capacity||next>Int32.MaxValue||!AllocateRecords(next,out KernelHeapAllocation fresh,out DeviceRecord* p))return false;Copy((Byte*)_devices,(Byte*)p,(UInt64)_capacity*(UInt64)sizeof(DeviceRecord));KernelHeapAllocation old=_allocation;_allocation=fresh;_devices=p;_capacity=next;return KernelHeap.TryRelease(old);}
    private static Byte Read8(UInt64 address)=>*(Byte*)(nuint)address;private static UInt16 Read16(UInt64 address)=>*(UInt16*)(nuint)address;private static UInt32 Read32(UInt64 address)=>*(UInt32*)(nuint)address;
    private static Boolean Write8(UInt64 address,Byte value){*(Byte*)(nuint)address=value;return true;}private static Boolean Write16(UInt64 address,UInt16 value){*(UInt16*)(nuint)address=value;return true;}private static Boolean Write32(UInt64 address,UInt32 value){*(UInt32*)(nuint)address=value;return true;}private static Boolean Write64(UInt64 address,UInt64 value){*(UInt64*)(nuint)address=value;return true;}
    private static Boolean Copy(Byte* source,Byte* destination,UInt64 bytes){for(UInt64 i=0;i<bytes;i++)destination[i]=source[i];return true;}private static Boolean Clear(Byte* destination,UInt64 bytes){for(UInt64 i=0;i<bytes;i++)destination[i]=0;return true;}
}
