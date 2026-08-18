using System;
using NovaOryn.Kernel.Heap;

namespace NovaOryn.Kernel.Drivers;

/// <summary>Provides the heap-backed NovaOryn driver registry, matcher, lifecycle manager, resources, and interrupt broker.</summary>
public static unsafe class KernelDrivers
{
    private const Int32 ResourcesPerDevice=8;
    private const Int32 GrantsPerDevice=16;
    private struct DriverRecord { internal Byte Used,State,Bus,MatchBus,MatchVendor,MatchDevice; internal UInt16 Vendor,Device; internal UInt32 ClassCode,ClassMask; internal UInt64 DeclaredCapabilities,Probe,Start,Stop,Remove,Interrupt; }
    private struct DeviceRecord { internal Byte Used,State,Bus,Revision,ResourceCount,GrantCount; internal UInt16 Vendor,Device,SubsystemVendor,Subsystem; internal UInt32 ClassCode,Location,BoundDriver; internal fixed Byte ResourceType[ResourcesPerDevice]; internal fixed UInt64 ResourceStart[ResourcesPerDevice]; internal fixed UInt64 ResourceLength[ResourcesPerDevice]; internal fixed UInt64 ResourceFlags[ResourcesPerDevice]; internal fixed UInt64 GrantToken[GrantsPerDevice]; internal fixed UInt64 GrantCapability[GrantsPerDevice]; internal fixed UInt64 GrantStart[GrantsPerDevice]; internal fixed UInt64 GrantLength[GrantsPerDevice]; internal fixed Byte GrantAccess[GrantsPerDevice]; }

    private static DriverRecord* _drivers; private static DeviceRecord* _devices;
    private static KernelHeapAllocation _driverAllocation,_deviceAllocation;
    private static UInt32 _driverCapacity,_deviceCapacity,_maximumDrivers,_maximumDevices;
    private static KernelDriverRegistryMode _mode; private static Boolean _initialized;
    private static UInt32 _driverCount,_deviceCount,_boundCount,_startedCount;
    private static UInt64 _interruptRequestBroker,_interruptReleaseBroker; private static UInt64 _nextCapabilityGrantToken=1UL;

    /// <summary>Initializes a dynamically growing registry backed by the already-online kernel heap.</summary>
    public static Boolean Initialize() => Initialize(KernelDriverFrameworkOptions.DynamicDefault);

    /// <summary>Initializes dynamic or explicitly bounded registry storage from the kernel heap.</summary>
    public static Boolean Initialize(KernelDriverFrameworkOptions options)
    {
        if(_initialized)return true;
        if(!KernelHeap.IsInitialized()||!KernelDriverMath.IsValidOptions(options))return false;
        _mode=options.RegistryMode; _maximumDrivers=options.MaximumDriverCapacity; _maximumDevices=options.MaximumDeviceCapacity;
        if(!AllocateDriverTable(options.InitialDriverCapacity,out _driverAllocation,out _drivers))return false;
        if(!AllocateDeviceTable(options.InitialDeviceCapacity,out _deviceAllocation,out _devices)){KernelHeap.TryRelease(_driverAllocation);_drivers=null;_driverAllocation=default;return false;}
        _driverCapacity=options.InitialDriverCapacity; _deviceCapacity=options.InitialDeviceCapacity;
        _driverCount=0U;_deviceCount=0U;_boundCount=0U;_startedCount=0U;_interruptRequestBroker=0UL;_interruptReleaseBroker=0UL;_initialized=true;return true;
    }

    public static Boolean IsInitialized()=>_initialized;
    public static KernelDriverCapabilities GetCapabilities()=>new(_initialized,_mode,_driverCount,_deviceCount,_boundCount,_startedCount,_driverCapacity,_deviceCapacity,_maximumDrivers,_maximumDevices,ResourcesPerDevice,_interruptRequestBroker!=0UL&&_interruptReleaseBroker!=0UL);

