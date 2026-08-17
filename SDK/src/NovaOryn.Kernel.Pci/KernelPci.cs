using System;
using NovaOryn.Kernel.Acpi;
using NovaOryn.Kernel.AddressSpace;
using NovaOryn.Kernel.Drivers;
using NovaOryn.Kernel.Heap;
using NovaOryn.Kernel.Internal.X64;
using NovaOryn.Kernel.VirtualMemory;

namespace NovaOryn.Kernel.Pci;

/// <summary>Enumerates PCI/PCIe functions, exposes configuration-space access, discovers BARs and capabilities, and maps device MMIO.</summary>
public static unsafe class KernelPci
{
    private const UInt16 ConfigurationAddressPort=0x0CF8;
    private const UInt16 ConfigurationDataPort=0x0CFC;
    private const UInt16 PciStatusCapabilities=0x0010;
    private const UInt64 PageSize=4096UL;
    private struct DeviceRecord { internal Byte Used,Transport,Bus,Device,Function,Revision,HeaderType; internal UInt16 Segment,VendorId,DeviceId,SubsystemVendorId,SubsystemId; internal UInt32 ClassCode,Handle; }
    private static DeviceRecord* _devices;
    private static KernelHeapAllocation _deviceAllocation;
    private static UInt32 _deviceCapacity,_deviceCount,_ecamSegmentCount;
    private static Boolean _initialized;
    private static UInt32 _mappedConfigLocation=UInt32.MaxValue;
    private static UInt64 _nextMmioVirtual=KernelAddressSpace.MmioBase+PageSize;

    /// <summary>Initializes PCI discovery and registers every discovered PCI function with the generic driver framework.</summary>
    /// <returns><see langword="true"/> when enumeration completes.</returns>
    public static Boolean Initialize()
    {
        if(_initialized)return true;
        if(!KernelDrivers.IsInitialized()||!KernelHeap.IsInitialized()||!KernelAddressSpace.IsInitialized()||!KernelVirtualMemory.IsInitialized())return false;
        if(!AllocateDeviceTable(64U,out _deviceAllocation,out _devices))return false;
        _deviceCapacity=64U;_deviceCount=0U;_ecamSegmentCount=KernelAcpi.GetPciEcamCount();_nextMmioVirtual=KernelAddressSpace.MmioBase+PageSize;
        Boolean ok=true;
        if(_ecamSegmentCount!=0U)
        {
            for(UInt32 i=0;i<_ecamSegmentCount;i++)
            {
                if(!KernelAcpi.TryGetPciEcam(i,out AcpiPciEcamInfo ecam)){ok=false;break;}
                for(UInt32 bus=ecam.StartBus;bus<=ecam.EndBus;bus++)
                {
                    PciConfigurationTransport transport=ecam.SegmentGroup==0U?PciConfigurationTransport.LegacyIo:PciConfigurationTransport.PcieEcam;if(!EnumerateBus(new PciLocation(ecam.SegmentGroup,(Byte)bus,0,0),transport)){ok=false;break;}
                    if(bus==255U)break;
                }
                if(!ok)break;
            }
        }
        else
        {
            for(UInt32 bus=0;bus<256U;bus++)if(!EnumerateBus(new PciLocation(0,(Byte)bus,0,0),PciConfigurationTransport.LegacyIo)){ok=false;break;}
        }
        if(_mappedConfigLocation!=UInt32.MaxValue){KernelVirtualMemory.TryUnmap(KernelAddressSpace.MmioBase);_mappedConfigLocation=UInt32.MaxValue;}
        if(!ok)return false;
        _initialized=true;return true;
    }

    /// <summary>Gets whether PCI discovery completed.</summary>
    public static Boolean IsInitialized()=>_initialized;
    /// <summary>Gets PCI discovery statistics.</summary>
    public static PciCapabilities GetCapabilities()=>new(_initialized,_deviceCount,_ecamSegmentCount,true,_nextMmioVirtual);
    /// <summary>Gets the number of discovered PCI functions.</summary>
    public static UInt32 GetDeviceCount()=>_deviceCount;

