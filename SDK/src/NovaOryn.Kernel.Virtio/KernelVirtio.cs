using System;
using NovaOryn.Kernel.AddressSpace;
using NovaOryn.Kernel.Drivers;
using NovaOryn.Kernel.Heap;
using NovaOryn.Kernel.Memory;
using NovaOryn.Kernel.Networking;
using NovaOryn.Kernel.Pci;
using NovaOryn.Kernel.Storage;
using NovaOryn.Kernel.Time;

namespace NovaOryn.Kernel.Virtio;

/// <summary>Provides the modern VirtIO PCI transport and built-in block, network, console, and entropy-source drivers.</summary>
public static unsafe class KernelVirtio
{
    private const UInt16 VirtioVendorId=0x1AF4;
    private const Byte VendorCapabilityId=0x09;
    private const Byte CommonConfigurationType=1,NotifyConfigurationType=2,IsrConfigurationType=3,DeviceConfigurationType=4;
    private const UInt64 FeatureVersion1=1UL<<32;
    private const UInt64 BlockFeatureReadOnly=1UL<<5,BlockFeatureBlockSize=1UL<<6,BlockFeatureFlush=1UL<<9;
    private const UInt64 NetworkFeatureMtu=1UL<<3,NetworkFeatureMac=1UL<<5,NetworkFeatureStatus=1UL<<16;
    private const UInt16 DescriptorNext=1,DescriptorWrite=2;
    private const UInt32 BlockRequestIn=0,BlockRequestOut=1,BlockRequestFlush=4;
    private const UInt64 SynchronousTimeoutNanoseconds=2000000000UL;
    private const UInt32 VirtioNetworkHeaderBytes=10U;

    private struct QueueRecord
    {
        internal Byte Ready;internal UInt16 Index,Size,LastUsed;internal UInt64 NotifyOffset,PhysicalBase,VirtualBase,DescriptorOffset,AvailableOffset,UsedOffset,AllocationToken,AllocationPages;
    }
    private struct DeviceRecord
    {
        internal Byte Used,Started,Type,ReceiveEnabled;internal UInt32 DeviceHandle;internal UInt16 Segment;internal Byte Bus,PciDevice,Function;internal UInt64 Common,Notify,Isr,DeviceConfig,NotifyMultiplier,DeviceFeatures,NegotiatedFeatures;
        internal UInt16 QueueCount;internal QueueRecord Queue0,Queue1;internal UInt32 StorageHandle,NetworkHandle,BlockSize,Mtu;internal UInt64 BlockCount,RxToken,RxPages,RxPhysical,RxVirtual;internal UInt32 RxBytes;
    }
    private static DeviceRecord* _devices;private static KernelHeapAllocation _deviceAllocation;private static UInt32 _capacity,_count,_blockCount,_networkCount,_consoleCount,_rngCount;private static Boolean _initialized;private static KernelDriverHandle _driverHandle;

    /// <summary>Installs the VirtIO PCI driver family, binds supported discovered PCI functions, and starts their transport-specific drivers.</summary>
    public static Boolean Initialize()
    {
        if(_initialized)return true;if(!KernelPci.IsInitialized()||!KernelDrivers.IsInitialized()||!KernelHeap.IsInitialized()||!KernelStorage.IsInitialized()||!KernelNetworking.IsInitialized())return false;
        if(!AllocateRecords(16U,out _deviceAllocation,out _devices))return false;_capacity=16U;_count=0U;_blockCount=0U;_networkCount=0U;_consoleCount=0U;_rngCount=0U;
        KernelDriverMatchRule rule=new(KernelDeviceBus.Pci,true,VirtioVendorId,true,0,false,0U,0U);KernelDriverCallbacks callbacks=new(&Probe,&Start,&Stop,&Remove,&Interrupt);KernelDriverCapabilityDeclaration declaration=new(KernelDriverCapability.Mmio|KernelDriverCapability.Dma|KernelDriverCapability.PciConfig|KernelDriverCapability.Networking|KernelDriverCapability.Filesystem);if(!KernelDrivers.RegisterDriver("VirtIO PCI",rule,callbacks,declaration,out _driverHandle))return false;
        UInt32 pciCount=KernelPci.GetDeviceCount();for(UInt32 i=0;i<pciCount;i++)
        {
            if(!KernelPci.TryGetDevice(i,out PciDeviceInfo pci)||pci.VendorId!=VirtioVendorId)continue;
            if(KernelDrivers.TryGetDevice(pci.DeviceHandle,out _,out _,out KernelDriverHandle bound)&&bound.Value!=0U)continue;
            if(KernelDrivers.TryBindDevice(pci.DeviceHandle,out KernelDriverHandle driver)&&driver.Value==_driverHandle.Value)KernelDrivers.StartDevice(pci.DeviceHandle);
        }
        _initialized=true;return true;
    }

