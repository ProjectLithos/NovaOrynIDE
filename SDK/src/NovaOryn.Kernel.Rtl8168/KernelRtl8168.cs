using System;
using NovaOryn.Kernel.AddressSpace;
using NovaOryn.Kernel.Drivers;
using NovaOryn.Kernel.Heap;
using NovaOryn.Kernel.Memory;
using NovaOryn.Kernel.Networking;
using NovaOryn.Kernel.Pci;

namespace NovaOryn.Kernel.Rtl8168;

/// <summary>Provides Realtek RTL8168/RTL8111-class PCIe gigabit Ethernet controllers using DMA descriptor rings.</summary>
public static unsafe class KernelRtl8168
{
    private const UInt16 RealtekVendor=0x10EC;
    private const UInt32 DefaultMtu=1500U,DescriptorCount=64U,BufferBytes=2048U;
    private const UInt32 Id0=0x00,TxDescLow=0x20,TxDescHigh=0x24,ChipCommand=0x37,TxPoll=0x38,InterruptMask=0x3C,InterruptStatus=0x3E,TxConfig=0x40,RxConfig=0x44,Cfg9346=0x50,RxMaxSize=0xDA,RxDescLow=0xE4,RxDescHigh=0xE8;
    private const Byte CommandReset=0x10,CommandRxEnable=0x08,CommandTxEnable=0x04;
    private const UInt32 DescOwn=1U<<31,DescEor=1U<<30,DescFs=1U<<29,DescLs=1U<<28,DescLengthMask=0x3FFFU;

    private struct DeviceRecord
    {
        internal Byte Used,Started,ReceiveEnabled,Family,Msi;internal UInt32 DeviceHandle,NetworkHandle,Mtu;internal UInt16 Segment;internal Byte Bus,PciDevice,Function,MacA,MacB,MacC,MacD,MacE,MacF;
        internal UInt64 Mmio,RxToken,RxPages,RxPhysical,RxVirtual,TxToken,TxPages,TxPhysical,TxVirtual,InterruptHandle;internal UInt32 RxIndex,TxIndex;
    }
    private static DeviceRecord* _devices;private static KernelHeapAllocation _allocation;private static UInt32 _capacity,_count;private static KernelDriverHandle _driver;private static Boolean _initialized;

    /// <summary>Installs the RTL8168/RTL8111 PCIe driver and starts already-discovered supported controllers.</summary>
    public static Boolean Initialize()
    {
        if(_initialized)return true;if(!KernelPci.IsInitialized()||!KernelDrivers.IsInitialized()||!KernelNetworking.IsInitialized()||!KernelHeap.IsInitialized())return false;
        if(!AllocateRecords(8U,out _allocation,out _devices))return false;_capacity=8U;KernelDriverMatchRule rule=new(KernelDeviceBus.Pci,true,RealtekVendor,true,0,false,0x020000U,0xFF0000U);KernelDriverCallbacks callbacks=new(&Probe,&Start,&Stop,&Remove,&Interrupt);KernelDriverCapabilityDeclaration declaration=new(KernelDriverCapability.Mmio|KernelDriverCapability.Interrupt|KernelDriverCapability.Msi|KernelDriverCapability.MsiX|KernelDriverCapability.Dma|KernelDriverCapability.PciConfig|KernelDriverCapability.Networking);if(!KernelDrivers.RegisterDriver(rule,callbacks,declaration,out _driver))return false;
        for(UInt32 i=0;i<KernelPci.GetDeviceCount();i++){if(!KernelPci.TryGetDevice(i,out PciDeviceInfo pci)||!Rtl8168Math.IsSupported(pci.VendorId,pci.DeviceId))continue;if(KernelDrivers.TryGetDevice(pci.DeviceHandle,out _,out _,out KernelDriverHandle bound)&&bound.Value!=0U)continue;if(KernelDrivers.TryBindDevice(pci.DeviceHandle,out KernelDriverHandle driver)&&driver.Value==_driver.Value)KernelDrivers.StartDevice(pci.DeviceHandle);} _initialized=true;return true;
    }
    public static Boolean IsInitialized()=>_initialized;
    public static Rtl8168Capabilities GetCapabilities()=>new(_initialized,_count,_count,DescriptorCount,DescriptorCount);
    public static UInt32 GetDeviceCount()=>_count;
    public static Boolean TryGetDevice(UInt32 index,out Rtl8168DeviceInfo info){info=default;if(index>=_count)return false;UInt32 found=0;for(UInt32 i=0;i<_capacity;i++){DeviceRecord* r=_devices+i;if(r->Used==0)continue;if(found++==index){info=Info(r);return true;}}return false;}
    public static Boolean ServiceAll(){if(!_initialized)return false;Boolean ok=true;for(UInt32 i=0;i<_capacity;i++){DeviceRecord* r=_devices+i;if(r->Used!=0&&r->Started!=0)ok=ServiceRecord(r)&ok;}return ok;}
    public static Boolean Service(KernelDeviceHandle device)=>TryRecord(device,out DeviceRecord* r)&&r->Started!=0&&ServiceRecord(r);