    /// <summary>Gets one discovered PCI function by discovery index.</summary>
    public static Boolean TryGetDevice(UInt32 index,out PciDeviceInfo info)
    {
        info=default;if(index>=_deviceCount)return false;UInt32 found=0;
        for(UInt32 i=0;i<_deviceCapacity;i++){DeviceRecord* r=_devices+i;if(r->Used==0)continue;if(found++==index){info=Info(r);return true;}}
        return false;
    }

    /// <summary>Finds PCI metadata for a generic driver-framework device handle.</summary>
    public static Boolean TryGetDevice(KernelDeviceHandle handle,out PciDeviceInfo info)
    {
        info=default;if(handle.Value==0U)return false;
        for(UInt32 i=0;i<_deviceCapacity;i++){DeviceRecord* r=_devices+i;if(r->Used!=0&&r->Handle==handle.Value){info=Info(r);return true;}}
        return false;
    }

    /// <summary>Reads one 8-bit PCI configuration field.</summary>
    public static Boolean TryRead8(PciLocation location,UInt16 offset,out Byte value){value=0;if(!TryRead32(location,(UInt16)(offset&0xFFFC),out UInt32 dword))return false;value=(Byte)(dword>>((offset&3)*8));return true;}
    /// <summary>Reads one 16-bit PCI configuration field.</summary>
    public static Boolean TryRead16(PciLocation location,UInt16 offset,out UInt16 value){value=0;if((offset&1)!=0)return false;if(!TryRead32(location,(UInt16)(offset&0xFFFC),out UInt32 dword))return false;value=(UInt16)(dword>>((offset&2)*8));return true;}
    /// <summary>Reads one 32-bit PCI configuration field.</summary>
    public static Boolean TryRead32(PciLocation location,UInt16 offset,out UInt32 value)
    {
        value=0;if((offset&3)!=0||location.Device>31||location.Function>7)return false;
        if(PciMath.ShouldUseLegacyConfiguration(location,offset))
        {
            UInt32 address=0x80000000U|((UInt32)location.Bus<<16)|((UInt32)location.Device<<11)|((UInt32)location.Function<<8)|(UInt32)(offset&0xFC);
            return Native.WritePort32(ConfigurationAddressPort,address)&&Native.ReadPort32(ConfigurationDataPort,out value);
        }
        if(TryFindEcam(location,out AcpiPciEcamInfo ecam))
        {
            if(offset>=4096U||!TryMapConfigurationFunction(location,ecam,out UInt64 virtualBase))return false;
            value=*(UInt32*)(nuint)(virtualBase+offset);return true;
        }
        return false;
    }

    /// <summary>Writes one 8-bit PCI configuration field.</summary>
    public static Boolean TryWrite8(PciLocation location,UInt16 offset,Byte value){UInt16 aligned=(UInt16)(offset&0xFFFC);if(!TryRead32(location,aligned,out UInt32 current))return false;Int32 shift=(offset&3)*8;UInt32 mask=0xFFU<<shift;return TryWrite32(location,aligned,(current&~mask)|((UInt32)value<<shift));}
    /// <summary>Writes one 16-bit PCI configuration field.</summary>
    public static Boolean TryWrite16(PciLocation location,UInt16 offset,UInt16 value){if((offset&1)!=0)return false;UInt16 aligned=(UInt16)(offset&0xFFFC);if(!TryRead32(location,aligned,out UInt32 current))return false;Int32 shift=(offset&2)*8;UInt32 mask=0xFFFFU<<shift;return TryWrite32(location,aligned,(current&~mask)|((UInt32)value<<shift));}
    /// <summary>Writes one 32-bit PCI configuration field.</summary>
    public static Boolean TryWrite32(PciLocation location,UInt16 offset,UInt32 value)
    {
        if((offset&3)!=0||location.Device>31||location.Function>7)return false;
        if(PciMath.ShouldUseLegacyConfiguration(location,offset))
        {
            UInt32 address=0x80000000U|((UInt32)location.Bus<<16)|((UInt32)location.Device<<11)|((UInt32)location.Function<<8)|(UInt32)(offset&0xFC);
            return Native.WritePort32(ConfigurationAddressPort,address)&&Native.WritePort32(ConfigurationDataPort,value);
        }
        if(TryFindEcam(location,out AcpiPciEcamInfo ecam))
        {
            if(offset>=4096U||!TryMapConfigurationFunction(location,ecam,out UInt64 virtualBase))return false;
            *(UInt32*)(nuint)(virtualBase+offset)=value;return true;
        }
        return false;
    }

