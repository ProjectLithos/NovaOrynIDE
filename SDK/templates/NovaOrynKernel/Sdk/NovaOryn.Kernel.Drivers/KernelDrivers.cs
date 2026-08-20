using System;
using NovaOryn.Kernel.Heap;

namespace NovaOryn.Kernel.Drivers;

/// <summary>Provides the heap-backed NovaOryn driver registry, matcher, lifecycle manager, resources, and interrupt broker.</summary>
public static unsafe class KernelDrivers
{
    private const Int32 ResourcesPerDevice=8;
    private const Int32 GrantsPerDevice=16;
    private const Int32 DriverNameBytes=48;
    private struct DriverRecord { internal Byte Used,State,Bus,MatchBus,MatchVendor,MatchDevice,NameLength; internal UInt16 Vendor,Device; internal UInt32 ClassCode,ClassMask; internal UInt64 DeclaredCapabilities,Discover,Probe,Bind,Start,Stop,Reset,Suspend,Resume,Remove,Fail,Recover,Interrupt; internal fixed Byte Name[DriverNameBytes]; }
    private struct DeviceRecord { internal Byte Used,State,Bus,Revision,ResourceCount,GrantCount; internal UInt16 Vendor,Device,SubsystemVendor,Subsystem; internal UInt32 ClassCode,Location,BoundDriver,Parent,FirstChild,NextSibling,FailureCode; internal fixed Byte ResourceType[ResourcesPerDevice]; internal fixed UInt64 ResourceStart[ResourcesPerDevice]; internal fixed UInt64 ResourceLength[ResourcesPerDevice]; internal fixed UInt64 ResourceFlags[ResourcesPerDevice]; internal fixed UInt64 GrantToken[GrantsPerDevice]; internal fixed UInt64 GrantCapability[GrantsPerDevice]; internal fixed UInt64 GrantStart[GrantsPerDevice]; internal fixed UInt64 GrantLength[GrantsPerDevice]; internal fixed Byte GrantAccess[GrantsPerDevice]; }