    public static Boolean RegisterDriver(KernelDriverMatchRule rule,KernelDriverCallbacks callbacks,out KernelDriverHandle handle)
        => RegisterDriver(rule,callbacks,KernelDriverCapabilityDeclaration.None,out handle);

    /// <summary>Registers a driver together with the maximum privilege set it may ever request.</summary>
    public static Boolean RegisterDriver(KernelDriverMatchRule rule,KernelDriverCallbacks callbacks,KernelDriverCapabilityDeclaration declaration,out KernelDriverHandle handle)
    {
        handle=default;if(!_initialized||callbacks.Probe==null||callbacks.Start==null||callbacks.Stop==null||callbacks.Remove==null)return false;
        Int32 slot=FindFreeDriver(); if(slot<0){if(!GrowDrivers())return false;slot=FindFreeDriver();if(slot<0)return false;}
        DriverRecord* r=Driver(slot);Clear((Byte*)r,sizeof(DriverRecord));r->Used=1;r->State=(Byte)KernelDriverState.Registered;r->Bus=(Byte)rule.Bus;r->MatchBus=rule.MatchBus?(Byte)1:(Byte)0;r->Vendor=rule.VendorId;r->MatchVendor=rule.MatchVendor?(Byte)1:(Byte)0;r->Device=rule.DeviceId;r->MatchDevice=rule.MatchDevice?(Byte)1:(Byte)0;r->ClassCode=rule.ClassCode;r->ClassMask=rule.ClassMask;
        r->DeclaredCapabilities=(UInt64)declaration.Capabilities;r->Probe=(UInt64)(void*)callbacks.Probe;r->Start=(UInt64)(void*)callbacks.Start;r->Stop=(UInt64)(void*)callbacks.Stop;r->Remove=(UInt64)(void*)callbacks.Remove;r->Interrupt=(UInt64)(void*)callbacks.Interrupt;_driverCount++;handle=new KernelDriverHandle((UInt32)slot+1U);return true;
    }

    public static Boolean UnregisterDriver(KernelDriverHandle handle)
    { if(!TryDriver(handle,out DriverRecord* driver))return false;for(Int32 i=0;i<(Int32)_deviceCapacity;i++){DeviceRecord* d=Device(i);if(d->Used!=0&&d->BoundDriver==handle.Value)return false;}driver->State=(Byte)KernelDriverState.Removing;Clear((Byte*)driver,sizeof(DriverRecord));_driverCount--;return true; }

    public static Boolean RegisterDevice(KernelDeviceIdentifier identifier,out KernelDeviceHandle handle)
    {
        handle=default;if(!_initialized||identifier.Bus==KernelDeviceBus.Unknown)return false;Int32 slot=FindFreeDevice();if(slot<0){if(!GrowDevices())return false;slot=FindFreeDevice();if(slot<0)return false;}
        DeviceRecord* d=Device(slot);Clear((Byte*)d,sizeof(DeviceRecord));d->Used=1;d->State=(Byte)KernelDeviceState.Registered;d->Bus=(Byte)identifier.Bus;d->Vendor=identifier.VendorId;d->Device=identifier.DeviceId;d->SubsystemVendor=identifier.SubsystemVendorId;d->Subsystem=identifier.SubsystemId;d->ClassCode=identifier.ClassCode;d->Revision=identifier.Revision;d->Location=identifier.Location;_deviceCount++;handle=new KernelDeviceHandle((UInt32)slot+1U);return true;
    }

    public static Boolean AddResource(KernelDeviceHandle device,KernelDeviceResource resource)
    { if(!TryDevice(device,out DeviceRecord* d)||!KernelDriverMath.IsValidResource(resource)||d->State!=(Byte)KernelDeviceState.Registered)return false;Int32 i=d->ResourceCount;if(i>=ResourcesPerDevice)return false;d->ResourceType[i]=(Byte)resource.Type;d->ResourceStart[i]=resource.Start;d->ResourceLength[i]=resource.Length;d->ResourceFlags[i]=resource.Flags;d->ResourceCount++;return true; }
    public static Boolean TryGetResource(KernelDeviceHandle device,UInt32 resourceIndex,out KernelDeviceResource resource)
    { resource=default;if(!TryDevice(device,out DeviceRecord* d)||resourceIndex>=d->ResourceCount)return false;Int32 i=(Int32)resourceIndex;resource=new KernelDeviceResource((KernelDeviceResourceType)d->ResourceType[i],d->ResourceStart[i],d->ResourceLength[i],d->ResourceFlags[i]);return true; }