    /// <summary>Discovers one implemented BAR, including its standard sizing transaction.</summary>
    public static Boolean TryGetBar(PciLocation location,Byte barIndex,out PciBarInfo bar)
    {
        bar=default;if(barIndex>=6U||!TryRead8(location,0x0E,out Byte header))return false;Byte type=(Byte)(header&0x7F);Byte maximum=type==0? (Byte)6 : type==1 ? (Byte)2 : (Byte)0;if(barIndex>=maximum)return false;
        UInt16 command;if(!TryRead16(location,0x04,out command))return false;if(!TryWrite16(location,0x04,(UInt16)(command&~3U)))return false;
        UInt16 offset=(UInt16)(0x10U+(UInt16)barIndex*4U);Boolean result=false;
        if(TryRead32(location,offset,out UInt32 low)&&low!=0U)
        {
            if((low&1U)!=0U)
            {
                if(TryWrite32(location,offset,0xFFFFFFFFU)&&TryRead32(location,offset,out UInt32 mask)&&TryWrite32(location,offset,low))
                {UInt32 masked=mask&0xFFFFFFFCU;UInt64 length=masked==0U?0UL:(UInt64)(~masked+1U);if(length!=0UL){bar=new PciBarInfo(barIndex,PciBarType.Io,low&0xFFFFFFFCU,length,false);result=true;}}
            }
            else
            {
                UInt32 memoryType=(low>>1)&3U;Boolean prefetch=(low&8U)!=0U;
                if(memoryType==2U&&barIndex+1U<maximum&&TryRead32(location,(UInt16)(offset+4U),out UInt32 high))
                {
                    if(TryWrite32(location,offset,0xFFFFFFFFU)&&TryWrite32(location,(UInt16)(offset+4U),0xFFFFFFFFU)&&TryRead32(location,offset,out UInt32 maskLow)&&TryRead32(location,(UInt16)(offset+4U),out UInt32 maskHigh)&&TryWrite32(location,offset,low)&&TryWrite32(location,(UInt16)(offset+4U),high))
                    {UInt64 address=((UInt64)high<<32)|(low&0xFFFFFFF0U);UInt64 mask=((UInt64)maskHigh<<32)|(maskLow&0xFFFFFFF0U);UInt64 length=mask==0UL?0UL:(~mask)+1UL;if(length!=0UL){bar=new PciBarInfo(barIndex,PciBarType.Memory64,address,length,prefetch);result=true;}}
                }
                else if(memoryType==0U)
                {
                    if(TryWrite32(location,offset,0xFFFFFFFFU)&&TryRead32(location,offset,out UInt32 mask)&&TryWrite32(location,offset,low))
                    {UInt32 masked=mask&0xFFFFFFF0U;UInt64 length=masked==0U?0UL:(UInt64)(~masked+1U);if(length!=0UL){bar=new PciBarInfo(barIndex,PciBarType.Memory32,low&0xFFFFFFF0U,length,prefetch);result=true;}}
                }
            }
        }
        TryWrite16(location,0x04,command);return result;
    }

    /// <summary>Gets a conventional PCI capability by zero-based list index.</summary>
    public static Boolean TryGetCapability(PciLocation location,UInt32 requestedIndex,out PciCapabilityInfo capability)
    {
        capability=default;if(!TryRead16(location,0x06,out UInt16 status)||(status&PciStatusCapabilities)==0||!TryRead8(location,0x34,out Byte pointer))return false;
        UInt64 visited=0UL;UInt32 index=0U;
        while(pointer>=0x40U&&pointer<=0xFCU&&(pointer&3U)==0U)
        {UInt32 slot=(UInt32)(pointer-0x40U)>>2;if(slot<64U){UInt64 bit=1UL<<(Int32)slot;if((visited&bit)!=0)return false;visited|=bit;}if(!TryRead8(location,pointer,out Byte id)||!TryRead8(location,(UInt16)(pointer+1U),out Byte next))return false;if(index++==requestedIndex){capability=new PciCapabilityInfo(id,pointer,next);return true;}pointer=(Byte)(next&0xFCU);}
        return false;
    }