    private static Boolean Probe(KernelDriverDeviceContext* context){if(context==null||!Rtl8168Math.IsSupported(context->Identifier.VendorId,context->Identifier.DeviceId)||!KernelPci.TryGetDevice(context->Device,out PciDeviceInfo pci))return false;return (pci.ClassCode&0xFF0000U)==0x020000U;}
    private static Boolean Start(KernelDriverDeviceContext* context)
    {
        if(context==null||!KernelPci.TryGetDevice(context->Device,out PciDeviceInfo pci))return false;Rtl8168ControllerFamily family=Rtl8168Math.Identify(pci.VendorId,pci.DeviceId);if(family==Rtl8168ControllerFamily.Unknown)return false;Int32 slot=FreeRecord();if(slot<0){if(!GrowRecords())return false;slot=FreeRecord();if(slot<0)return false;}DeviceRecord* r=_devices+slot;Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));r->Used=1;r->Family=(Byte)family;r->DeviceHandle=context->Device.Value;r->Segment=pci.Location.Segment;r->Bus=pci.Location.Bus;r->PciDevice=pci.Location.Device;r->Function=pci.Location.Function;r->Mtu=DefaultMtu;
        if(!EnablePci(pci.Location)||!KernelPci.TryMapBar(pci.Location,2,out PciBarInfo bar,out r->Mmio)){if(!KernelPci.TryMapBar(pci.Location,0,out bar,out r->Mmio)){Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));return false;}}if(bar.Length<0x100UL||!Reset(r)||!ReadMac(r,out KernelMacAddress mac)||!AllocateRings(r)||!InitializeHardware(r)){ReleaseResources(r);Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));return false;}
        r->Msi=KernelPci.TryGetMsiCapability(pci.Location,out _)?(Byte)1:(Byte)0;KernelContextualNetworkInterfaceCallbacks cb=new(&Transmit,&SetReceiveEnabled);if(!KernelNetworking.RegisterInterface(context->Device,mac,DefaultMtu,cb,out KernelNetworkInterfaceHandle network)){ReleaseResources(r);Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));return false;}r->NetworkHandle=network.Value;r->ReceiveEnabled=1;r->Started=1;KernelDriverInterruptRequest interruptRequest=new(context->Device,0U,8,0U,false,false,0UL);if(KernelDrivers.TryRequestInterrupt(interruptRequest,out KernelDriverInterruptHandle interrupt)){r->InterruptHandle=interrupt.Value;Write16(r,InterruptMask,(UInt16)0x002F);}if(!KernelNetworking.SetInterfaceState(network,KernelNetworkInterfaceState.Up)){if(r->InterruptHandle!=0UL)KernelDrivers.ReleaseInterrupt(new KernelDriverInterruptHandle(r->InterruptHandle));r->Started=0;ReleaseResources(r);Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));return false;}_count++;return true;
    }
    private static Boolean Stop(KernelDriverDeviceContext* context){if(context==null||!TryRecord(context->Device,out DeviceRecord* r))return false;Write16(r,InterruptMask,0);if(r->InterruptHandle!=0UL){KernelDrivers.ReleaseInterrupt(new KernelDriverInterruptHandle(r->InterruptHandle));r->InterruptHandle=0UL;}Write8(r,ChipCommand,0);r->Started=0;return true;}
    private static Boolean Remove(KernelDriverDeviceContext* context){if(context==null||!TryRecord(context->Device,out DeviceRecord* r))return false;ReleaseResources(r);Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));if(_count!=0)_count--;return true;}
    private static Boolean Interrupt(KernelDriverDeviceContext* context,UInt64 cookie)=>context!=null&&Service(context->Device);

    private static Boolean EnablePci(PciLocation location){if(!KernelPci.TryRead16(location,0x04,out UInt16 command))return false;return KernelPci.TryWrite16(location,0x04,(UInt16)(command|0x0006U));}
    private static Boolean Reset(DeviceRecord* r){Write16(r,InterruptMask,0);Write8(r,ChipCommand,CommandReset);for(UInt32 spin=0;spin<1000000U;spin++)if((Read8(r,ChipCommand)&CommandReset)==0)return true;return false;}
    private static Boolean ReadMac(DeviceRecord* r,out KernelMacAddress mac){Byte a=Read8(r,Id0),b=Read8(r,Id0+1),c=Read8(r,Id0+2),d=Read8(r,Id0+3),e=Read8(r,Id0+4),f=Read8(r,Id0+5);mac=new(a,b,c,d,e,f);if(mac.IsZero)return false;r->MacA=a;r->MacB=b;r->MacC=c;r->MacD=d;r->MacE=e;r->MacF=f;return true;}
    private static Boolean AllocateRings(DeviceRecord* r)
    {
        UInt64 descriptorBytes=(UInt64)DescriptorCount*16UL,rxBytes=descriptorBytes+(UInt64)DescriptorCount*BufferBytes,txBytes=descriptorBytes+(UInt64)DescriptorCount*BufferBytes;if(!AllocateDma(rxBytes,out r->RxToken,out r->RxPages,out r->RxPhysical,out r->RxVirtual))return false;if(!AllocateDma(txBytes,out r->TxToken,out r->TxPages,out r->TxPhysical,out r->TxVirtual)){ReleaseDma(r->RxToken,r->RxPhysical,r->RxPages);r->RxToken=0;return false;}
        for(UInt32 i=0;i<DescriptorCount;i++){UInt32 rxOptions=DescOwn|BufferBytes;if(i==DescriptorCount-1U)rxOptions|=DescEor;UInt64 rxDescriptor=r->RxVirtual+(UInt64)i*16UL;Write32(rxDescriptor,rxOptions);Write32(rxDescriptor+4,0);UInt64 rxBuffer=r->RxPhysical+descriptorBytes+(UInt64)i*BufferBytes;Write32(rxDescriptor+8,(UInt32)rxBuffer);Write32(rxDescriptor+12,(UInt32)(rxBuffer>>32));UInt64 txDescriptor=r->TxVirtual+(UInt64)i*16UL;Write32(txDescriptor,i==DescriptorCount-1U?DescEor:0U);UInt64 txBuffer=r->TxPhysical+descriptorBytes+(UInt64)i*BufferBytes;Write32(txDescriptor+8,(UInt32)txBuffer);Write32(txDescriptor+12,(UInt32)(txBuffer>>32));}return true;
    }
    private static Boolean InitializeHardware(DeviceRecord* r)
    {
        Write8(r,Cfg9346,0xC0);Write16(r,InterruptMask,0);Write16(r,InterruptStatus,0xFFFF);Write16(r,RxMaxSize,(UInt16)(DefaultMtu+18U));Write32(r,TxDescLow,(UInt32)r->TxPhysical);Write32(r,TxDescHigh,(UInt32)(r->TxPhysical>>32));Write32(r,RxDescLow,(UInt32)r->RxPhysical);Write32(r,RxDescHigh,(UInt32)(r->RxPhysical>>32));Write32(r,RxConfig,0x0000E70FU);Write32(r,TxConfig,0x03000700U);Write8(r,ChipCommand,(Byte)(CommandRxEnable|CommandTxEnable));Write8(r,Cfg9346,0);r->RxIndex=0;r->TxIndex=0;return true;
    }
    private static Boolean ServiceRecord(DeviceRecord* r)
    {
        UInt16 status=Read16(r,InterruptStatus);if(status!=0)Write16(r,InterruptStatus,status);UInt64 descriptorBytes=(UInt64)DescriptorCount*16UL;for(UInt32 guard=0;guard<DescriptorCount;guard++){UInt64 descriptor=r->RxVirtual+(UInt64)r->RxIndex*16UL;UInt32 options=Read32(descriptor);if((options&DescOwn)!=0)break;UInt32 length=options&DescLengthMask;Boolean whole=(options&(DescFs|DescLs))==(DescFs|DescLs);if(whole&&length>4U&&length<=BufferBytes&&r->ReceiveEnabled!=0&&r->NetworkHandle!=0U)KernelNetworking.QueueReceivedFrame(new KernelNetworkInterfaceHandle(r->NetworkHandle),(Byte*)(nuint)(r->RxVirtual+descriptorBytes+(UInt64)r->RxIndex*BufferBytes),length-4U,out _);UInt32 refill=DescOwn|BufferBytes;if(r->RxIndex==DescriptorCount-1U)refill|=DescEor;Write32(descriptor,refill);r->RxIndex=(r->RxIndex+1U)&(DescriptorCount-1U);}return true;
    }
    private static Boolean Transmit(KernelDeviceHandle device,Byte* frame,UInt32 length)
    {
        if(frame==null||length<14U||!TryRecord(device,out DeviceRecord* r)||r->Started==0||length>r->Mtu+14U)return false;UInt32 index=r->TxIndex;UInt64 descriptor=r->TxVirtual+(UInt64)index*16UL;UInt32 old=Read32(descriptor);if((old&DescOwn)!=0)return false;UInt64 descriptorBytes=(UInt64)DescriptorCount*16UL;Copy(frame,(Byte*)(nuint)(r->TxVirtual+descriptorBytes+(UInt64)index*BufferBytes),length);UInt32 options=DescOwn|DescFs|DescLs|length;if(index==DescriptorCount-1U)options|=DescEor;Write32(descriptor+4,0);Write32(descriptor,options);r->TxIndex=(index+1U)&(DescriptorCount-1U);Write8(r,TxPoll,0x40);return true;
    }
    private static Boolean SetReceiveEnabled(KernelDeviceHandle device,Boolean enabled){if(!TryRecord(device,out DeviceRecord* r))return false;r->ReceiveEnabled=enabled?(Byte)1:(Byte)0;Byte cmd=Read8(r,ChipCommand);return Write8(r,ChipCommand,enabled?(Byte)(cmd|CommandRxEnable):(Byte)(cmd&~CommandRxEnable));}

    private static Rtl8168DeviceInfo Info(DeviceRecord* r)=>new(new KernelDeviceHandle(r->DeviceHandle),new PciLocation(r->Segment,r->Bus,r->PciDevice,r->Function),(Rtl8168ControllerFamily)r->Family,new KernelMacAddress(r->MacA,r->MacB,r->MacC,r->MacD,r->MacE,r->MacF),new KernelNetworkInterfaceHandle(r->NetworkHandle),r->Mtu,r->Msi!=0);
    private static Boolean TryRecord(KernelDeviceHandle device,out DeviceRecord* record){record=null;if(device.Value==0||_devices==null)return false;for(UInt32 i=0;i<_capacity;i++){DeviceRecord* r=_devices+i;if(r->Used!=0&&r->DeviceHandle==device.Value){record=r;return true;}}return false;}
    private static Int32 FreeRecord(){for(Int32 i=0;i<(Int32)_capacity;i++)if((_devices+i)->Used==0)return i;return -1;}
    private static Boolean AllocateRecords(UInt32 capacity,out KernelHeapAllocation allocation,out DeviceRecord* pointer){allocation=default;pointer=null;if(!KernelHeap.TryAllocate((UInt64)capacity*(UInt64)sizeof(DeviceRecord),64,true,out allocation))return false;pointer=(DeviceRecord*)(nuint)allocation.Address;return true;}
    private static Boolean GrowRecords(){UInt32 next=_capacity>=0x40000000U?UInt32.MaxValue:_capacity*2U;if(next<=_capacity||next>Int32.MaxValue||!AllocateRecords(next,out KernelHeapAllocation fresh,out DeviceRecord* pointer))return false;Copy((Byte*)_devices,(Byte*)pointer,(UInt64)_capacity*(UInt64)sizeof(DeviceRecord));KernelHeapAllocation old=_allocation;_allocation=fresh;_devices=pointer;_capacity=next;return KernelHeap.TryRelease(old);}
    private static Boolean AllocateDma(UInt64 bytes,out UInt64 token,out UInt64 pages,out UInt64 physical,out UInt64 virtualAddress){token=pages=physical=virtualAddress=0;if(bytes==0||bytes>UInt64.MaxValue-4095UL)return false;pages=(bytes+4095UL)/4096UL;if(!KernelPhysicalMemory.TryAllocate(pages,1,out KernelPhysicalAllocation a))return false;if(!KernelAddressSpace.TryPhysicalToDirectMap(a.StartAddress,out virtualAddress)){KernelPhysicalMemory.TryRelease(a);return false;}token=a.Token;physical=a.StartAddress;Clear((Byte*)(nuint)virtualAddress,pages*4096UL);return true;}
    private static Boolean ReleaseDma(UInt64 token,UInt64 physical,UInt64 pages)=>token==0?true:KernelPhysicalMemory.TryRelease(new KernelPhysicalAllocation(token,physical,pages));private static Boolean ReleaseResources(DeviceRecord* r){Boolean ok=true;if(r->RxToken!=0)ok=ReleaseDma(r->RxToken,r->RxPhysical,r->RxPages)&ok;if(r->TxToken!=0)ok=ReleaseDma(r->TxToken,r->TxPhysical,r->TxPages)&ok;r->RxToken=r->TxToken=0;return ok;}
    private static Byte Read8(DeviceRecord* r,UInt32 offset)=>*(Byte*)(nuint)(r->Mmio+offset);private static UInt16 Read16(DeviceRecord* r,UInt32 offset)=>*(UInt16*)(nuint)(r->Mmio+offset);private static UInt32 Read32(DeviceRecord* r,UInt32 offset)=>*(UInt32*)(nuint)(r->Mmio+offset);private static Boolean Write8(DeviceRecord* r,UInt32 offset,Byte value){*(Byte*)(nuint)(r->Mmio+offset)=value;return true;}private static Boolean Write16(DeviceRecord* r,UInt32 offset,UInt16 value){*(UInt16*)(nuint)(r->Mmio+offset)=value;return true;}private static Boolean Write32(DeviceRecord* r,UInt32 offset,UInt32 value){*(UInt32*)(nuint)(r->Mmio+offset)=value;return true;}
    private static UInt32 Read32(UInt64 address)=>*(UInt32*)(nuint)address;private static Boolean Write32(UInt64 address,UInt32 value){*(UInt32*)(nuint)address=value;return true;}
    private static Boolean Copy(Byte* source,Byte* destination,UInt64 bytes){for(UInt64 i=0;i<bytes;i++)destination[i]=source[i];return true;}private static Boolean Clear(Byte* destination,UInt64 bytes){for(UInt64 i=0;i<bytes;i++)destination[i]=0;return true;}
}