    public static Boolean TryBindDevice(KernelDeviceHandle device,out KernelDriverHandle driver)
    {
        driver=default;if(!TryDevice(device,out DeviceRecord* d)||d->BoundDriver!=0U)return false;KernelDeviceIdentifier identifier=Identifier(d);
        for(Int32 i=0;i<(Int32)_driverCapacity;i++){DriverRecord* r=Driver(i);if(r->Used==0||!KernelDriverMath.Matches(Rule(r),identifier))continue;KernelDriverHandle candidate=new((UInt32)i+1U);KernelDriverDeviceContext context=new(device,candidate,identifier);delegate*<KernelDriverDeviceContext*,Boolean> probe=(delegate*<KernelDriverDeviceContext*,Boolean>)(void*)r->Probe;if(!probe(&context))continue;d->State=(Byte)KernelDeviceState.Probed;d->BoundDriver=candidate.Value;d->State=(Byte)KernelDeviceState.Bound;r->State=(Byte)KernelDriverState.Active;_boundCount++;driver=candidate;return true;}return false;
    }

    public static Boolean StartDevice(KernelDeviceHandle device)
    { if(!TryBound(device,out DeviceRecord* d,out DriverRecord* r,out KernelDriverDeviceContext context))return false;if(d->State==(Byte)KernelDeviceState.Started)return true;delegate*<KernelDriverDeviceContext*,Boolean> start=(delegate*<KernelDriverDeviceContext*,Boolean>)(void*)r->Start;if(!start(&context)){d->State=(Byte)KernelDeviceState.Failed;return false;}d->State=(Byte)KernelDeviceState.Started;_startedCount++;return true; }
    public static Boolean StopDevice(KernelDeviceHandle device)
    { if(!TryBound(device,out DeviceRecord* d,out DriverRecord* r,out KernelDriverDeviceContext context))return false;if(d->State!=(Byte)KernelDeviceState.Started)return true;delegate*<KernelDriverDeviceContext*,Boolean> stop=(delegate*<KernelDriverDeviceContext*,Boolean>)(void*)r->Stop;if(!stop(&context))return false;d->State=(Byte)KernelDeviceState.Stopped;_startedCount--;return true; }
    public static Boolean RemoveDevice(KernelDeviceHandle device)
    { if(!TryDevice(device,out DeviceRecord* d))return false;UInt32 bound=d->BoundDriver;if(bound!=0U){if(d->State==(Byte)KernelDeviceState.Started&&!StopDevice(device))return false;DriverRecord* r=Driver((Int32)bound-1);KernelDriverDeviceContext context=new(device,new KernelDriverHandle(bound),Identifier(d));delegate*<KernelDriverDeviceContext*,Boolean> remove=(delegate*<KernelDriverDeviceContext*,Boolean>)(void*)r->Remove;if(!remove(&context))return false;_boundCount--;}Clear((Byte*)d,sizeof(DeviceRecord));_deviceCount--;return true; }
    public static Boolean TryGetDevice(KernelDeviceHandle device,out KernelDeviceIdentifier identifier,out KernelDeviceState state,out KernelDriverHandle driver)
    { identifier=default;state=default;driver=default;if(!TryDevice(device,out DeviceRecord* d))return false;identifier=Identifier(d);state=(KernelDeviceState)d->State;driver=new KernelDriverHandle(d->BoundDriver);return true; }

    /// <summary>Gets the privilege declaration registered for a driver.</summary>
    public static Boolean TryGetDeclaredCapabilities(KernelDriverHandle driver,out KernelDriverCapabilityDeclaration declaration)
    { declaration=default;if(!TryDriver(driver,out DriverRecord* r))return false;declaration=new KernelDriverCapabilityDeclaration((KernelDriverCapability)r->DeclaredCapabilities);return true; }