    /// <summary>Finds the first conventional PCI capability with the requested identifier.</summary>
    public static Boolean TryFindCapability(PciLocation location,Byte capabilityId,out PciCapabilityInfo capability)
    {capability=default;for(UInt32 i=0;i<48U;i++){if(!TryGetCapability(location,i,out PciCapabilityInfo current))return false;if(current.Id==capabilityId){capability=current;return true;}}return false;}

    /// <summary>Gets one PCIe extended capability by zero-based list index.</summary>
    public static Boolean TryGetExtendedCapability(PciLocation location,UInt32 requestedIndex,out PciExtendedCapabilityInfo capability)
    {
        capability=default;if(!TryFindEcam(location,out _))return false;UInt16 offset=0x100;UInt32 index=0;
        for(UInt32 guard=0;guard<1024U&&offset>=0x100U&&offset<=0xFFCU;guard++)
        {if(!TryRead32(location,offset,out UInt32 header)||header==0U||header==0xFFFFFFFFU)return false;UInt16 id=(UInt16)(header&0xFFFFU);Byte version=(Byte)((header>>16)&0xFU);UInt16 next=(UInt16)((header>>20)&0xFFFU);if(index++==requestedIndex){capability=new PciExtendedCapabilityInfo(id,version,offset,next);return true;}if(next==0U||next==offset)return false;offset=next;}
        return false;
    }

    /// <summary>Discovers the standard MSI capability when present.</summary>
    public static Boolean TryGetMsiCapability(PciLocation location,out PciMsiCapability msi)
    {msi=default;if(!TryFindCapability(location,0x05,out PciCapabilityInfo cap)||!TryRead16(location,(UInt16)(cap.Offset+2U),out UInt16 control))return false;msi=new PciMsiCapability(cap.Offset,(control&(1U<<7))!=0,(control&(1U<<8))!=0,(Byte)((control>>1)&7U));return true;}

    /// <summary>Discovers the standard MSI-X capability when present.</summary>
    public static Boolean TryGetMsixCapability(PciLocation location,out PciMsixCapability msix)
    {msix=default;if(!TryFindCapability(location,0x11,out PciCapabilityInfo cap)||!TryRead16(location,(UInt16)(cap.Offset+2U),out UInt16 control)||!TryRead32(location,(UInt16)(cap.Offset+4U),out UInt32 table)||!TryRead32(location,(UInt16)(cap.Offset+8U),out UInt32 pending))return false;msix=new PciMsixCapability(cap.Offset,(UInt16)((control&0x7FFU)+1U),(Byte)(table&7U),table&0xFFFFFFF8U,(Byte)(pending&7U),pending&0xFFFFFFF8U);return true;}

    /// <summary>Programs one standard PCI MSI message. Interrupt policy remains owned by the kernel interrupt broker.</summary>
    public static Boolean TryProgramMsi(PciLocation location,UInt64 messageAddress,UInt16 messageData)
    {if(!TryGetMsiCapability(location,out PciMsiCapability msi)||!TryRead16(location,(UInt16)(msi.Offset+2U),out UInt16 control))return false;if(!TryWrite32(location,(UInt16)(msi.Offset+4U),(UInt32)messageAddress))return false;UInt16 dataOffset;if(msi.Address64){if(!TryWrite32(location,(UInt16)(msi.Offset+8U),(UInt32)(messageAddress>>32)))return false;dataOffset=(UInt16)(msi.Offset+12U);}else dataOffset=(UInt16)(msi.Offset+8U);if(!TryWrite16(location,dataOffset,messageData))return false;control=(UInt16)((control&~0x0070U)|1U);if(!TryWrite16(location,(UInt16)(msi.Offset+2U),control))return false;return SetIntxDisabled(location,true);}

