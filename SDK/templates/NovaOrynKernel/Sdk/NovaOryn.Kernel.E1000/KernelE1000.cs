using System;
using NovaOryn.Kernel.AddressSpace;
using NovaOryn.Kernel.Drivers;
using NovaOryn.Kernel.Heap;
using NovaOryn.Kernel.Memory;
using NovaOryn.Kernel.Networking;
using NovaOryn.Kernel.Pci;

namespace NovaOryn.Kernel.E1000;

/// <summary>Provides Intel E1000/E1000e PCI gigabit Ethernet controllers using DMA descriptor rings.</summary>
public static unsafe class KernelE1000
{
    private const UInt16 IntelVendor=0x8086;
    private const UInt32 DefaultMtu=1500U,DescriptorCount=64U,BufferBytes=2048U;
    private const UInt32 Ctrl=0x0000,Status=0x0008,Icr=0x00C0,Ims=0x00D0,Imc=0x00D8,Rctl=0x0100,Tctl=0x0400,Tipg=0x0410;
    private const UInt32 Rdbal=0x2800,Rdbah=0x2804,Rdlen=0x2808,Rdh=0x2810,Rdt=0x2818,Tdbal=0x3800,Tdbah=0x3804,Tdlen=0x3808,Tdh=0x3810,Tdt=0x3818;
    private const UInt32 Ral0=0x5400,Rah0=0x5404;
    private const UInt32 CtrlReset=1U<<26,RctlEnable=1U<<1,RctlBroadcast=1U<<15,RctlStripCrc=1U<<26,TctlEnable=1U<<1,TctlPadShort=1U<<3;
    private const Byte RxDone=1,TxDone=1,TxEopIfcsRs=0x0B;

    private struct DeviceRecord
    {
        internal Byte Used,Started,ReceiveEnabled,Family,Msi,Msix;internal UInt32 DeviceHandle,NetworkHandle,Mtu;internal UInt16 Segment;internal Byte Bus,PciDevice,Function,MacA,MacB,MacC,MacD,MacE,MacF;
        internal UInt64 Mmio,RxToken,RxPages,RxPhysical,RxVirtual,TxToken,TxPages,TxPhysical,TxVirtual,InterruptHandle;internal UInt32 RxIndex,TxIndex;
    }
    private static DeviceRecord* _devices;private static KernelHeapAllocation _allocation;private static UInt32 _capacity,_count;private static KernelDriverHandle _driver;private static Boolean _initialized;

    /// <summary>Installs the Intel E1000/E1000e PCI driver and starts already-discovered supported controllers.</summary>
    public static Boolean Initialize()
    {
        if(_initialized)return true;if(!KernelPci.IsInitialized()||!KernelDrivers.IsInitialized()||!KernelNetworking.IsInitialized()||!KernelHeap.IsInitialized())return false;
        if(!AllocateRecords(8U,out _allocation,out _devices))return false;_capacity=8U;
        KernelDriverMatchRule rule=new(KernelDeviceBus.Pci,true,IntelVendor,true,0,false,0x020000U,0xFF0000U);KernelDriverCallbacks callbacks=new(&Probe,&Start,&Stop,&Remove,&Interrupt);KernelDriverCapabilityDeclaration declaration=new(KernelDriverCapability.Mmio|KernelDriverCapability.Interrupt|KernelDriverCapability.Msi|KernelDriverCapability.MsiX|KernelDriverCapability.Dma|KernelDriverCapability.PciConfig|KernelDriverCapability.Networking);if(!KernelDrivers.RegisterDriver("Intel E1000",rule,callbacks,declaration,out _driver))return false;
        for(UInt32 i=0;i<KernelPci.GetDeviceCount();i++){if(!KernelPci.TryGetDevice(i,out PciDeviceInfo pci)||!E1000Math.IsSupported(pci.VendorId,pci.DeviceId))continue;if(KernelDrivers.TryGetDevice(pci.DeviceHandle,out _,out _,out KernelDriverHandle bound)&&bound.Value!=0U)continue;if(KernelDrivers.TryBindDevice(pci.DeviceHandle,out KernelDriverHandle driver)&&driver.Value==_driver.Value)KernelDrivers.StartDevice(pci.DeviceHandle);} _initialized=true;return true;
    }
    public static Boolean IsInitialized()=>_initialized;
    public static E1000Capabilities GetCapabilities()=>new(_initialized,_count,_count,DescriptorCount,DescriptorCount);
    public static UInt32 GetDeviceCount()=>_count;
    public static Boolean TryGetDevice(UInt32 index,out E1000DeviceInfo info){info=default;if(index>=_count)return false;UInt32 found=0;for(UInt32 i=0;i<_capacity;i++){DeviceRecord* r=_devices+i;if(r->Used==0)continue;if(found++==index){info=Info(r);return true;}}return false;}
    public static Boolean ServiceAll(){if(!_initialized)return false;Boolean ok=true;for(UInt32 i=0;i<_capacity;i++){DeviceRecord* r=_devices+i;if(r->Used!=0&&r->Started!=0)ok=ServiceRecord(r)&ok;}return ok;}
    public static Boolean Service(KernelDeviceHandle device){return TryRecord(device,out DeviceRecord* r)&&r->Started!=0&&ServiceRecord(r);}