    /// <summary>Explicitly grants one declared capability to a bound driver/device pair after kernel policy validation.</summary>
    public static Boolean TryGrantCapability(KernelDriverDeviceContext context,KernelDriverCapabilityRequest request,out KernelDriverCapabilityGrant grant)
    {
        grant=default;if(!KernelDriverMath.IsValidCapabilityRequest(request)||!TryBound(context.Device,out DeviceRecord* d,out DriverRecord* r,out KernelDriverDeviceContext actual)||actual.Driver.Value!=context.Driver.Value)return false;
        UInt64 bit=(UInt64)request.Capability;if((r->DeclaredCapabilities&bit)!=bit||d->GrantCount>=GrantsPerDevice)return false;
        if(!CapabilityAllowedByDevice(d,request))return false;
        UInt64 token=_nextCapabilityGrantToken++;if(token==0UL)token=_nextCapabilityGrantToken++;Int32 i=d->GrantCount++;d->GrantToken[i]=token;d->GrantCapability[i]=bit;d->GrantStart[i]=request.Start;d->GrantLength[i]=request.Length;d->GrantAccess[i]=(Byte)request.Access;grant=new KernelDriverCapabilityGrant(token,context.Device,context.Driver,request.Capability,request.Start,request.Length,request.Access);return true;
    }

    /// <summary>Validates that an opaque grant is still active and belongs to the supplied binding.</summary>
    public static Boolean ValidateCapabilityGrant(KernelDriverCapabilityGrant grant)
    { if(!grant.IsValid||!TryDevice(grant.Device,out DeviceRecord* d)||d->BoundDriver!=grant.Driver.Value)return false;for(Int32 i=0;i<d->GrantCount;i++)if(d->GrantToken[i]==grant.Token&&d->GrantCapability[i]==(UInt64)grant.Capability&&d->GrantStart[i]==grant.Start&&d->GrantLength[i]==grant.Length&&d->GrantAccess[i]==(Byte)grant.Access)return true;return false; }

    /// <summary>Revokes a previously issued capability token.</summary>
    public static Boolean RevokeCapability(KernelDriverCapabilityGrant grant)
    { if(!grant.IsValid||!TryDevice(grant.Device,out DeviceRecord* d)||d->BoundDriver!=grant.Driver.Value)return false;for(Int32 i=0;i<d->GrantCount;i++){if(d->GrantToken[i]!=grant.Token)continue;Int32 last=d->GrantCount-1;d->GrantToken[i]=d->GrantToken[last];d->GrantCapability[i]=d->GrantCapability[last];d->GrantStart[i]=d->GrantStart[last];d->GrantLength[i]=d->GrantLength[last];d->GrantAccess[i]=d->GrantAccess[last];d->GrantToken[last]=0UL;d->GrantCount--;return true;}return false; }

    private static Boolean CapabilityAllowedByDevice(DeviceRecord* d,KernelDriverCapabilityRequest request)
    {
        if(request.Capability==KernelDriverCapability.PciConfig)return d->Bus==(Byte)KernelDeviceBus.Pci;
        if(request.Capability==KernelDriverCapability.Mmio)return HasResourceRange(d,KernelDeviceResourceType.Memory,request.Start,request.Length);
        if(request.Capability==KernelDriverCapability.PortIo)return HasResourceRange(d,KernelDeviceResourceType.IoPort,request.Start,request.Length);
        if(request.Capability==KernelDriverCapability.Dma)return HasResourceType(d,KernelDeviceResourceType.Dma);
        if(request.Capability==KernelDriverCapability.Interrupt||request.Capability==KernelDriverCapability.Msi||request.Capability==KernelDriverCapability.MsiX)return HasResourceType(d,KernelDeviceResourceType.Interrupt);
        if(request.Capability==KernelDriverCapability.PhysicalMemory)return HasResourceRange(d,KernelDeviceResourceType.Memory,request.Start,request.Length);
        return request.Capability==KernelDriverCapability.Timers||request.Capability==KernelDriverCapability.Networking||request.Capability==KernelDriverCapability.Filesystem;
    }
    private static Boolean HasResourceType(DeviceRecord* d,KernelDeviceResourceType type){for(Int32 i=0;i<d->ResourceCount;i++)if(d->ResourceType[i]==(Byte)type)return true;return false;}
    private static Boolean HasResourceRange(DeviceRecord* d,KernelDeviceResourceType type,UInt64 start,UInt64 length){for(Int32 i=0;i<d->ResourceCount;i++)if(d->ResourceType[i]==(Byte)type&&KernelDriverMath.RangeContains(d->ResourceStart[i],d->ResourceLength[i],start,length))return true;return false;}