    /// <summary>Disables standard PCI MSI delivery for one function.</summary>
    public static Boolean TryDisableMsi(PciLocation location)
    {if(!TryGetMsiCapability(location,out PciMsiCapability msi)||!TryRead16(location,(UInt16)(msi.Offset+2U),out UInt16 control))return false;return TryWrite16(location,(UInt16)(msi.Offset+2U),(UInt16)(control&~1U));}

    /// <summary>Programs and enables one PCI MSI-X table entry. Interrupt policy remains owned by the kernel interrupt broker.</summary>
    public static Boolean TryProgramMsix(PciLocation location,UInt16 tableEntry,UInt64 messageAddress,UInt32 messageData)
    {if(!TryGetMsixCapability(location,out PciMsixCapability msix)||tableEntry>=msix.TableSize||!TryMapBar(location,msix.TableBar,out PciBarInfo bar,out UInt64 tableBase))return false;UInt64 offset=(UInt64)msix.TableOffset+(UInt64)tableEntry*16UL;if(bar.Length<16UL||offset>bar.Length-16UL)return false;UInt32* entry=(UInt32*)(nuint)(tableBase+offset);entry[3]=1U;entry[0]=(UInt32)messageAddress;entry[1]=(UInt32)(messageAddress>>32);entry[2]=messageData;entry[3]=0U;if(!TryRead16(location,(UInt16)(msix.Offset+2U),out UInt16 control))return false;control=(UInt16)((control|0x8000U)&~0x4000U);if(!TryWrite16(location,(UInt16)(msix.Offset+2U),control))return false;return SetIntxDisabled(location,true);}

    /// <summary>Disables PCI MSI-X delivery for one function.</summary>
    public static Boolean TryDisableMsix(PciLocation location)
    {if(!TryGetMsixCapability(location,out PciMsixCapability msix)||!TryRead16(location,(UInt16)(msix.Offset+2U),out UInt16 control))return false;return TryWrite16(location,(UInt16)(msix.Offset+2U),(UInt16)(control&~0x8000U));}

    /// <summary>Enables or disables legacy PCI INTx assertion through the PCI command register.</summary>
    public static Boolean SetIntxDisabled(PciLocation location,Boolean disabled)
    {if(!TryRead16(location,4,out UInt16 command))return false;UInt16 next=disabled?(UInt16)(command|0x0400U):(UInt16)(command&~0x0400U);return TryWrite16(location,4,next);}

    /// <summary>Maps an arbitrary physical MMIO range into the standard kernel MMIO window using device-memory page protections.</summary>
    public static Boolean TryMapMmio(UInt64 physicalAddress,UInt64 length,out UInt64 virtualAddress)
    {
        virtualAddress=0;if(length==0UL||physicalAddress>UInt64.MaxValue-(length-1UL))return false;UInt64 pageBase=physicalAddress&~0xFFFUL;UInt64 offset=physicalAddress-pageBase;UInt64 span=offset+length;if(span<length)return false;UInt64 pages=(span+0xFFFUL)>>12;if(pages>UInt64.MaxValue/PageSize)return false;UInt64 bytes=pages*PageSize;UInt64 start=AlignUp(_nextMmioVirtual,PageSize);if(start<KernelAddressSpace.MmioBase||start>KernelAddressSpace.MmioBase+KernelAddressSpace.MmioLength-bytes)return false;
        KernelVirtualMemoryProtection protection=KernelVirtualMemoryProtection.Read|KernelVirtualMemoryProtection.Write|KernelVirtualMemoryProtection.Device|KernelVirtualMemoryProtection.Global;
        UInt64 mapped=0;for(UInt64 i=0;i<pages;i++){if(!KernelVirtualMemory.TryMap(start+i*PageSize,pageBase+i*PageSize,KernelVirtualPageSize.Page4KiB,protection)){for(UInt64 j=0;j<mapped;j++)KernelVirtualMemory.TryUnmap(start+j*PageSize);return false;}mapped++;}
        _nextMmioVirtual=start+bytes;virtualAddress=start+offset;return true;
    }

    /// <summary>Discovers and maps a memory BAR into the kernel MMIO window.</summary>
    public static Boolean TryMapBar(PciLocation location,Byte barIndex,out PciBarInfo bar,out UInt64 virtualAddress)
    {virtualAddress=0;if(!TryGetBar(location,barIndex,out bar)||bar.Type==PciBarType.Io)return false;return TryMapMmio(bar.Address,bar.Length,out virtualAddress);}