    /// <summary>Gets whether the VirtIO driver family was installed.</summary>
    public static Boolean IsInitialized()=>_initialized;
    /// <summary>Gets started VirtIO device counts by built-in driver type.</summary>
    public static VirtioCapabilities GetCapabilities()=>new(_initialized,_count,_blockCount,_networkCount,_consoleCount,_rngCount);
    /// <summary>Gets the number of started VirtIO devices.</summary>
    public static UInt32 GetDeviceCount()=>_count;

    /// <summary>Gets one started VirtIO device by zero-based discovery index.</summary>
    public static Boolean TryGetDevice(UInt32 index,out VirtioDeviceInfo info)
    {info=default;if(index>=_count)return false;UInt32 found=0;for(UInt32 i=0;i<_capacity;i++){DeviceRecord* r=_devices+i;if(r->Used==0)continue;if(found++==index){info=Info(r);return true;}}return false;}

    /// <summary>Gets VirtIO metadata for a generic device handle.</summary>
    public static Boolean TryGetDevice(KernelDeviceHandle device,out VirtioDeviceInfo info)
    {info=default;if(!TryRecord(device,out DeviceRecord* r))return false;info=Info(r);return true;}

    /// <summary>Services all started VirtIO network devices for receive-side work when interrupt delivery is not installed.</summary>
    public static Boolean ServiceAll()
    {if(!_initialized)return false;Boolean ok=true;for(UInt32 i=0;i<_capacity;i++){DeviceRecord* r=_devices+i;if(r->Used!=0&&r->Started!=0&&(VirtioDeviceType)r->Type==VirtioDeviceType.Network)ok=ServiceNetwork(r)&ok;}return ok;}

    /// <summary>Services one VirtIO device for receive-side work. This also serves systems that have not yet installed MSI/MSI-X delivery.</summary>
    public static Boolean Service(KernelDeviceHandle device)
    {if(!TryRecord(device,out DeviceRecord* r)||r->Started==0)return false;if((VirtioDeviceType)r->Type==VirtioDeviceType.Network)return ServiceNetwork(r);return true;}

    /// <summary>Writes bytes synchronously to a started VirtIO console transmit queue.</summary>
    public static Boolean WriteConsole(KernelDeviceHandle device,Byte* buffer,UInt32 length)
    {if(buffer==null||length==0||!TryRecord(device,out DeviceRecord* r)||(VirtioDeviceType)r->Type!=VirtioDeviceType.Console||r->Started==0)return false;return TransferSimple(r,&r->Queue1,buffer,length,false);}

    /// <summary>Reads bytes synchronously from a started VirtIO console receive queue.</summary>
    public static Boolean ReadConsole(KernelDeviceHandle device,Byte* buffer,UInt32 capacity,out UInt32 bytesRead)
    {bytesRead=0;if(buffer==null||capacity==0||!TryRecord(device,out DeviceRecord* r)||(VirtioDeviceType)r->Type!=VirtioDeviceType.Console||r->Started==0)return false;return TransferSimpleRead(r,&r->Queue0,buffer,capacity,out bytesRead);}

    /// <summary>Obtains entropy bytes synchronously from a started VirtIO RNG device.</summary>
    public static Boolean FillRandom(KernelDeviceHandle device,Byte* buffer,UInt32 length)
    {if(buffer==null||length==0||!TryRecord(device,out DeviceRecord* r)||(VirtioDeviceType)r->Type!=VirtioDeviceType.EntropySource||r->Started==0)return false;UInt32 read;return TransferSimpleRead(r,&r->Queue0,buffer,length,out read)&&read==length;}