    public static Boolean InstallInterruptBroker(delegate*<KernelDriverInterruptRequest*,KernelDriverInterruptHandle*,Boolean> request,delegate*<KernelDriverInterruptHandle,Boolean> release)
    { if(!_initialized||request==null||release==null)return false;_interruptRequestBroker=(UInt64)(void*)request;_interruptReleaseBroker=(UInt64)(void*)release;return true; }
    public static Boolean TryRequestInterrupt(KernelDriverInterruptRequest request,out KernelDriverInterruptHandle handle)
    { handle=default;if(_interruptRequestBroker==0UL||!KernelDriverMath.IsValidInterruptRequest(request)||!TryDevice(request.Device,out DeviceRecord* device))return false;if(device->BoundDriver!=0U){DriverRecord* driver=Driver((Int32)device->BoundDriver-1);if(driver->DeclaredCapabilities!=0UL&&!HasAnyInterruptGrant(device))return false;}delegate*<KernelDriverInterruptRequest*,KernelDriverInterruptHandle*,Boolean> broker=(delegate*<KernelDriverInterruptRequest*,KernelDriverInterruptHandle*,Boolean>)(void*)_interruptRequestBroker;KernelDriverInterruptHandle result=default;if(!broker(&request,&result))return false;handle=result;return true; }
    public static Boolean ReleaseInterrupt(KernelDriverInterruptHandle handle)
    { if(_interruptReleaseBroker==0UL||handle.Value==0UL)return false;delegate*<KernelDriverInterruptHandle,Boolean> broker=(delegate*<KernelDriverInterruptHandle,Boolean>)(void*)_interruptReleaseBroker;return broker(handle); }
    public static Boolean DispatchInterrupt(KernelDeviceHandle device,UInt64 driverCookie)
    { if(!TryBound(device,out _,out DriverRecord* r,out KernelDriverDeviceContext context)||r->Interrupt==0UL)return false;delegate*<KernelDriverDeviceContext*,UInt64,Boolean> handler=(delegate*<KernelDriverDeviceContext*,UInt64,Boolean>)(void*)r->Interrupt;return handler(&context,driverCookie); }

    private static Boolean HasAnyInterruptGrant(DeviceRecord* d){UInt64 mask=(UInt64)(KernelDriverCapability.Interrupt|KernelDriverCapability.Msi|KernelDriverCapability.MsiX);for(Int32 i=0;i<d->GrantCount;i++)if((d->GrantCapability[i]&mask)!=0UL)return true;return false;}