    private static Boolean EnumerateBus(PciLocation busLocation,PciConfigurationTransport transport)
    {
        for(Byte device=0;device<32;device++)
        {
            PciLocation first=new(busLocation.Segment,busLocation.Bus,device,0);if(!TryRead16(first,0x00,out UInt16 vendor)||vendor==0xFFFFU)continue;
            if(!EnumerateFunction(first,transport))return false;if(!TryRead8(first,0x0E,out Byte header))return false;if((header&0x80U)==0U)continue;
            for(Byte function=1;function<8;function++){PciLocation location=new(busLocation.Segment,busLocation.Bus,device,function);if(!TryRead16(location,0x00,out vendor))return false;if(vendor!=0xFFFFU&&!EnumerateFunction(location,transport))return false;}
        }
        return true;
    }

    private static Boolean EnumerateFunction(PciLocation location,PciConfigurationTransport transport)
    {
        if(!TryRead32(location,0x00,out UInt32 ids)||!TryRead32(location,0x08,out UInt32 classRevision)||!TryRead8(location,0x0E,out Byte rawHeader))return false;
        UInt16 vendor=(UInt16)ids,device=(UInt16)(ids>>16);if(vendor==0xFFFFU)return true;Byte header=(Byte)(rawHeader&0x7FU);UInt16 subsystemVendor=0,subsystem=0;if(header==0&&TryRead32(location,0x2C,out UInt32 subsystems)){subsystemVendor=(UInt16)subsystems;subsystem=(UInt16)(subsystems>>16);}
        UInt32 classCode=classRevision>>8;Byte revision=(Byte)classRevision;KernelDeviceIdentifier identifier=new(KernelDeviceBus.Pci,vendor,device,subsystemVendor,subsystem,classCode,revision,location.Encode());if(!KernelDrivers.RegisterDevice(identifier,out KernelDeviceHandle handle))return false;
        Byte barMaximum=header==0?(Byte)6:header==1?(Byte)2:(Byte)0;for(Byte barIndex=0;barIndex<barMaximum;barIndex++){if(!TryGetBar(location,barIndex,out PciBarInfo bar))continue;KernelDeviceResourceType resourceType=bar.Type==PciBarType.Io?KernelDeviceResourceType.IoPort:KernelDeviceResourceType.Memory;UInt64 flags=((UInt64)bar.Index)|((UInt64)bar.Type<<8)|(bar.Prefetchable?1UL<<16:0UL);if(!KernelDrivers.AddResource(handle,new KernelDeviceResource(resourceType,bar.Address,bar.Length,flags)))return false;if(bar.Type==PciBarType.Memory64)barIndex++;}
        if(TryRead8(location,0x3C,out Byte interruptLine)&&interruptLine!=0xFFU&&interruptLine!=0U)KernelDrivers.AddResource(handle,new KernelDeviceResource(KernelDeviceResourceType.Interrupt,interruptLine,0UL,0UL));
        if(!AddDeviceRecord(location,transport,handle,vendor,device,subsystemVendor,subsystem,classCode,revision,header))return false;return true;
    }