    private static Boolean Probe(KernelDriverDeviceContext* context){if(context==null||!E1000Math.IsSupported(context->Identifier.VendorId,context->Identifier.DeviceId)||!KernelPci.TryGetDevice(context->Device,out PciDeviceInfo pci))return false;return (pci.ClassCode&0xFF0000U)==0x020000U;}
    private static Boolean Start(KernelDriverDeviceContext* context)
    {
        if(context==null||!KernelPci.TryGetDevice(context->Device,out PciDeviceInfo pci))return false;E1000ControllerFamily family=E1000Math.Identify(pci.VendorId,pci.DeviceId);if(family==E1000ControllerFamily.Unknown)return false;Int32 slot=FreeRecord();if(slot<0){if(!GrowRecords())return false;slot=FreeRecord();if(slot<0)return false;}DeviceRecord* r=_devices+slot;Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));r->Used=1;r->Family=(Byte)family;r->DeviceHandle=context->Device.Value;r->Segment=pci.Location.Segment;r->Bus=pci.Location.Bus;r->PciDevice=pci.Location.Device;r->Function=pci.Location.Function;r->Mtu=DefaultMtu;
        if(!EnablePci(pci.Location)||!KernelPci.TryMapBar(pci.Location,0,out PciBarInfo bar,out r->Mmio)||bar.Length<0x6000UL||!Reset(r)||!ReadMac(r,out KernelMacAddress mac)||!AllocateRings(r)||!InitializeReceive(r)||!InitializeTransmit(r)){ReleaseResources(r);Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));return false;}
        r->Msi=KernelPci.TryGetMsiCapability(pci.Location,out _)?(Byte)1:(Byte)0;r->Msix=KernelPci.TryGetMsixCapability(pci.Location,out _)?(Byte)1:(Byte)0;KernelContextualNetworkInterfaceCallbacks cb=new(&Transmit,&SetReceiveEnabled);if(!KernelNetworking.RegisterInterface(context->Device,mac,DefaultMtu,cb,out KernelNetworkInterfaceHandle network)){ReleaseResources(r);Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));return false;}r->NetworkHandle=network.Value;r->ReceiveEnabled=1;r->Started=1;KernelDriverInterruptRequest interruptRequest=new(context->Device,0U,8,0U,false,false,0UL);if(KernelDrivers.TryRequestInterrupt(interruptRequest,out KernelDriverInterruptHandle interrupt)){r->InterruptHandle=interrupt.Value;Write32(r,Ims,0x000000D0U);}if(!KernelNetworking.SetInterfaceState(network,KernelNetworkInterfaceState.Up)){if(r->InterruptHandle!=0UL)KernelDrivers.ReleaseInterrupt(new KernelDriverInterruptHandle(r->InterruptHandle));r->Started=0;ReleaseResources(r);Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));return false;}_count++;return true;
    }
    private static Boolean Stop(KernelDriverDeviceContext* context){if(context==null||!TryRecord(context->Device,out DeviceRecord* r))return false;Write32(r,Imc,0xFFFFFFFFU);if(r->InterruptHandle!=0UL){KernelDrivers.ReleaseInterrupt(new KernelDriverInterruptHandle(r->InterruptHandle));r->InterruptHandle=0UL;}Write32(r,Rctl,Read32(r,Rctl)&~RctlEnable);Write32(r,Tctl,Read32(r,Tctl)&~TctlEnable);r->Started=0;return true;}
    private static Boolean Remove(KernelDriverDeviceContext* context){if(context==null||!TryRecord(context->Device,out DeviceRecord* r))return false;ReleaseResources(r);Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));if(_count!=0)_count--;return true;}
    private static Boolean Interrupt(KernelDriverDeviceContext* context,UInt64 cookie){return context!=null&&Service(context->Device);}

    private static Boolean EnablePci(PciLocation location){if(!KernelPci.TryRead16(location,0x04,out UInt16 command))return false;return KernelPci.TryWrite16(location,0x04,(UInt16)(command|0x0006U));}
    private static Boolean Reset(DeviceRecord* r){Write32(r,Imc,0xFFFFFFFFU);Write32(r,Ctrl,Read32(r,Ctrl)|CtrlReset);for(UInt32 spin=0;spin<1000000U;spin++)if((Read32(r,Ctrl)&CtrlReset)==0U){Read32(r,Icr);return true;}return false;}
    private static Boolean AllocateRings(DeviceRecord* r)
    {
        UInt64 descriptorBytes=(UInt64)DescriptorCount*16UL,rxBytes=descriptorBytes+(UInt64)DescriptorCount*BufferBytes,txBytes=descriptorBytes+(UInt64)DescriptorCount*BufferBytes;if(!AllocateDma(rxBytes,out r->RxToken,out r->RxPages,out r->RxPhysical,out r->RxVirtual))return false;if(!AllocateDma(txBytes,out r->TxToken,out r->TxPages,out r->TxPhysical,out r->TxVirtual)){ReleaseDma(r->RxToken,r->RxPhysical,r->RxPages);r->RxToken=0;return false;}
        for(UInt32 i=0;i<DescriptorCount;i++){UInt64 rxDescriptor=r->RxVirtual+(UInt64)i*16UL;Write64(rxDescriptor,r->RxPhysical+descriptorBytes+(UInt64)i*BufferBytes);Write8(rxDescriptor+12,0);UInt64 txDescriptor=r->TxVirtual+(UInt64)i*16UL;Write64(txDescriptor,r->TxPhysical+descriptorBytes+(UInt64)i*BufferBytes);Write8(txDescriptor+12,TxDone);}return true;
    }
    private static Boolean InitializeReceive(DeviceRecord* r){UInt64 descriptorBytes=(UInt64)DescriptorCount*16UL;Write32(r,Rdbal,(UInt32)r->RxPhysical);Write32(r,Rdbah,(UInt32)(r->RxPhysical>>32));Write32(r,Rdlen,(UInt32)descriptorBytes);Write32(r,Rdh,0);Write32(r,Rdt,DescriptorCount-1U);r->RxIndex=0;Write32(r,Rctl,RctlEnable|RctlBroadcast|RctlStripCrc);return true;}
    private static Boolean InitializeTransmit(DeviceRecord* r){UInt64 descriptorBytes=(UInt64)DescriptorCount*16UL;Write32(r,Tdbal,(UInt32)r->TxPhysical);Write32(r,Tdbah,(UInt32)(r->TxPhysical>>32));Write32(r,Tdlen,(UInt32)descriptorBytes);Write32(r,Tdh,0);Write32(r,Tdt,0);r->TxIndex=0;Write32(r,Tipg,0x0060200AU);Write32(r,Tctl,TctlEnable|TctlPadShort|(15U<<4)|(64U<<12));return true;}
    private static Boolean ServiceRecord(DeviceRecord* r)
    {
        Read32(r,Icr);UInt64 descriptorBytes=(UInt64)DescriptorCount*16UL;for(UInt32 guard=0;guard<DescriptorCount;guard++){UInt64 descriptor=r->RxVirtual+(UInt64)r->RxIndex*16UL;Byte status=Read8(descriptor+12);if((status&RxDone)==0)break;UInt16 length=Read16(descriptor+8);Byte errors=Read8(descriptor+13);if(errors==0&&length>=14U&&length<=BufferBytes&&r->ReceiveEnabled!=0&&r->NetworkHandle!=0U)KernelNetworking.QueueReceivedFrame(new KernelNetworkInterfaceHandle(r->NetworkHandle),(Byte*)(nuint)(r->RxVirtual+descriptorBytes+(UInt64)r->RxIndex*BufferBytes),length,out _);Write8(descriptor+12,0);UInt32 completed=r->RxIndex;r->RxIndex=(r->RxIndex+1U)&(DescriptorCount-1U);Write32(r,Rdt,completed);}return true;
    }
    private static Boolean Transmit(KernelDeviceHandle device,Byte* frame,UInt32 length)
    {
        if(frame==null||length<14U||!TryRecord(device,out DeviceRecord* r)||r->Started==0||length>r->Mtu+14U)return false;UInt32 index=r->TxIndex;UInt64 descriptor=r->TxVirtual+(UInt64)index*16UL;if((Read8(descriptor+12)&TxDone)==0)return false;UInt64 descriptorBytes=(UInt64)DescriptorCount*16UL;Copy(frame,(Byte*)(nuint)(r->TxVirtual+descriptorBytes+(UInt64)index*BufferBytes),length);Write16(descriptor+8,(UInt16)length);Write8(descriptor+11,TxEopIfcsRs);Write8(descriptor+12,0);r->TxIndex=(index+1U)&(DescriptorCount-1U);Write32(r,Tdt,r->TxIndex);for(UInt32 spin=0;spin<1000000U;spin++)if((Read8(descriptor+12)&TxDone)!=0)return true;return false;
    }
    private static Boolean SetReceiveEnabled(KernelDeviceHandle device,Boolean enabled){if(!TryRecord(device,out DeviceRecord* r))return false;r->ReceiveEnabled=enabled?(Byte)1:(Byte)0;UInt32 value=Read32(r,Rctl);return Write32(r,Rctl,enabled?value|RctlEnable:value&~RctlEnable);}

    private static E1000DeviceInfo Info(DeviceRecord* r)=>new(new KernelDeviceHandle(r->DeviceHandle),new PciLocation(r->Segment,r->Bus,r->PciDevice,r->Function),(E1000ControllerFamily)r->Family,new KernelMacAddress(r->MacA,r->MacB,r->MacC,r->MacD,r->MacE,r->MacF),new KernelNetworkInterfaceHandle(r->NetworkHandle),r->Mtu,r->Msi!=0,r->Msix!=0);
    private static Boolean ReadMac(DeviceRecord* r,out Byte a,out Byte b,out Byte c,out Byte d,out Byte e,out Byte f){UInt32 low=Read32(r,Ral0),high=Read32(r,Rah0);a=(Byte)low;b=(Byte)(low>>8);c=(Byte)(low>>16);d=(Byte)(low>>24);e=(Byte)high;f=(Byte)(high>>8);return (a|b|c|d|e|f)!=0;}
    private static Boolean ReadMac(DeviceRecord* r,out KernelMacAddress mac,Boolean store=true){Boolean ok=ReadMac(r,out Byte a,out Byte b,out Byte c,out Byte d,out Byte e,out Byte f);mac=new(a,b,c,d,e,f);if(ok&&store){r->MacA=a;r->MacB=b;r->MacC=c;r->MacD=d;r->MacE=e;r->MacF=f;}return ok;}
    private static Boolean TryRecord(KernelDeviceHandle device,out DeviceRecord* record){record=null;if(device.Value==0||_devices==null)return false;for(UInt32 i=0;i<_capacity;i++){DeviceRecord* r=_devices+i;if(r->Used!=0&&r->DeviceHandle==device.Value){record=r;return true;}}return false;}
    private static Int32 FreeRecord(){for(Int32 i=0;i<(Int32)_capacity;i++)if((_devices+i)->Used==0)return i;return -1;}
    private static Boolean AllocateRecords(UInt32 capacity,out KernelHeapAllocation allocation,out DeviceRecord* pointer){allocation=default;pointer=null;if(!KernelHeap.TryAllocate((UInt64)capacity*(UInt64)sizeof(DeviceRecord),64,true,out allocation))return false;pointer=(DeviceRecord*)(nuint)allocation.Address;return true;}
    private static Boolean GrowRecords(){UInt32 next=_capacity>=0x40000000U?UInt32.MaxValue:_capacity*2U;if(next<=_capacity||next>Int32.MaxValue||!AllocateRecords(next,out KernelHeapAllocation fresh,out DeviceRecord* pointer))return false;Copy((Byte*)_devices,(Byte*)pointer,(UInt64)_capacity*(UInt64)sizeof(DeviceRecord));KernelHeapAllocation old=_allocation;_allocation=fresh;_devices=pointer;_capacity=next;return KernelHeap.TryRelease(old);}
    private static Boolean AllocateDma(UInt64 bytes,out UInt64 token,out UInt64 pages,out UInt64 physical,out UInt64 virtualAddress){token=pages=physical=virtualAddress=0;if(bytes==0||bytes>UInt64.MaxValue-4095UL)return false;pages=(bytes+4095UL)/4096UL;if(!KernelPhysicalMemory.TryAllocate(pages,1,out KernelPhysicalAllocation a))return false;if(!KernelAddressSpace.TryPhysicalToDirectMap(a.StartAddress,out virtualAddress)){KernelPhysicalMemory.TryRelease(a);return false;}token=a.Token;physical=a.StartAddress;Clear((Byte*)(nuint)virtualAddress,pages*4096UL);return true;}
    private static Boolean ReleaseDma(UInt64 token,UInt64 physical,UInt64 pages)=>token==0?true:KernelPhysicalMemory.TryRelease(new KernelPhysicalAllocation(token,physical,pages));
    private static Boolean ReleaseResources(DeviceRecord* r){Boolean ok=true;if(r->RxToken!=0)ok=ReleaseDma(r->RxToken,r->RxPhysical,r->RxPages)&ok;if(r->TxToken!=0)ok=ReleaseDma(r->TxToken,r->TxPhysical,r->TxPages)&ok;r->RxToken=r->TxToken=0;return ok;}
    private static UInt32 Read32(DeviceRecord* r,UInt32 offset)=>*(UInt32*)(nuint)(r->Mmio+offset);private static Boolean Write32(DeviceRecord* r,UInt32 offset,UInt32 value){*(UInt32*)(nuint)(r->Mmio+offset)=value;return true;}
    private static Byte Read8(UInt64 address)=>*(Byte*)(nuint)address;private static UInt16 Read16(UInt64 address)=>*(UInt16*)(nuint)address;private static Boolean Write8(UInt64 address,Byte value){*(Byte*)(nuint)address=value;return true;}private static Boolean Write16(UInt64 address,UInt16 value){*(UInt16*)(nuint)address=value;return true;}private static Boolean Write64(UInt64 address,UInt64 value){*(UInt64*)(nuint)address=value;return true;}
    private static Boolean Copy(Byte* source,Byte* destination,UInt64 bytes){for(UInt64 i=0;i<bytes;i++)destination[i]=source[i];return true;}private static Boolean Clear(Byte* destination,UInt64 bytes){for(UInt64 i=0;i<bytes;i++)destination[i]=0;return true;}
}