    private static DriverRecord* _drivers; private static DeviceRecord* _devices;
    private static KernelHeapAllocation _driverAllocation,_deviceAllocation;
    private static UInt32 _driverCapacity,_deviceCapacity,_maximumDrivers,_maximumDevices;
    private static KernelDriverRegistryMode _mode; private static Boolean _initialized;
    private static UInt32 _driverCount,_deviceCount,_boundCount,_startedCount;
    private static UInt64 _interruptRequestBroker,_interruptReleaseBroker; private static UInt64 _nextCapabilityGrantToken=1UL,_lifecycleSequence=1UL; private static UInt64 _lifecycleSink,_deviceTreeGeneration=1UL;

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
        _driverCount=0U;_deviceCount=0U;_boundCount=0U;_startedCount=0U;_deviceTreeGeneration=1UL;_interruptRequestBroker=0UL;_interruptReleaseBroker=0UL;_initialized=true;return true;
    }

    public static Boolean IsInitialized()=>_initialized;
    public static KernelDriverCapabilities GetCapabilities()=>new(_initialized,_mode,_driverCount,_deviceCount,_boundCount,_startedCount,_driverCapacity,_deviceCapacity,_maximumDrivers,_maximumDevices,ResourcesPerDevice,_interruptRequestBroker!=0UL&&_interruptReleaseBroker!=0UL);

    public static Boolean RegisterDriver(KernelDriverMatchRule rule,KernelDriverCallbacks callbacks,out KernelDriverHandle handle)
        => RegisterDriver(null,rule,callbacks,KernelDriverCapabilityDeclaration.None,out handle);
    public static Boolean RegisterDriver(KernelDriverMatchRule rule,KernelDriverCallbacks callbacks,KernelDriverCapabilityDeclaration declaration,out KernelDriverHandle handle)
        => RegisterDriver(null,rule,callbacks,declaration,out handle);
    public static Boolean RegisterDriver(String name,KernelDriverMatchRule rule,KernelDriverCallbacks callbacks,out KernelDriverHandle handle)
        => RegisterDriver(name,rule,callbacks,KernelDriverCapabilityDeclaration.None,out handle);

    /// <summary>Registers a named driver together with the maximum privilege set it may ever request.</summary>
    public static Boolean RegisterDriver(String name,KernelDriverMatchRule rule,KernelDriverCallbacks callbacks,KernelDriverCapabilityDeclaration declaration,out KernelDriverHandle handle)
    {
        handle=default;if(!_initialized||callbacks.Probe==null||callbacks.Start==null||callbacks.Stop==null||callbacks.Remove==null)return false;
        Int32 slot=FindFreeDriver(); if(slot<0){if(!GrowDrivers())return false;slot=FindFreeDriver();if(slot<0)return false;}
        DriverRecord* r=Driver(slot);Clear((Byte*)r,sizeof(DriverRecord));r->Used=1;r->State=(Byte)KernelDriverState.Registered;r->Bus=(Byte)rule.Bus;r->MatchBus=rule.MatchBus?(Byte)1:(Byte)0;r->Vendor=rule.VendorId;r->MatchVendor=rule.MatchVendor?(Byte)1:(Byte)0;r->Device=rule.DeviceId;r->MatchDevice=rule.MatchDevice?(Byte)1:(Byte)0;r->ClassCode=rule.ClassCode;r->ClassMask=rule.ClassMask;
        CopyDriverName(r,name);r->DeclaredCapabilities=(UInt64)declaration.Capabilities;r->Discover=(UInt64)(void*)callbacks.Discover;r->Probe=(UInt64)(void*)callbacks.Probe;r->Bind=(UInt64)(void*)callbacks.Bind;r->Start=(UInt64)(void*)callbacks.Start;r->Stop=(UInt64)(void*)callbacks.Stop;r->Reset=(UInt64)(void*)callbacks.Reset;r->Suspend=(UInt64)(void*)callbacks.Suspend;r->Resume=(UInt64)(void*)callbacks.Resume;r->Remove=(UInt64)(void*)callbacks.Remove;r->Fail=(UInt64)(void*)callbacks.Fail;r->Recover=(UInt64)(void*)callbacks.Recover;r->Interrupt=(UInt64)(void*)callbacks.Interrupt;_driverCount++;handle=new KernelDriverHandle((UInt32)slot+1U);return true;
    }

    public static Boolean UnregisterDriver(KernelDriverHandle handle)
    { if(!TryDriver(handle,out DriverRecord* driver))return false;for(Int32 i=0;i<(Int32)_deviceCapacity;i++){DeviceRecord* d=Device(i);if(d->Used!=0&&d->BoundDriver==handle.Value)return false;}driver->State=(Byte)KernelDriverState.Removing;Clear((Byte*)driver,sizeof(DriverRecord));_driverCount--;return true; }

    public static Boolean RegisterDevice(KernelDeviceIdentifier identifier,out KernelDeviceHandle handle) => DiscoverDevice(identifier,default,out handle);

    /// <summary>Discovers a device and adds it to the authoritative PCI/USB/ACPI/platform/virtual/logical device tree.</summary>
    public static Boolean DiscoverDevice(KernelDeviceIdentifier identifier,KernelDeviceHandle parent,out KernelDeviceHandle handle)
    {
        handle=default;if(!_initialized||identifier.Bus==KernelDeviceBus.Unknown)return false;if(parent.Value!=0U&&!TryDevice(parent,out _))return false;Int32 slot=FindFreeDevice();if(slot<0){if(!GrowDevices())return false;slot=FindFreeDevice();if(slot<0)return false;}
        DeviceRecord* d=Device(slot);Clear((Byte*)d,sizeof(DeviceRecord));d->Used=1;d->State=(Byte)KernelDeviceState.Discovered;d->Bus=(Byte)identifier.Bus;d->Vendor=identifier.VendorId;d->Device=identifier.DeviceId;d->SubsystemVendor=identifier.SubsystemVendorId;d->Subsystem=identifier.SubsystemId;d->ClassCode=identifier.ClassCode;d->Revision=identifier.Revision;d->Location=identifier.Location;d->Parent=parent.Value;_deviceCount++;_deviceTreeGeneration++;handle=new KernelDeviceHandle((UInt32)slot+1U);
        if(parent.Value!=0U){DeviceRecord* p=Device((Int32)parent.Value-1);d->NextSibling=p->FirstChild;p->FirstChild=handle.Value;}EmitLifecycle(handle,default,KernelDriverLifecycleStage.Discover,KernelDeviceState.Registered,KernelDeviceState.Discovered,KernelDriverFailureCode.None);return true;
    }

    public static Boolean AddResource(KernelDeviceHandle device,KernelDeviceResource resource)
    { if(!TryDevice(device,out DeviceRecord* d)||!KernelDriverMath.IsValidResource(resource)||(d->State!=(Byte)KernelDeviceState.Registered&&d->State!=(Byte)KernelDeviceState.Discovered))return false;Int32 i=d->ResourceCount;if(i>=ResourcesPerDevice)return false;d->ResourceType[i]=(Byte)resource.Type;d->ResourceStart[i]=resource.Start;d->ResourceLength[i]=resource.Length;d->ResourceFlags[i]=resource.Flags;d->ResourceCount++;return true; }
    public static Boolean TryGetResource(KernelDeviceHandle device,UInt32 resourceIndex,out KernelDeviceResource resource)
    { resource=default;if(!TryDevice(device,out DeviceRecord* d)||resourceIndex>=d->ResourceCount)return false;Int32 i=(Int32)resourceIndex;resource=new KernelDeviceResource((KernelDeviceResourceType)d->ResourceType[i],d->ResourceStart[i],d->ResourceLength[i],d->ResourceFlags[i]);return true; }

    public static Boolean TryBindDevice(KernelDeviceHandle device,out KernelDriverHandle driver)
    {
        driver=default;if(!TryDevice(device,out DeviceRecord* d)||d->BoundDriver!=0U)return false;KernelDeviceIdentifier identifier=Identifier(d);
        for(Int32 i=0;i<(Int32)_driverCapacity;i++)
        {
            DriverRecord* r=Driver(i);if(r->Used==0||!KernelDriverMath.Matches(Rule(r),identifier))continue;KernelDriverHandle candidate=new((UInt32)i+1U);KernelDriverDeviceContext context=new(device,candidate,identifier);
            KernelDeviceState previous=(KernelDeviceState)d->State;d->State=(Byte)KernelDeviceState.Probing;
            if(r->Discover!=0UL){delegate*<KernelDriverDeviceContext*,Boolean> discover=(delegate*<KernelDriverDeviceContext*,Boolean>)(void*)r->Discover;if(!discover(&context)){d->State=(Byte)previous;continue;}}
            delegate*<KernelDriverDeviceContext*,Boolean> probe=(delegate*<KernelDriverDeviceContext*,Boolean>)(void*)r->Probe;if(!probe(&context)){d->State=(Byte)previous;continue;}
            d->State=(Byte)KernelDeviceState.Probed;EmitLifecycle(device,candidate,KernelDriverLifecycleStage.Probe,KernelDeviceState.Probing,KernelDeviceState.Probed,KernelDriverFailureCode.None);d->BoundDriver=candidate.Value;d->State=(Byte)KernelDeviceState.Binding;
            if(r->Bind!=0UL){delegate*<KernelDriverDeviceContext*,Boolean> bind=(delegate*<KernelDriverDeviceContext*,Boolean>)(void*)r->Bind;if(!bind(&context)){d->BoundDriver=0U;MarkFailed(d,device,candidate,KernelDriverFailureCode.BindFailed,KernelDriverLifecycleStage.Bind);continue;}}
            d->State=(Byte)KernelDeviceState.Bound;r->State=(Byte)KernelDriverState.Active;if(r->DeclaredCapabilities!=0UL&&!GrantDeclaredCapabilities(&context,d,r)){d->BoundDriver=0U;MarkFailed(d,device,candidate,KernelDriverFailureCode.CapabilityFailure,KernelDriverLifecycleStage.Bind);continue;}
            _boundCount++;EmitLifecycle(device,candidate,KernelDriverLifecycleStage.Bind,KernelDeviceState.Binding,KernelDeviceState.Bound,KernelDriverFailureCode.None);driver=candidate;return true;
        }
        return false;
    }

    /// <summary>
    /// Reconciles the authoritative device tree with all currently registered
    /// drivers. Matching devices are bound and started; unsupported devices stay
    /// discovered rather than being treated as failures.
    /// </summary>
    public static Boolean BindAndStartMatchingDevices()
    {
        if(!_initialized)return false;
        Boolean ok=true;
        for(UInt32 i=0;i<_deviceCapacity;i++)
        {
            DeviceRecord* d=Device((Int32)i);
            if(d->Used==0)continue;
            KernelDeviceHandle device=new(i+1U);

            if(d->BoundDriver!=0U)
            {
                if(d->State==(Byte)KernelDeviceState.Bound||d->State==(Byte)KernelDeviceState.Stopped)
                    ok=StartDevice(device)&ok;
                continue;
            }

            if(d->State==(Byte)KernelDeviceState.Failed)
            {
                d->State=(Byte)KernelDeviceState.Discovered;
                d->FailureCode=0U;
            }

            if(TryBindDevice(device,out _))
                ok=StartDevice(device)&ok;
        }
        return ok;
    }

    public static Boolean StartDevice(KernelDeviceHandle device)
    {
        if(!TryBound(device,out DeviceRecord* d,out DriverRecord* r,out KernelDriverDeviceContext context))return false;if(d->State==(Byte)KernelDeviceState.Started)return true;if(d->State==(Byte)KernelDeviceState.Suspended)return ResumeDevice(device);
        KernelDeviceState previous=(KernelDeviceState)d->State;d->State=(Byte)KernelDeviceState.Starting;if(r->DeclaredCapabilities!=0UL&&!AllDeclaredCapabilitiesGranted(d,r->DeclaredCapabilities)&&!GrantDeclaredCapabilities(&context,d,r)){MarkFailed(d,device,context.Driver,KernelDriverFailureCode.CapabilityFailure,KernelDriverLifecycleStage.Start);return false;}
        delegate*<KernelDriverDeviceContext*,Boolean> start=(delegate*<KernelDriverDeviceContext*,Boolean>)(void*)r->Start;if(!start(&context)){RevokeAllCapabilities(d);d->State=(Byte)KernelDeviceState.Bound;d->FailureCode=(UInt32)KernelDriverFailureCode.StartFailed;EmitLifecycle(device,context.Driver,KernelDriverLifecycleStage.Start,KernelDeviceState.Starting,KernelDeviceState.Bound,KernelDriverFailureCode.StartFailed);return false;}d->State=(Byte)KernelDeviceState.Started;d->FailureCode=0U;_startedCount++;EmitLifecycle(device,context.Driver,KernelDriverLifecycleStage.Start,previous,KernelDeviceState.Started,KernelDriverFailureCode.None);return true;
    }
    public static Boolean StopDevice(KernelDeviceHandle device)
    {
        if(!TryBound(device,out DeviceRecord* d,out DriverRecord* r,out KernelDriverDeviceContext context))return false;if(d->State!=(Byte)KernelDeviceState.Started&&d->State!=(Byte)KernelDeviceState.Suspended){RevokeAllCapabilities(d);return true;}
        KernelDeviceState previous=(KernelDeviceState)d->State;d->State=(Byte)KernelDeviceState.Stopping;delegate*<KernelDriverDeviceContext*,Boolean> stop=(delegate*<KernelDriverDeviceContext*,Boolean>)(void*)r->Stop;if(!stop(&context)){MarkFailed(d,device,context.Driver,KernelDriverFailureCode.StopFailed,KernelDriverLifecycleStage.Stop);return false;}d->State=(Byte)KernelDeviceState.Stopped;if(previous==KernelDeviceState.Started&&_startedCount>0U)_startedCount--;RevokeAllCapabilities(d);EmitLifecycle(device,context.Driver,KernelDriverLifecycleStage.Stop,previous,KernelDeviceState.Stopped,KernelDriverFailureCode.None);return true;
    }
    public static Boolean ResetDevice(KernelDeviceHandle device)
    {
        if(!TryBound(device,out DeviceRecord* d,out DriverRecord* r,out KernelDriverDeviceContext context)||r->Reset==0UL)return false;KernelDeviceState previous=(KernelDeviceState)d->State;d->State=(Byte)KernelDeviceState.Resetting;delegate*<KernelDriverDeviceContext*,Boolean> reset=(delegate*<KernelDriverDeviceContext*,Boolean>)(void*)r->Reset;if(!reset(&context)){MarkFailed(d,device,context.Driver,KernelDriverFailureCode.ResetFailed,KernelDriverLifecycleStage.Reset);return false;}d->State=(Byte)(previous==KernelDeviceState.Started?KernelDeviceState.Started:KernelDeviceState.Bound);d->FailureCode=0U;EmitLifecycle(device,context.Driver,KernelDriverLifecycleStage.Reset,previous,(KernelDeviceState)d->State,KernelDriverFailureCode.None);return true;
    }
    public static Boolean SuspendDevice(KernelDeviceHandle device)
    {
        if(!TryBound(device,out DeviceRecord* d,out DriverRecord* r,out KernelDriverDeviceContext context)||d->State!=(Byte)KernelDeviceState.Started)return false;d->State=(Byte)KernelDeviceState.Suspending;if(r->Suspend!=0UL){delegate*<KernelDriverDeviceContext*,Boolean> suspend=(delegate*<KernelDriverDeviceContext*,Boolean>)(void*)r->Suspend;if(!suspend(&context)){MarkFailed(d,device,context.Driver,KernelDriverFailureCode.SuspendFailed,KernelDriverLifecycleStage.Suspend);return false;}}d->State=(Byte)KernelDeviceState.Suspended;r->State=(Byte)KernelDriverState.Suspended;if(_startedCount>0U)_startedCount--;EmitLifecycle(device,context.Driver,KernelDriverLifecycleStage.Suspend,KernelDeviceState.Started,KernelDeviceState.Suspended,KernelDriverFailureCode.None);return true;
    }
    public static Boolean ResumeDevice(KernelDeviceHandle device)
    {
        if(!TryBound(device,out DeviceRecord* d,out DriverRecord* r,out KernelDriverDeviceContext context)||d->State!=(Byte)KernelDeviceState.Suspended)return false;d->State=(Byte)KernelDeviceState.Resuming;if(r->DeclaredCapabilities!=0UL&&!AllDeclaredCapabilitiesGranted(d,r->DeclaredCapabilities)&&!GrantDeclaredCapabilities(&context,d,r)){MarkFailed(d,device,context.Driver,KernelDriverFailureCode.CapabilityFailure,KernelDriverLifecycleStage.Resume);return false;}if(r->Resume!=0UL){delegate*<KernelDriverDeviceContext*,Boolean> resume=(delegate*<KernelDriverDeviceContext*,Boolean>)(void*)r->Resume;if(!resume(&context)){MarkFailed(d,device,context.Driver,KernelDriverFailureCode.ResumeFailed,KernelDriverLifecycleStage.Resume);return false;}}d->State=(Byte)KernelDeviceState.Started;r->State=(Byte)KernelDriverState.Active;_startedCount++;EmitLifecycle(device,context.Driver,KernelDriverLifecycleStage.Resume,KernelDeviceState.Suspended,KernelDeviceState.Started,KernelDriverFailureCode.None);return true;
    }
    public static Boolean FailDevice(KernelDeviceHandle device,KernelDriverFailureCode failure)
    { if(failure==KernelDriverFailureCode.None||!TryBound(device,out DeviceRecord* d,out _,out KernelDriverDeviceContext context))return false;MarkFailed(d,device,context.Driver,failure,KernelDriverLifecycleStage.Fail);return true; }
    public static Boolean RecoverDevice(KernelDeviceHandle device)
    {
        if(!TryBound(device,out DeviceRecord* d,out DriverRecord* r,out KernelDriverDeviceContext context)||d->State!=(Byte)KernelDeviceState.Failed)return false;KernelDriverFailureCode failure=(KernelDriverFailureCode)d->FailureCode;d->State=(Byte)KernelDeviceState.Recovering;r->State=(Byte)KernelDriverState.Recovering;if(r->Recover!=0UL){delegate*<KernelDriverDeviceContext*,Boolean> recover=(delegate*<KernelDriverDeviceContext*,Boolean>)(void*)r->Recover;if(!recover(&context)){d->State=(Byte)KernelDeviceState.Failed;r->State=(Byte)KernelDriverState.Failed;return false;}}d->State=(Byte)KernelDeviceState.Bound;d->FailureCode=0U;r->State=(Byte)KernelDriverState.Active;EmitLifecycle(device,context.Driver,KernelDriverLifecycleStage.Recover,KernelDeviceState.Failed,KernelDeviceState.Bound,failure);return true;
    }
    public static Boolean RemoveDevice(KernelDeviceHandle device)
    {
        if(!TryDevice(device,out DeviceRecord* d))return false;UInt32 bound=d->BoundDriver;KernelDeviceState previous=(KernelDeviceState)d->State;if(bound!=0U){if((d->State==(Byte)KernelDeviceState.Started||d->State==(Byte)KernelDeviceState.Suspended)&&!StopDevice(device))return false;DriverRecord* r=Driver((Int32)bound-1);KernelDriverDeviceContext context=new(device,new KernelDriverHandle(bound),Identifier(d));d->State=(Byte)KernelDeviceState.Removing;delegate*<KernelDriverDeviceContext*,Boolean> remove=(delegate*<KernelDriverDeviceContext*,Boolean>)(void*)r->Remove;if(!remove(&context))return false;RevokeAllCapabilities(d);if(_boundCount>0U)_boundCount--;EmitLifecycle(device,new KernelDriverHandle(bound),KernelDriverLifecycleStage.Remove,previous,KernelDeviceState.Removed,KernelDriverFailureCode.None);}UnlinkFromParent(device,d);Clear((Byte*)d,sizeof(DeviceRecord));_deviceCount--;_deviceTreeGeneration++;return true;
    }
    public static Boolean TryGetDevice(KernelDeviceHandle device,out KernelDeviceIdentifier identifier,out KernelDeviceState state,out KernelDriverHandle driver)
    { identifier=default;state=default;driver=default;if(!TryDevice(device,out DeviceRecord* d))return false;identifier=Identifier(d);state=(KernelDeviceState)d->State;driver=new KernelDriverHandle(d->BoundDriver);return true; }
    public static Boolean TryGetDeviceNode(KernelDeviceHandle device,out KernelDeviceNode node)
    { node=default;if(!TryDevice(device,out DeviceRecord* d))return false;node=new KernelDeviceNode(device,new KernelDeviceHandle(d->Parent),new KernelDeviceHandle(d->FirstChild),new KernelDeviceHandle(d->NextSibling),Identifier(d),(KernelDeviceState)d->State,new KernelDriverHandle(d->BoundDriver),(KernelDriverFailureCode)d->FailureCode);return true; }
    public static Boolean TryGetFirstChild(KernelDeviceHandle parent,out KernelDeviceHandle child){child=default;if(!TryDevice(parent,out DeviceRecord* d)||d->FirstChild==0U)return false;child=new KernelDeviceHandle(d->FirstChild);return true;}
    public static Boolean TryGetNextSibling(KernelDeviceHandle device,out KernelDeviceHandle sibling){sibling=default;if(!TryDevice(device,out DeviceRecord* d)||d->NextSibling==0U)return false;sibling=new KernelDeviceHandle(d->NextSibling);return true;}

    /// <summary>Gets a stable slot-ordered node so tooling can snapshot the exact kernel device tree without bus-specific enumeration.</summary>
    public static Boolean TryGetDeviceNodeByIndex(UInt32 index,out KernelDeviceNode node)
    { node=default;if(!_initialized)return false;UInt32 seen=0U;for(UInt32 i=0U;i<_deviceCapacity;i++){DeviceRecord* d=Device((Int32)i);if(d->Used==0)continue;if(seen++!=index)continue;KernelDeviceHandle h=new(i+1U);return TryGetDeviceNode(h,out node);}return false; }

    /// <summary>Gets a root node by root index. A root has no parent; children are linked through the same node contract.</summary>
    public static Boolean TryGetRootDevice(UInt32 rootIndex,out KernelDeviceNode node)
    { node=default;if(!_initialized)return false;UInt32 seen=0U;for(UInt32 i=0U;i<_deviceCapacity;i++){DeviceRecord* d=Device((Int32)i);if(d->Used==0||d->Parent!=0U)continue;if(seen++!=rootIndex)continue;return TryGetDeviceNode(new KernelDeviceHandle(i+1U),out node);}return false; }

    /// <summary>Returns counts for the six canonical device classes represented by the authoritative tree.</summary>
    public static KernelDeviceTreeSnapshot GetDeviceTreeSnapshot()
    { UInt32 roots=0,pci=0,usb=0,acpi=0,platform=0,virtuals=0,logical=0;if(_initialized)for(UInt32 i=0U;i<_deviceCapacity;i++){DeviceRecord* d=Device((Int32)i);if(d->Used==0)continue;if(d->Parent==0U)roots++;switch((KernelDeviceBus)d->Bus){case KernelDeviceBus.Pci:pci++;break;case KernelDeviceBus.Usb:usb++;break;case KernelDeviceBus.Acpi:acpi++;break;case KernelDeviceBus.Platform:platform++;break;case KernelDeviceBus.Virtual:virtuals++;break;case KernelDeviceBus.Logical:logical++;break;}}return new KernelDeviceTreeSnapshot(_deviceTreeGeneration,_deviceCount,roots,pci,usb,acpi,platform,virtuals,logical); }
    public static Boolean InstallLifecycleSink(delegate*<KernelDriverLifecycleEvent*,Boolean> sink){_lifecycleSink=(UInt64)(void*)sink;return sink!=null;}

    /// <summary>Gets a snapshot of one registered driver by handle.</summary>
    public static Boolean TryGetDriverInfo(KernelDriverHandle driver,out KernelDriverInfo info)
    { info=default;if(!TryDriver(driver,out DriverRecord* r))return false;info=new KernelDriverInfo(driver,(KernelDriverState)r->State,Rule(r),(KernelDriverCapability)r->DeclaredCapabilities,r->NameLength);return true; }
    /// <summary>Reads one ASCII byte from a driver's stable display name without allocating a managed string.</summary>
    public static Boolean TryGetDriverNameByte(KernelDriverHandle driver,UInt32 index,out Byte value)
    { value=0;if(!TryDriver(driver,out DriverRecord* r)||index>=r->NameLength)return false;value=r->Name[index];return true; }

    /// <summary>Gets the privilege declaration registered for a driver.</summary>
    public static Boolean TryGetDeclaredCapabilities(KernelDriverHandle driver,out KernelDriverCapabilityDeclaration declaration)
    { declaration=default;if(!TryDriver(driver,out DriverRecord* r))return false;declaration=new KernelDriverCapabilityDeclaration((KernelDriverCapability)r->DeclaredCapabilities);return true; }


    /// <summary>Gets whether a bound driver currently owns a live grant for the requested capability.</summary>
    public static Boolean HasCapabilityGrant(KernelDriverDeviceContext context,KernelDriverCapability capability)
    {
        if(!KernelDriverMath.IsSingleCapability(capability)||!TryBound(context.Device,out DeviceRecord* d,out _,out KernelDriverDeviceContext actual)||actual.Driver.Value!=context.Driver.Value)return false;
        UInt64 bit=(UInt64)capability;for(Int32 i=0;i<d->GrantCount;i++)if((d->GrantCapability[i]&bit)==bit)return true;return false;
    }

    /// <summary>Returns one live grant owned by a bound driver for a requested capability.</summary>
    public static Boolean TryGetCapabilityGrant(KernelDriverDeviceContext context,KernelDriverCapability capability,out KernelDriverCapabilityGrant grant)
    {
        grant=default;if(!KernelDriverMath.IsSingleCapability(capability)||!TryBound(context.Device,out DeviceRecord* d,out _,out KernelDriverDeviceContext actual)||actual.Driver.Value!=context.Driver.Value)return false;
        UInt64 bit=(UInt64)capability;for(Int32 i=0;i<d->GrantCount;i++)if((d->GrantCapability[i]&bit)==bit){grant=new KernelDriverCapabilityGrant(d->GrantToken[i],context.Device,context.Driver,capability,d->GrantStart[i],d->GrantLength[i],(KernelDriverCapabilityAccess)d->GrantAccess[i]);return true;}return false;
    }

    /// <summary>Applies kernel policy to the complete declaration when a driver binds. Declarations are ceilings; grants are the actual authority.</summary>
    private static Boolean GrantDeclaredCapabilities(KernelDriverDeviceContext* context,DeviceRecord* d,DriverRecord* r)
    {
        if(context==null)return false;UInt64 declared=r->DeclaredCapabilities;
        if(!GrantGlobalIfDeclared(*context,declared,KernelDriverCapability.PciConfig))return false;
        if(!GrantGlobalIfDeclared(*context,declared,KernelDriverCapability.Timers))return false;
        if(!GrantGlobalIfDeclared(*context,declared,KernelDriverCapability.Networking))return false;
        if(!GrantGlobalIfDeclared(*context,declared,KernelDriverCapability.Filesystem))return false;
        if(!GrantGlobalIfDeclared(*context,declared,KernelDriverCapability.Dma))return false;
        for(Int32 i=0;i<d->ResourceCount;i++)
        {
            KernelDeviceResourceType type=(KernelDeviceResourceType)d->ResourceType[i];UInt64 start=d->ResourceStart[i],length=d->ResourceLength[i];
            if(type==KernelDeviceResourceType.Memory)
            {
                if((declared&(UInt64)KernelDriverCapability.Mmio)!=0UL&&!TryGrantCapability(*context,new KernelDriverCapabilityRequest(KernelDriverCapability.Mmio,start,length,KernelDriverCapabilityAccess.ReadWrite),out _))return false;
                if((declared&(UInt64)KernelDriverCapability.PhysicalMemory)!=0UL&&!TryGrantCapability(*context,new KernelDriverCapabilityRequest(KernelDriverCapability.PhysicalMemory,start,length,KernelDriverCapabilityAccess.ReadWrite),out _))return false;
            }
            else if(type==KernelDeviceResourceType.IoPort&&(declared&(UInt64)KernelDriverCapability.PortIo)!=0UL)
            { if(!TryGrantCapability(*context,new KernelDriverCapabilityRequest(KernelDriverCapability.PortIo,start,length,KernelDriverCapabilityAccess.ReadWrite),out _))return false; }
        }
        if((declared&(UInt64)KernelDriverCapability.Interrupt)!=0UL&&!TryGrantCapability(*context,new KernelDriverCapabilityRequest(KernelDriverCapability.Interrupt,0UL,0UL,KernelDriverCapabilityAccess.ReadWrite),out _))return false;
        if((declared&(UInt64)KernelDriverCapability.Msi)!=0UL&&!TryGrantCapability(*context,new KernelDriverCapabilityRequest(KernelDriverCapability.Msi,0UL,0UL,KernelDriverCapabilityAccess.ReadWrite),out _))return false;
        if((declared&(UInt64)KernelDriverCapability.MsiX)!=0UL&&!TryGrantCapability(*context,new KernelDriverCapabilityRequest(KernelDriverCapability.MsiX,0UL,0UL,KernelDriverCapabilityAccess.ReadWrite),out _))return false;
        return AllDeclaredCapabilitiesGranted(d,declared);
    }

    private static Boolean AllDeclaredCapabilitiesGranted(DeviceRecord* d,UInt64 declared)
    {
        UInt64 granted=0UL;for(Int32 i=0;i<d->GrantCount;i++)granted|=d->GrantCapability[i];return (granted&declared)==declared;
    }

    private static Boolean GrantGlobalIfDeclared(KernelDriverDeviceContext context,UInt64 declared,KernelDriverCapability capability)
    { if((declared&(UInt64)capability)==0UL)return true;return TryGrantCapability(context,new KernelDriverCapabilityRequest(capability,0UL,0UL,KernelDriverCapabilityAccess.ReadWrite),out _); }

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
    { return ValidateCapabilityGrant(grant,grant.Capability,grant.Start,grant.Length,grant.Access); }

    /// <summary>Validates a grant for a concrete operation, including capability kind, range and read/write authority.</summary>
    public static Boolean ValidateCapabilityGrant(KernelDriverCapabilityGrant grant,KernelDriverCapability requiredCapability,UInt64 start,UInt64 length,KernelDriverCapabilityAccess requiredAccess)
    {
        if(!grant.IsValid||!KernelDriverMath.IsSingleCapability(requiredCapability)||!KernelDriverMath.AccessCovers(grant.Access,requiredAccess)||grant.Capability!=requiredCapability||!TryDevice(grant.Device,out DeviceRecord* d)||d->BoundDriver!=grant.Driver.Value)return false;
        Boolean ranged=requiredCapability==KernelDriverCapability.Mmio||requiredCapability==KernelDriverCapability.PortIo||requiredCapability==KernelDriverCapability.PhysicalMemory;
        if(ranged&&!KernelDriverMath.RangeContains(grant.Start,grant.Length,start,length))return false;if(!ranged&&(start!=0UL||length!=0UL))return false;
        for(Int32 i=0;i<d->GrantCount;i++)if(d->GrantToken[i]==grant.Token&&d->GrantCapability[i]==(UInt64)grant.Capability&&d->GrantStart[i]==grant.Start&&d->GrantLength[i]==grant.Length&&d->GrantAccess[i]==(Byte)grant.Access)return true;return false;
    }

    /// <summary>Returns a live grant that authorizes a specific operation for the bound driver.</summary>
    public static Boolean TryGetCapabilityGrant(KernelDriverDeviceContext context,KernelDriverCapability capability,UInt64 start,UInt64 length,KernelDriverCapabilityAccess access,out KernelDriverCapabilityGrant grant)
    {
        grant=default;if(!KernelDriverMath.IsSingleCapability(capability)||!TryBound(context.Device,out DeviceRecord* d,out _,out KernelDriverDeviceContext actual)||actual.Driver.Value!=context.Driver.Value)return false;
        UInt64 bit=(UInt64)capability;for(Int32 i=0;i<d->GrantCount;i++){if((d->GrantCapability[i]&bit)!=bit)continue;KernelDriverCapabilityGrant candidate=new(d->GrantToken[i],context.Device,context.Driver,capability,d->GrantStart[i],d->GrantLength[i],(KernelDriverCapabilityAccess)d->GrantAccess[i]);if(ValidateCapabilityGrant(candidate,capability,start,length,access)){grant=candidate;return true;}}return false;
    }

    /// <summary>Revokes a previously issued capability token.</summary>
    public static Boolean RevokeCapability(KernelDriverCapabilityGrant grant)
    { if(!grant.IsValid||!TryDevice(grant.Device,out DeviceRecord* d)||d->BoundDriver!=grant.Driver.Value)return false;for(Int32 i=0;i<d->GrantCount;i++){if(d->GrantToken[i]!=grant.Token)continue;Int32 last=d->GrantCount-1;d->GrantToken[i]=d->GrantToken[last];d->GrantCapability[i]=d->GrantCapability[last];d->GrantStart[i]=d->GrantStart[last];d->GrantLength[i]=d->GrantLength[last];d->GrantAccess[i]=d->GrantAccess[last];d->GrantToken[last]=0UL;d->GrantCount--;return true;}return false; }

    private static void RevokeAllCapabilities(DeviceRecord* d)
    {
        if(d==null)return;for(Int32 i=0;i<d->GrantCount;i++){d->GrantToken[i]=0UL;d->GrantCapability[i]=0UL;d->GrantStart[i]=0UL;d->GrantLength[i]=0UL;d->GrantAccess[i]=0;}d->GrantCount=0;
    }

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

    private static void MarkFailed(DeviceRecord* d,KernelDeviceHandle device,KernelDriverHandle driver,KernelDriverFailureCode failure,KernelDriverLifecycleStage stage)
    { KernelDeviceState previous=(KernelDeviceState)d->State;d->State=(Byte)KernelDeviceState.Failed;d->FailureCode=(UInt32)failure;RevokeAllCapabilities(d);if(driver.Value!=0U){DriverRecord* r=Driver((Int32)driver.Value-1);r->State=(Byte)KernelDriverState.Failed;if(r->Fail!=0UL){KernelDriverDeviceContext context=new(device,driver,Identifier(d));delegate*<KernelDriverDeviceContext*,KernelDriverFailureCode,Boolean> fail=(delegate*<KernelDriverDeviceContext*,KernelDriverFailureCode,Boolean>)(void*)r->Fail;fail(&context,failure);}}EmitLifecycle(device,driver,stage,previous,KernelDeviceState.Failed,failure); }
    private static void EmitLifecycle(KernelDeviceHandle device,KernelDriverHandle driver,KernelDriverLifecycleStage stage,KernelDeviceState previous,KernelDeviceState current,KernelDriverFailureCode failure)
    { if(_lifecycleSink==0UL)return;KernelDriverLifecycleEvent e=new(device,driver,stage,previous,current,failure,_lifecycleSequence++);delegate*<KernelDriverLifecycleEvent*,Boolean> sink=(delegate*<KernelDriverLifecycleEvent*,Boolean>)(void*)_lifecycleSink;sink(&e); }
    private static void UnlinkFromParent(KernelDeviceHandle handle,DeviceRecord* d)
    { if(d->Parent==0U)return;DeviceRecord* p=Device((Int32)d->Parent-1);UInt32 cur=p->FirstChild,prev=0U;while(cur!=0U){DeviceRecord* c=Device((Int32)cur-1);if(cur==handle.Value){if(prev==0U)p->FirstChild=c->NextSibling;else Device((Int32)prev-1)->NextSibling=c->NextSibling;return;}prev=cur;cur=c->NextSibling;} }

    private static Int32 FindFreeDriver(){for(Int32 i=0;i<(Int32)_driverCapacity;i++)if(Driver(i)->Used==0)return i;return -1;}
    private static Int32 FindFreeDevice(){for(Int32 i=0;i<(Int32)_deviceCapacity;i++)if(Device(i)->Used==0)return i;return -1;}
    private static Boolean GrowDrivers(){if(_mode!=KernelDriverRegistryMode.Dynamic)return false;UInt32 next=KernelDriverMath.NextCapacity(_driverCapacity,_maximumDrivers);if(next<=_driverCapacity)return false;if(!AllocateDriverTable(next,out KernelHeapAllocation allocation,out DriverRecord* table))return false;Copy((Byte*)_drivers,(Byte*)table,(UInt64)_driverCapacity*(UInt64)sizeof(DriverRecord));KernelHeapAllocation old=_driverAllocation;if(!KernelHeap.TryRelease(old)){KernelHeap.TryRelease(allocation);return false;}_driverAllocation=allocation;_drivers=table;_driverCapacity=next;return true;}
    private static Boolean GrowDevices(){if(_mode!=KernelDriverRegistryMode.Dynamic)return false;UInt32 next=KernelDriverMath.NextCapacity(_deviceCapacity,_maximumDevices);if(next<=_deviceCapacity)return false;if(!AllocateDeviceTable(next,out KernelHeapAllocation allocation,out DeviceRecord* table))return false;Copy((Byte*)_devices,(Byte*)table,(UInt64)_deviceCapacity*(UInt64)sizeof(DeviceRecord));KernelHeapAllocation old=_deviceAllocation;if(!KernelHeap.TryRelease(old)){KernelHeap.TryRelease(allocation);return false;}_deviceAllocation=allocation;_devices=table;_deviceCapacity=next;return true;}
    private static Boolean AllocateDriverTable(UInt32 capacity,out KernelHeapAllocation allocation,out DriverRecord* table){allocation=default;table=null;UInt64 bytes=(UInt64)capacity*(UInt64)sizeof(DriverRecord);if(!KernelHeap.TryAllocate(bytes,64UL,true,out allocation))return false;table=(DriverRecord*)(nuint)allocation.Address;return true;}
    private static Boolean AllocateDeviceTable(UInt32 capacity,out KernelHeapAllocation allocation,out DeviceRecord* table){allocation=default;table=null;UInt64 bytes=(UInt64)capacity*(UInt64)sizeof(DeviceRecord);if(!KernelHeap.TryAllocate(bytes,64UL,true,out allocation))return false;table=(DeviceRecord*)(nuint)allocation.Address;return true;}
    private static Boolean TryDriver(KernelDriverHandle handle,out DriverRecord* record){record=null;Int32 i=(Int32)handle.Value-1;if(!_initialized||i<0||(UInt32)i>=_driverCapacity)return false;DriverRecord* r=Driver(i);if(r->Used==0)return false;record=r;return true;}
    private static Boolean TryDevice(KernelDeviceHandle handle,out DeviceRecord* record){record=null;Int32 i=(Int32)handle.Value-1;if(!_initialized||i<0||(UInt32)i>=_deviceCapacity)return false;DeviceRecord* d=Device(i);if(d->Used==0)return false;record=d;return true;}
    private static Boolean TryBound(KernelDeviceHandle device,out DeviceRecord* d,out DriverRecord* r,out KernelDriverDeviceContext context){r=null;context=default;if(!TryDevice(device,out d)||d->BoundDriver==0U)return false;r=Driver((Int32)d->BoundDriver-1);if(r->Used==0)return false;context=new KernelDriverDeviceContext(device,new KernelDriverHandle(d->BoundDriver),Identifier(d));return true;}
    private static void CopyDriverName(DriverRecord* r,String name){if(r==null||name==null)return;Int32 n=name.Length;if(n>DriverNameBytes)n=DriverNameBytes;for(Int32 i=0;i<n;i++){Char c=name[i];r->Name[i]=(Byte)(c>=32&&c<=126?c:'?');}r->NameLength=(Byte)n;}
    private static KernelDeviceIdentifier Identifier(DeviceRecord* d)=>new((KernelDeviceBus)d->Bus,d->Vendor,d->Device,d->SubsystemVendor,d->Subsystem,d->ClassCode,d->Revision,d->Location);
    private static KernelDriverMatchRule Rule(DriverRecord* r)=>new((KernelDeviceBus)r->Bus,r->MatchBus!=0,r->Vendor,r->MatchVendor!=0,r->Device,r->MatchDevice!=0,r->ClassCode,r->ClassMask);
    private static DriverRecord* Driver(Int32 slot)=>_drivers+slot; private static DeviceRecord* Device(Int32 slot)=>_devices+slot;
    private static void Clear(Byte* p,Int32 bytes){for(Int32 i=0;i<bytes;i++)p[i]=0;} private static void Copy(Byte* source,Byte* target,UInt64 bytes){for(UInt64 i=0UL;i<bytes;i++)target[i]=source[i];}
}