    private static Boolean AddDeviceRecord(PciLocation location,PciConfigurationTransport transport,KernelDeviceHandle handle,UInt16 vendor,UInt16 device,UInt16 subsystemVendor,UInt16 subsystem,UInt32 classCode,Byte revision,Byte header)
    {
        Int32 slot=FreeDevice();if(slot<0){if(!GrowDevices())return false;slot=FreeDevice();if(slot<0)return false;}DeviceRecord* r=_devices+slot;Clear((Byte*)r,(UInt64)sizeof(DeviceRecord));r->Used=1;r->Transport=(Byte)transport;r->Segment=location.Segment;r->Bus=location.Bus;r->Device=location.Device;r->Function=location.Function;r->VendorId=vendor;r->DeviceId=device;r->SubsystemVendorId=subsystemVendor;r->SubsystemId=subsystem;r->ClassCode=classCode;r->Revision=revision;r->HeaderType=header;r->Handle=handle.Value;_deviceCount++;return true;
    }
    private static PciDeviceInfo Info(DeviceRecord* r)=>new(new PciLocation(r->Segment,r->Bus,r->Device,r->Function),(PciConfigurationTransport)r->Transport,new KernelDeviceHandle(r->Handle),r->VendorId,r->DeviceId,r->SubsystemVendorId,r->SubsystemId,r->ClassCode,r->Revision,r->HeaderType);
    private static Boolean TryFindEcam(PciLocation location,out AcpiPciEcamInfo ecam){ecam=default;UInt32 count=KernelAcpi.GetPciEcamCount();for(UInt32 i=0;i<count;i++){if(KernelAcpi.TryGetPciEcam(i,out AcpiPciEcamInfo current)&&current.SegmentGroup==location.Segment&&location.Bus>=current.StartBus&&location.Bus<=current.EndBus){ecam=current;return true;}}return false;}
    private static Boolean TryMapConfigurationFunction(PciLocation location,AcpiPciEcamInfo ecam,out UInt64 virtualAddress)
    {
        virtualAddress=0;UInt32 encoded=location.Encode();if(_mappedConfigLocation==encoded){virtualAddress=KernelAddressSpace.MmioBase;return true;}if(_mappedConfigLocation!=UInt32.MaxValue){if(!KernelVirtualMemory.TryUnmap(KernelAddressSpace.MmioBase))return false;_mappedConfigLocation=UInt32.MaxValue;}
        UInt64 busOffset=(UInt64)(location.Bus-ecam.StartBus)<<20;UInt64 deviceOffset=(UInt64)location.Device<<15;UInt64 functionOffset=(UInt64)location.Function<<12;if(ecam.BaseAddress>UInt64.MaxValue-busOffset-deviceOffset-functionOffset)return false;UInt64 physical=ecam.BaseAddress+busOffset+deviceOffset+functionOffset;KernelVirtualMemoryProtection protection=KernelVirtualMemoryProtection.Read|KernelVirtualMemoryProtection.Write|KernelVirtualMemoryProtection.Device|KernelVirtualMemoryProtection.Global;if(!KernelVirtualMemory.TryMap(KernelAddressSpace.MmioBase,physical,KernelVirtualPageSize.Page4KiB,protection))return false;_mappedConfigLocation=encoded;virtualAddress=KernelAddressSpace.MmioBase;return true;
    }
    private static Boolean AllocateDeviceTable(UInt32 capacity,out KernelHeapAllocation allocation,out DeviceRecord* pointer){allocation=default;pointer=null;if(!KernelHeap.TryAllocate((UInt64)capacity*(UInt64)sizeof(DeviceRecord),64,true,out allocation))return false;pointer=(DeviceRecord*)(nuint)allocation.Address;return true;}
    private static Int32 FreeDevice(){for(Int32 i=0;i<(Int32)_deviceCapacity;i++)if((_devices+i)->Used==0)return i;return -1;}
    private static Boolean GrowDevices(){UInt32 next=_deviceCapacity>=0x40000000U?UInt32.MaxValue:_deviceCapacity*2U;if(next<=_deviceCapacity||next>Int32.MaxValue)return false;if(!AllocateDeviceTable(next,out KernelHeapAllocation fresh,out DeviceRecord* pointer))return false;Copy((Byte*)_devices,(Byte*)pointer,(UInt64)_deviceCapacity*(UInt64)sizeof(DeviceRecord));KernelHeapAllocation old=_deviceAllocation;_deviceAllocation=fresh;_devices=pointer;_deviceCapacity=next;return KernelHeap.TryRelease(old);}
    private static UInt64 AlignUp(UInt64 value,UInt64 alignment)=> (value+(alignment-1UL))&~(alignment-1UL);
    private static Boolean Copy(Byte* source,Byte* destination,UInt64 bytes){for(UInt64 i=0;i<bytes;i++)destination[i]=source[i];return true;}
    private static Boolean Clear(Byte* destination,UInt64 bytes){for(UInt64 i=0;i<bytes;i++)destination[i]=0;return true;}
}