    private static Boolean Probe(KernelDriverDeviceContext* context)
    {if(context==null||context->Identifier.VendorId!=VirtioVendorId||!KernelPci.TryGetDevice(context->Device,out PciDeviceInfo pci))return false;VirtioDeviceType type=VirtioMath.IdentifyDeviceType(pci.DeviceId,pci.SubsystemId);return type>=VirtioDeviceType.Network&&type<=VirtioDeviceType.EntropySource&&TryFindTransportCapability(pci.Location,CommonConfigurationType,out _);}

    private static Boolean Start(KernelDriverDeviceContext* context)
    {
        if(context==null||!KernelPci.TryGetDevice(context->Device,out PciDeviceInfo pci)||!EnablePci(pci.Location))return false;VirtioDeviceType type=VirtioMath.IdentifyDeviceType(pci.DeviceId,pci.SubsystemId);if(type==VirtioDeviceType.Unknown)return false;
        Int32 slot=FreeRecord();if(slot<0){if(!GrowRecords())return false;slot=FreeRecord();if(slot<0)return false;}DeviceRecord* r=_devices+slot;Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));r->Used=1;r->Type=(Byte)type;r->DeviceHandle=context->Device.Value;r->Segment=pci.Location.Segment;r->Bus=pci.Location.Bus;r->PciDevice=pci.Location.Device;r->Function=pci.Location.Function;
        if(!InitializeTransport(r,pci.Location,type)){Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));return false;}
        Boolean started=type==VirtioDeviceType.Block?InitializeBlock(r):type==VirtioDeviceType.Network?InitializeNetwork(r):type==VirtioDeviceType.Console?InitializeConsole(r):InitializeEntropy(r);
        if(!started){SetStatus(r,VirtioDeviceStatus.Failed);ReleaseRecordResources(r);Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));return false;}SetStatus(r,(VirtioDeviceStatus)(Read8(r->Common+20)|((Byte)VirtioDeviceStatus.DriverOk)));r->Started=1;r->QueueCount=(UInt16)((r->Queue0.Ready!=0?1:0)+(r->Queue1.Ready!=0?1:0));_count++;if(type==VirtioDeviceType.Block)_blockCount++;else if(type==VirtioDeviceType.Network)_networkCount++;else if(type==VirtioDeviceType.Console)_consoleCount++;else _rngCount++;return true;
    }

    private static Boolean Stop(KernelDriverDeviceContext* context)
    {if(context==null||!TryRecord(context->Device,out DeviceRecord* r))return false;SetStatus(r,VirtioDeviceStatus.Reset);r->Started=0;return true;}
    private static Boolean Remove(KernelDriverDeviceContext* context)
    {if(context==null||!TryRecord(context->Device,out DeviceRecord* r))return false;VirtioDeviceType type=(VirtioDeviceType)r->Type;ReleaseRecordResources(r);Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));if(_count!=0)_count--;if(type==VirtioDeviceType.Block&&_blockCount!=0)_blockCount--;else if(type==VirtioDeviceType.Network&&_networkCount!=0)_networkCount--;else if(type==VirtioDeviceType.Console&&_consoleCount!=0)_consoleCount--;else if(type==VirtioDeviceType.EntropySource&&_rngCount!=0)_rngCount--;return true;}
    private static Boolean Interrupt(KernelDriverDeviceContext* context,UInt64 cookie)=>context!=null&&Service(context->Device);

    private static Boolean InitializeTransport(DeviceRecord* r,PciLocation location,VirtioDeviceType type)
    {
        if(!MapTransportCapability(location,CommonConfigurationType,out r->Common,out _)||!MapTransportCapability(location,NotifyConfigurationType,out r->Notify,out UInt16 notifyCap))return false;
        MapTransportCapability(location,IsrConfigurationType,out r->Isr,out _);MapTransportCapability(location,DeviceConfigurationType,out r->DeviceConfig,out _);if(!KernelPci.TryRead32(location,(UInt16)(notifyCap+16U),out UInt32 multiplier)||multiplier==0U)return false;r->NotifyMultiplier=multiplier;
        Write8(r->Common+20,0);Write16(r->Common+16,(UInt16)0xFFFF);Write8(r->Common+20,(Byte)VirtioDeviceStatus.Acknowledge);Write8(r->Common+20,(Byte)(VirtioDeviceStatus.Acknowledge|VirtioDeviceStatus.Driver));
        Write32(r->Common+0,0);UInt64 features=Read32(r->Common+4);Write32(r->Common+0,1);features|=(UInt64)Read32(r->Common+4)<<32;r->DeviceFeatures=features;if((features&FeatureVersion1)==0UL)return false;
        UInt64 supported=FeatureVersion1;if(type==VirtioDeviceType.Block)supported|=BlockFeatureReadOnly|BlockFeatureBlockSize|BlockFeatureFlush;else if(type==VirtioDeviceType.Network)supported|=NetworkFeatureMtu|NetworkFeatureMac|NetworkFeatureStatus;r->NegotiatedFeatures=features&supported;
        Write32(r->Common+8,0);Write32(r->Common+12,(UInt32)r->NegotiatedFeatures);Write32(r->Common+8,1);Write32(r->Common+12,(UInt32)(r->NegotiatedFeatures>>32));
        Byte status=(Byte)(Read8(r->Common+20)|(Byte)VirtioDeviceStatus.FeaturesOk);Write8(r->Common+20,status);if((Read8(r->Common+20)&(Byte)VirtioDeviceStatus.FeaturesOk)==0)return false;return true;
    }

    private static Boolean InitializeBlock(DeviceRecord* r)
    {
        if(r->DeviceConfig==0||!SetupQueue(r,&r->Queue0,0,128))return false;UInt64 sectors=Read64(r->DeviceConfig);UInt32 blockSize=(r->NegotiatedFeatures&BlockFeatureBlockSize)!=0?Read32(r->DeviceConfig+20):512U;if(blockSize<512U||(blockSize&(blockSize-1U))!=0U)return false;UInt64 bytes=sectors>UInt64.MaxValue/512UL?UInt64.MaxValue:sectors*512UL;UInt64 blocks=bytes/blockSize;if(blocks==0)return false;r->BlockSize=blockSize;r->BlockCount=blocks;Boolean readOnly=(r->NegotiatedFeatures&BlockFeatureReadOnly)!=0;
        KernelStorageGeometry geometry=new(blockSize,blockSize,blocks,readOnly,false);KernelContextualBlockDeviceCallbacks callbacks=new(&BlockRead,&BlockWrite,&BlockFlush);if(!KernelStorage.RegisterBlockDevice(new KernelDeviceHandle(r->DeviceHandle),KernelStorageDeviceKind.Virtual,geometry,callbacks,out KernelStorageDeviceHandle storage))return false;r->StorageHandle=storage.Value;return true;
    }

    private static Boolean InitializeNetwork(DeviceRecord* r)
    {
        if(r->DeviceConfig==0||(r->NegotiatedFeatures&NetworkFeatureMac)==0||!SetupQueue(r,&r->Queue0,0,128)||!SetupQueue(r,&r->Queue1,1,128))return false;KernelMacAddress mac=new(Read8(r->DeviceConfig),Read8(r->DeviceConfig+1),Read8(r->DeviceConfig+2),Read8(r->DeviceConfig+3),Read8(r->DeviceConfig+4),Read8(r->DeviceConfig+5));if(mac.IsZero)return false;UInt32 mtu=(r->NegotiatedFeatures&NetworkFeatureMtu)!=0?Read16(r->DeviceConfig+10):1500U;if(mtu<576U||mtu>65525U)return false;r->Mtu=mtu;
        UInt64 rxBytes=(UInt64)mtu+VirtioNetworkHeaderBytes;if(!AllocateDma(rxBytes,out r->RxToken,out r->RxPages,out r->RxPhysical,out r->RxVirtual))return false;r->RxBytes=(UInt32)rxBytes;if(!PostReceiveBuffer(r))return false;KernelContextualNetworkInterfaceCallbacks callbacks=new(&NetworkTransmit,&NetworkReceiveEnabled);if(!KernelNetworking.RegisterInterface(new KernelDeviceHandle(r->DeviceHandle),mac,mtu,callbacks,out KernelNetworkInterfaceHandle network))return false;r->NetworkHandle=network.Value;return true;
    }

    private static Boolean InitializeConsole(DeviceRecord* r)=>SetupQueue(r,&r->Queue0,0,64)&&SetupQueue(r,&r->Queue1,1,64);
    private static Boolean InitializeEntropy(DeviceRecord* r)=>SetupQueue(r,&r->Queue0,0,64);

    private static Boolean SetupQueue(DeviceRecord* r,QueueRecord* q,UInt16 index,UInt16 requested)
    {
        Write16(r->Common+22,index);UInt16 maximum=Read16(r->Common+24);if(maximum==0)return false;UInt16 size=VirtioMath.SelectQueueSize(maximum,requested);if(size<8)return false;Write16(r->Common+24,size);
        UInt64 descriptorBytes=(UInt64)size*16UL;UInt64 availableOffset=descriptorBytes;UInt64 availableBytes=6UL+(UInt64)size*2UL;UInt64 usedOffset=AlignUp(availableOffset+availableBytes,4UL);UInt64 usedBytes=6UL+(UInt64)size*8UL;UInt64 bytes=usedOffset+usedBytes;
        if(!AllocateDma(bytes,out UInt64 token,out UInt64 pages,out UInt64 physical,out UInt64 virtualAddress))return false;Clear((Byte*)(nuint)virtualAddress,pages*4096UL);q->Index=index;q->Size=size;q->PhysicalBase=physical;q->VirtualBase=virtualAddress;q->DescriptorOffset=0;q->AvailableOffset=availableOffset;q->UsedOffset=usedOffset;q->AllocationToken=token;q->AllocationPages=pages;q->LastUsed=0;
        Write64(r->Common+32,physical);Write64(r->Common+40,physical+availableOffset);Write64(r->Common+48,physical+usedOffset);q->NotifyOffset=Read16(r->Common+30);Write16(r->Common+26,(UInt16)0xFFFF);Write16(r->Common+28,1);if(Read16(r->Common+28)==0){ReleaseDma(token,physical,pages);Clear((Byte*)q,(UInt64)sizeof(QueueRecord));return false;}q->Ready=1;return true;
    }

    private static Boolean BlockRead(KernelDeviceHandle device,UInt64 firstBlock,UInt32 blockCount,Byte* buffer,UInt32 bufferBytes)=>TransferBlocks(device,firstBlock,blockCount,buffer,bufferBytes,false);
    private static Boolean BlockWrite(KernelDeviceHandle device,UInt64 firstBlock,UInt32 blockCount,Byte* buffer,UInt32 bufferBytes)=>TransferBlocks(device,firstBlock,blockCount,buffer,bufferBytes,true);
    private static Boolean BlockFlush(KernelDeviceHandle device)
    {
        if(!TryRecord(device,out DeviceRecord* r)||(VirtioDeviceType)r->Type!=VirtioDeviceType.Block||r->Started==0)return false;if((r->NegotiatedFeatures&BlockFeatureFlush)==0)return true;UInt64 token,pages,physical,virtualAddress;if(!AllocateDma(17,out token,out pages,out physical,out virtualAddress))return false;Byte* p=(Byte*)(nuint)virtualAddress;Clear(p,pages*4096UL);Write32((UInt64)(nuint)p,BlockRequestFlush);p[16]=0xFF;SetDescriptor(&r->Queue0,0,physical,16,DescriptorNext,1);SetDescriptor(&r->Queue0,1,physical+16,1,DescriptorWrite,0);Boolean ok=SubmitAndWait(r,&r->Queue0,0,out _)&&p[16]==0;ReleaseDma(token,physical,pages);return ok;
    }

    private static Boolean TransferBlocks(KernelDeviceHandle device,UInt64 firstBlock,UInt32 blockCount,Byte* buffer,UInt32 bufferBytes,Boolean write)
    {
        if(buffer==null||blockCount==0||!TryRecord(device,out DeviceRecord* r)||(VirtioDeviceType)r->Type!=VirtioDeviceType.Block||r->Started==0||firstBlock>=r->BlockCount||blockCount>r->BlockCount-firstBlock)return false;UInt64 dataBytes=(UInt64)blockCount*r->BlockSize;if(dataBytes>bufferBytes||dataBytes>UInt32.MaxValue)return false;UInt64 total=16UL+dataBytes+1UL;UInt64 token,pages,physical,virtualAddress;if(!AllocateDma(total,out token,out pages,out physical,out virtualAddress))return false;Byte* p=(Byte*)(nuint)virtualAddress;Clear(p,pages*4096UL);Write32((UInt64)(nuint)p,write?BlockRequestOut:BlockRequestIn);Write64((UInt64)(nuint)(p+8),firstBlock*((UInt64)r->BlockSize/512UL));if(write)Copy(buffer,p+16,dataBytes);p[16+dataBytes]=0xFF;
        SetDescriptor(&r->Queue0,0,physical,16,DescriptorNext,1);SetDescriptor(&r->Queue0,1,physical+16,(UInt32)dataBytes,(UInt16)(DescriptorNext|(write?0:DescriptorWrite)),2);SetDescriptor(&r->Queue0,2,physical+16+dataBytes,1,DescriptorWrite,0);Boolean ok=SubmitAndWait(r,&r->Queue0,0,out _)&&p[16+dataBytes]==0;if(ok&&!write)Copy(p+16,buffer,dataBytes);ReleaseDma(token,physical,pages);return ok;
    }

    private static Boolean NetworkTransmit(KernelDeviceHandle device,Byte* frame,UInt32 length)
    {
        if(frame==null||length==0||!TryRecord(device,out DeviceRecord* r)||(VirtioDeviceType)r->Type!=VirtioDeviceType.Network||r->Started==0||length>r->Mtu+14U)return false;UInt64 total=VirtioNetworkHeaderBytes+(UInt64)length;UInt64 token,pages,physical,virtualAddress;if(!AllocateDma(total,out token,out pages,out physical,out virtualAddress))return false;Byte* p=(Byte*)(nuint)virtualAddress;Clear(p,VirtioNetworkHeaderBytes);Copy(frame,p+VirtioNetworkHeaderBytes,length);SetDescriptor(&r->Queue1,0,physical,(UInt32)total,0,0);Boolean ok=SubmitAndWait(r,&r->Queue1,0,out _);ReleaseDma(token,physical,pages);return ok;
    }
    private static Boolean NetworkReceiveEnabled(KernelDeviceHandle device,Boolean enabled){if(!TryRecord(device,out DeviceRecord* r)||(VirtioDeviceType)r->Type!=VirtioDeviceType.Network)return false;r->ReceiveEnabled=enabled?(Byte)1:(Byte)0;return true;}
    private static Boolean ServiceNetwork(DeviceRecord* r)
    {
        while(TryConsumeUsed(&r->Queue0,out _,out UInt32 length)){if(length>VirtioNetworkHeaderBytes&&length<=r->RxBytes&&r->ReceiveEnabled!=0)KernelNetworking.QueueReceivedFrame(new KernelNetworkInterfaceHandle(r->NetworkHandle),(Byte*)(nuint)(r->RxVirtual+VirtioNetworkHeaderBytes),length-VirtioNetworkHeaderBytes,out _);if(!PostReceiveBuffer(r))return false;}return true;
    }
    private static Boolean PostReceiveBuffer(DeviceRecord* r){SetDescriptor(&r->Queue0,0,r->RxPhysical,r->RxBytes,DescriptorWrite,0);return Submit(r,&r->Queue0,0);}

    private static Boolean TransferSimple(DeviceRecord* r,QueueRecord* q,Byte* source,UInt32 length,Boolean deviceWrites)
    {UInt64 token,pages,physical,virtualAddress;if(!AllocateDma(length,out token,out pages,out physical,out virtualAddress))return false;if(!deviceWrites)Copy(source,(Byte*)(nuint)virtualAddress,length);SetDescriptor(q,0,physical,length,deviceWrites?DescriptorWrite:(UInt16)0,0);Boolean ok=SubmitAndWait(r,q,0,out _);ReleaseDma(token,physical,pages);return ok;}
    private static Boolean TransferSimpleRead(DeviceRecord* r,QueueRecord* q,Byte* destination,UInt32 capacity,out UInt32 bytesRead)
    {bytesRead=0;UInt64 token,pages,physical,virtualAddress;if(!AllocateDma(capacity,out token,out pages,out physical,out virtualAddress))return false;SetDescriptor(q,0,physical,capacity,DescriptorWrite,0);Boolean ok=SubmitAndWait(r,q,0,out UInt32 used);if(ok&&used<=capacity){Copy((Byte*)(nuint)virtualAddress,destination,used);bytesRead=used;}else ok=false;ReleaseDma(token,physical,pages);return ok;}

    private static Boolean SubmitAndWait(DeviceRecord* r,QueueRecord* q,UInt16 head,out UInt32 length)
    {length=0;if(!Submit(r,q,head)||!KernelTime.TryCreateDeadline(SynchronousTimeoutNanoseconds,out UInt64 deadline))return false;while(!TryConsumeUsed(q,out _,out length)){if(KernelTime.HasReached(deadline))return false;}return true;}
    private static Boolean Submit(DeviceRecord* r,QueueRecord* q,UInt16 head)
    {if(q->Ready==0||head>=q->Size)return false;Byte* available=(Byte*)(nuint)(q->VirtualBase+q->AvailableOffset);UInt16 index=Read16((UInt64)(nuint)(available+2));*(UInt16*)(available+4+(UInt64)(index%q->Size)*2UL)=head;Write16((UInt64)(nuint)(available+2),(UInt16)(index+1));UInt64 notify=r->Notify+q->NotifyOffset*r->NotifyMultiplier;Write16(notify,q->Index);return true;}
    private static Boolean TryConsumeUsed(QueueRecord* q,out UInt32 id,out UInt32 length)
    {id=0;length=0;if(q->Ready==0)return false;Byte* used=(Byte*)(nuint)(q->VirtualBase+q->UsedOffset);UInt16 current=Read16((UInt64)(nuint)(used+2));if(q->LastUsed==current)return false;UInt64 element=4UL+(UInt64)(q->LastUsed%q->Size)*8UL;id=Read32((UInt64)(nuint)(used+element));length=Read32((UInt64)(nuint)(used+element+4));q->LastUsed++;return true;}
    private static Boolean SetDescriptor(QueueRecord* q,UInt16 index,UInt64 address,UInt32 length,UInt16 flags,UInt16 next)
    {if(q->Ready==0||index>=q->Size)return false;UInt64 descriptor=q->VirtualBase+(UInt64)index*16UL;Write64(descriptor,address);Write32(descriptor+8,length);Write16(descriptor+12,flags);Write16(descriptor+14,next);return true;}

    private static Boolean EnablePci(PciLocation location)
    {
        if(!KernelPci.TryRead16(location,0x04,out UInt16 command))return false;
        return KernelPci.TryWrite16(location,0x04,(UInt16)(command|0x0006U));
    }

    private static Boolean MapTransportCapability(PciLocation location,Byte configurationType,out UInt64 virtualAddress,out UInt16 capabilityOffset)
    {virtualAddress=0;capabilityOffset=0;if(!TryFindTransportCapability(location,configurationType,out PciCapabilityInfo capability))return false;if(!KernelPci.TryRead8(location,(UInt16)(capability.Offset+2),out Byte capabilityLength)||capabilityLength<(configurationType==NotifyConfigurationType?20U:16U))return false;if(!KernelPci.TryRead8(location,(UInt16)(capability.Offset+4),out Byte barIndex)||!KernelPci.TryRead32(location,(UInt16)(capability.Offset+8),out UInt32 offset)||!KernelPci.TryRead32(location,(UInt16)(capability.Offset+12),out UInt32 length)||length==0)return false;if(!KernelPci.TryGetBar(location,barIndex,out PciBarInfo bar)||bar.Type==PciBarType.Io||offset>bar.Length||length>bar.Length-offset)return false;if(!KernelPci.TryMapMmio(bar.Address+offset,length,out virtualAddress))return false;capabilityOffset=capability.Offset;return true;}
    private static Boolean TryFindTransportCapability(PciLocation location,Byte configurationType,out PciCapabilityInfo found)
    {found=default;for(UInt32 i=0;i<48U;i++){if(!KernelPci.TryGetCapability(location,i,out PciCapabilityInfo capability))return false;if(capability.Id!=VendorCapabilityId)continue;if(KernelPci.TryRead8(location,(UInt16)(capability.Offset+3),out Byte type)&&type==configurationType){found=capability;return true;}}return false;}

    private static Boolean AllocateDma(UInt64 bytes,out UInt64 token,out UInt64 pages,out UInt64 physical,out UInt64 virtualAddress)
    {token=0;pages=0;physical=0;virtualAddress=0;if(bytes==0||bytes>UInt64.MaxValue-4095UL)return false;pages=(bytes+4095UL)/4096UL;if(!KernelPhysicalMemory.TryAllocate(pages,1,out KernelPhysicalAllocation allocation))return false;if(!KernelAddressSpace.TryPhysicalToDirectMap(allocation.StartAddress,out virtualAddress)){KernelPhysicalMemory.TryRelease(allocation);return false;}token=allocation.Token;physical=allocation.StartAddress;Clear((Byte*)(nuint)virtualAddress,pages*4096UL);return true;}
    private static Boolean ReleaseDma(UInt64 token,UInt64 physical,UInt64 pages)=>token==0?true:KernelPhysicalMemory.TryRelease(new KernelPhysicalAllocation(token,physical,pages));
    private static Boolean ReleaseQueue(QueueRecord* q){if(q->AllocationToken==0)return true;Boolean ok=ReleaseDma(q->AllocationToken,q->PhysicalBase,q->AllocationPages);Clear((Byte*)q,(UInt64)sizeof(QueueRecord));return ok;}
    private static Boolean ReleaseRecordResources(DeviceRecord* r){Boolean ok=ReleaseQueue(&r->Queue0)&ReleaseQueue(&r->Queue1);if(r->RxToken!=0)ok=ReleaseDma(r->RxToken,r->RxPhysical,r->RxPages)&ok;return ok;}

    private static Boolean SetStatus(DeviceRecord* r,VirtioDeviceStatus status){if(r->Common==0)return false;Write8(r->Common+20,(Byte)status);return true;}
    private static VirtioDeviceInfo Info(DeviceRecord* r)=>new(new KernelDeviceHandle(r->DeviceHandle),(VirtioDeviceType)r->Type,r->DeviceFeatures,r->NegotiatedFeatures,r->QueueCount,r->Started!=0,new KernelStorageDeviceHandle(r->StorageHandle),new KernelNetworkInterfaceHandle(r->NetworkHandle));
    private static Boolean TryRecord(KernelDeviceHandle device,out DeviceRecord* record){record=null;if(device.Value==0||_devices==null)return false;for(UInt32 i=0;i<_capacity;i++){DeviceRecord* r=_devices+i;if(r->Used!=0&&r->DeviceHandle==device.Value){record=r;return true;}}return false;}
    private static Int32 FreeRecord(){for(Int32 i=0;i<(Int32)_capacity;i++)if((_devices+i)->Used==0)return i;return -1;}
    private static Boolean AllocateRecords(UInt32 capacity,out KernelHeapAllocation allocation,out DeviceRecord* pointer){allocation=default;pointer=null;if(!KernelHeap.TryAllocate((UInt64)capacity*(UInt64)sizeof(DeviceRecord),64,true,out allocation))return false;pointer=(DeviceRecord*)(nuint)allocation.Address;return true;}
    private static Boolean GrowRecords(){UInt32 next=_capacity>=0x40000000U?UInt32.MaxValue:_capacity*2U;if(next<=_capacity||next>Int32.MaxValue||!AllocateRecords(next,out KernelHeapAllocation fresh,out DeviceRecord* pointer))return false;Copy((Byte*)_devices,(Byte*)pointer,(UInt64)_capacity*(UInt64)sizeof(DeviceRecord));KernelHeapAllocation old=_deviceAllocation;_deviceAllocation=fresh;_devices=pointer;_capacity=next;return KernelHeap.TryRelease(old);}
    private static UInt64 AlignUp(UInt64 value,UInt64 alignment)=>(value+alignment-1UL)&~(alignment-1UL);
    private static Byte Read8(UInt64 address)=>*(Byte*)(nuint)address;private static UInt16 Read16(UInt64 address)=>*(UInt16*)(nuint)address;private static UInt32 Read32(UInt64 address)=>*(UInt32*)(nuint)address;private static UInt64 Read64(UInt64 address)=>*(UInt64*)(nuint)address;
    private static Boolean Write8(UInt64 address,Byte value){*(Byte*)(nuint)address=value;return true;}private static Boolean Write16(UInt64 address,UInt16 value){*(UInt16*)(nuint)address=value;return true;}private static Boolean Write32(UInt64 address,UInt32 value){*(UInt32*)(nuint)address=value;return true;}private static Boolean Write64(UInt64 address,UInt64 value){*(UInt64*)(nuint)address=value;return true;}
    private static Boolean Copy(Byte* source,Byte* destination,UInt64 bytes){for(UInt64 i=0;i<bytes;i++)destination[i]=source[i];return true;}private static Boolean Clear(Byte* destination,UInt64 bytes){for(UInt64 i=0;i<bytes;i++)destination[i]=0;return true;}
}