    private static Int32 FindFreeDriver(){for(Int32 i=0;i<(Int32)_driverCapacity;i++)if(Driver(i)->Used==0)return i;return -1;}
    private static Int32 FindFreeDevice(){for(Int32 i=0;i<(Int32)_deviceCapacity;i++)if(Device(i)->Used==0)return i;return -1;}
    private static Boolean GrowDrivers(){if(_mode!=KernelDriverRegistryMode.Dynamic)return false;UInt32 next=KernelDriverMath.NextCapacity(_driverCapacity,_maximumDrivers);if(next<=_driverCapacity)return false;if(!AllocateDriverTable(next,out KernelHeapAllocation allocation,out DriverRecord* table))return false;Copy((Byte*)_drivers,(Byte*)table,(UInt64)_driverCapacity*(UInt64)sizeof(DriverRecord));KernelHeapAllocation old=_driverAllocation;if(!KernelHeap.TryRelease(old)){KernelHeap.TryRelease(allocation);return false;}_driverAllocation=allocation;_drivers=table;_driverCapacity=next;return true;}
    private static Boolean GrowDevices(){if(_mode!=KernelDriverRegistryMode.Dynamic)return false;UInt32 next=KernelDriverMath.NextCapacity(_deviceCapacity,_maximumDevices);if(next<=_deviceCapacity)return false;if(!AllocateDeviceTable(next,out KernelHeapAllocation allocation,out DeviceRecord* table))return false;Copy((Byte*)_devices,(Byte*)table,(UInt64)_deviceCapacity*(UInt64)sizeof(DeviceRecord));KernelHeapAllocation old=_deviceAllocation;if(!KernelHeap.TryRelease(old)){KernelHeap.TryRelease(allocation);return false;}_deviceAllocation=allocation;_devices=table;_deviceCapacity=next;return true;}
    private static Boolean AllocateDriverTable(UInt32 capacity,out KernelHeapAllocation allocation,out DriverRecord* table){allocation=default;table=null;UInt64 bytes=(UInt64)capacity*(UInt64)sizeof(DriverRecord);if(!KernelHeap.TryAllocate(bytes,64UL,true,out allocation))return false;table=(DriverRecord*)(nuint)allocation.Address;return true;}
    private static Boolean AllocateDeviceTable(UInt32 capacity,out KernelHeapAllocation allocation,out DeviceRecord* table){allocation=default;table=null;UInt64 bytes=(UInt64)capacity*(UInt64)sizeof(DeviceRecord);if(!KernelHeap.TryAllocate(bytes,64UL,true,out allocation))return false;table=(DeviceRecord*)(nuint)allocation.Address;return true;}
    private static Boolean TryDriver(KernelDriverHandle handle,out DriverRecord* record){record=null;Int32 i=(Int32)handle.Value-1;if(!_initialized||i<0||(UInt32)i>=_driverCapacity)return false;DriverRecord* r=Driver(i);if(r->Used==0)return false;record=r;return true;}
    private static Boolean TryDevice(KernelDeviceHandle handle,out DeviceRecord* record){record=null;Int32 i=(Int32)handle.Value-1;if(!_initialized||i<0||(UInt32)i>=_deviceCapacity)return false;DeviceRecord* d=Device(i);if(d->Used==0)return false;record=d;return true;}
    private static Boolean TryBound(KernelDeviceHandle device,out DeviceRecord* d,out DriverRecord* r,out KernelDriverDeviceContext context){r=null;context=default;if(!TryDevice(device,out d)||d->BoundDriver==0U)return false;r=Driver((Int32)d->BoundDriver-1);if(r->Used==0)return false;context=new KernelDriverDeviceContext(device,new KernelDriverHandle(d->BoundDriver),Identifier(d));return true;}
    private static KernelDeviceIdentifier Identifier(DeviceRecord* d)=>new((KernelDeviceBus)d->Bus,d->Vendor,d->Device,d->SubsystemVendor,d->Subsystem,d->ClassCode,d->Revision,d->Location);
    private static KernelDriverMatchRule Rule(DriverRecord* r)=>new((KernelDeviceBus)r->Bus,r->MatchBus!=0,r->Vendor,r->MatchVendor!=0,r->Device,r->MatchDevice!=0,r->ClassCode,r->ClassMask);
    private static DriverRecord* Driver(Int32 slot)=>_drivers+slot; private static DeviceRecord* Device(Int32 slot)=>_devices+slot;
    private static void Clear(Byte* p,Int32 bytes){for(Int32 i=0;i<bytes;i++)p[i]=0;} private static void Copy(Byte* source,Byte* target,UInt64 bytes){for(UInt64 i=0UL;i<bytes;i++)target[i]=source[i];}
}
