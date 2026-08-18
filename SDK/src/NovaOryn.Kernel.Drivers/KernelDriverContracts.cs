using System;

namespace NovaOryn.Kernel.Drivers;

public enum KernelDeviceBus : Byte { Unknown=0, Platform=1, Pci=2, Usb=3, Virtio=4, Acpi=5, Synthetic=6 }
public enum KernelDeviceResourceType : Byte { None=0, Memory=1, IoPort=2, Interrupt=3, Dma=4, BusSpecific=5 }
public enum KernelDeviceState : Byte { Registered=1, Probed=2, Bound=3, Started=4, Stopped=5, Failed=6 }
public enum KernelDriverState : Byte { Registered=1, Active=2, Removing=3 }

/// <summary>Selects whether driver/device tables grow from the kernel heap or stay at an explicitly fixed capacity.</summary>
public enum KernelDriverRegistryMode : Byte { Dynamic=0, Fixed=1 }

/// <summary>Configures driver/device registry storage without requiring record/runtime helper support.</summary>
public readonly struct KernelDriverFrameworkOptions
{
    public KernelDriverFrameworkOptions(KernelDriverRegistryMode registryMode, UInt32 initialDriverCapacity, UInt32 initialDeviceCapacity, UInt32 maximumDriverCapacity, UInt32 maximumDeviceCapacity)
    { RegistryMode=registryMode; InitialDriverCapacity=initialDriverCapacity; InitialDeviceCapacity=initialDeviceCapacity; MaximumDriverCapacity=maximumDriverCapacity; MaximumDeviceCapacity=maximumDeviceCapacity; }
    public KernelDriverRegistryMode RegistryMode { get; }
    public UInt32 InitialDriverCapacity { get; }
    public UInt32 InitialDeviceCapacity { get; }
    public UInt32 MaximumDriverCapacity { get; }
    public UInt32 MaximumDeviceCapacity { get; }
    public static KernelDriverFrameworkOptions DynamicDefault => new(KernelDriverRegistryMode.Dynamic, 64U, 128U, UInt32.MaxValue, UInt32.MaxValue);
    public static KernelDriverFrameworkOptions Fixed(UInt32 drivers, UInt32 devices) => new(KernelDriverRegistryMode.Fixed, drivers, devices, drivers, devices);
}

public readonly struct KernelDeviceIdentifier
{
    public KernelDeviceIdentifier(KernelDeviceBus bus, UInt16 vendorId, UInt16 deviceId, UInt16 subsystemVendorId, UInt16 subsystemId, UInt32 classCode, Byte revision, UInt32 location)
    { Bus=bus; VendorId=vendorId; DeviceId=deviceId; SubsystemVendorId=subsystemVendorId; SubsystemId=subsystemId; ClassCode=classCode; Revision=revision; Location=location; }
    public KernelDeviceBus Bus { get; }
    public UInt16 VendorId { get; }
    public UInt16 DeviceId { get; }
    public UInt16 SubsystemVendorId { get; }
    public UInt16 SubsystemId { get; }
    public UInt32 ClassCode { get; }
    public Byte Revision { get; }
    public UInt32 Location { get; }
}

public readonly struct KernelDeviceResource
{
    public KernelDeviceResource(KernelDeviceResourceType type, UInt64 start, UInt64 length, UInt64 flags) { Type=type; Start=start; Length=length; Flags=flags; }
    public KernelDeviceResourceType Type { get; }
    public UInt64 Start { get; }
    public UInt64 Length { get; }
    public UInt64 Flags { get; }
}

public readonly struct KernelDriverMatchRule
{
    public KernelDriverMatchRule(KernelDeviceBus bus, Boolean matchBus, UInt16 vendorId, Boolean matchVendor, UInt16 deviceId, Boolean matchDevice, UInt32 classCode, UInt32 classMask)
    { Bus=bus; MatchBus=matchBus; VendorId=vendorId; MatchVendor=matchVendor; DeviceId=deviceId; MatchDevice=matchDevice; ClassCode=classCode; ClassMask=classMask; }
    public KernelDeviceBus Bus { get; }
    public Boolean MatchBus { get; }
    public UInt16 VendorId { get; }
    public Boolean MatchVendor { get; }
    public UInt16 DeviceId { get; }
    public Boolean MatchDevice { get; }
    public UInt32 ClassCode { get; }
    public UInt32 ClassMask { get; }
}

public readonly struct KernelDriverHandle { public KernelDriverHandle(UInt32 value) { Value=value; } public UInt32 Value { get; } }
public readonly struct KernelDeviceHandle { public KernelDeviceHandle(UInt32 value) { Value=value; } public UInt32 Value { get; } }
public readonly struct KernelDriverInterruptHandle { public KernelDriverInterruptHandle(UInt64 value) { Value=value; } public UInt64 Value { get; } }

[Flags]
public enum KernelDriverCapability : UInt64
{
    None=0UL,
    Mmio=1UL<<0,
    PortIo=1UL<<1,
    Interrupt=1UL<<2,
    Msi=1UL<<3,
    MsiX=1UL<<4,
    Dma=1UL<<5,
    PciConfig=1UL<<6,
    PhysicalMemory=1UL<<7,
    Timers=1UL<<8,
    Networking=1UL<<9,
    Filesystem=1UL<<10
}

public enum KernelDriverCapabilityAccess : Byte { None=0, Read=1, Write=2, ReadWrite=3, Execute=4 }

/// <summary>Declares the complete privilege set a driver is allowed to request.</summary>
public readonly struct KernelDriverCapabilityDeclaration
{
    public KernelDriverCapabilityDeclaration(KernelDriverCapability capabilities) { Capabilities=capabilities; }
    public KernelDriverCapability Capabilities { get; }
    public Boolean Contains(KernelDriverCapability capability) => capability!=KernelDriverCapability.None && (Capabilities&capability)==capability;
    public static KernelDriverCapabilityDeclaration None => new(KernelDriverCapability.None);
}

/// <summary>Requests one concrete capability. Range is mandatory for MMIO, port I/O and physical-memory grants.</summary>
public readonly struct KernelDriverCapabilityRequest
{
    public KernelDriverCapabilityRequest(KernelDriverCapability capability, UInt64 start, UInt64 length, KernelDriverCapabilityAccess access, UInt64 flags=0UL)
    { Capability=capability; Start=start; Length=length; Access=access; Flags=flags; }
    public KernelDriverCapability Capability { get; }
    public UInt64 Start { get; }
    public UInt64 Length { get; }
    public KernelDriverCapabilityAccess Access { get; }
    public UInt64 Flags { get; }
}

/// <summary>Opaque kernel-issued proof that a driver/device binding was granted one capability.</summary>
public readonly struct KernelDriverCapabilityGrant
{
    public KernelDriverCapabilityGrant(UInt64 token, KernelDeviceHandle device, KernelDriverHandle driver, KernelDriverCapability capability, UInt64 start, UInt64 length, KernelDriverCapabilityAccess access)
    { Token=token; Device=device; Driver=driver; Capability=capability; Start=start; Length=length; Access=access; }
    public UInt64 Token { get; }
    public KernelDeviceHandle Device { get; }
    public KernelDriverHandle Driver { get; }
    public KernelDriverCapability Capability { get; }
    public UInt64 Start { get; }
    public UInt64 Length { get; }
    public KernelDriverCapabilityAccess Access { get; }
    public Boolean IsValid => Token!=0UL;
}


public readonly struct KernelDriverDeviceContext
{
    public KernelDriverDeviceContext(KernelDeviceHandle device, KernelDriverHandle driver, KernelDeviceIdentifier identifier) { Device=device; Driver=driver; Identifier=identifier; }
    public KernelDeviceHandle Device { get; }
    public KernelDriverHandle Driver { get; }
    public KernelDeviceIdentifier Identifier { get; }
}

public readonly struct KernelDriverInterruptRequest
{
    public KernelDriverInterruptRequest(KernelDeviceHandle device, UInt32 source, Byte priority, UInt32 targetProcessor, Boolean levelTriggered, Boolean activeLow, UInt64 driverCookie)
    { Device=device; Source=source; Priority=priority; TargetProcessor=targetProcessor; LevelTriggered=levelTriggered; ActiveLow=activeLow; DriverCookie=driverCookie; }
    public KernelDeviceHandle Device { get; }
    public UInt32 Source { get; }
    public Byte Priority { get; }
    public UInt32 TargetProcessor { get; }
    public Boolean LevelTriggered { get; }
    public Boolean ActiveLow { get; }
    public UInt64 DriverCookie { get; }
}

/// <summary>Reports registry mode, current heap-backed capacity, configured limits, and current use.</summary>
public readonly struct KernelDriverCapabilities
{
    public KernelDriverCapabilities(Boolean initialized, KernelDriverRegistryMode registryMode, UInt32 registeredDrivers, UInt32 registeredDevices, UInt32 boundDevices, UInt32 startedDevices, UInt32 driverCapacity, UInt32 deviceCapacity, UInt32 maximumDrivers, UInt32 maximumDevices, UInt32 maximumResourcesPerDevice, Boolean interruptBrokerInstalled)
    { Initialized=initialized; RegistryMode=registryMode; RegisteredDrivers=registeredDrivers; RegisteredDevices=registeredDevices; BoundDevices=boundDevices; StartedDevices=startedDevices; DriverCapacity=driverCapacity; DeviceCapacity=deviceCapacity; MaximumDrivers=maximumDrivers; MaximumDevices=maximumDevices; MaximumResourcesPerDevice=maximumResourcesPerDevice; InterruptBrokerInstalled=interruptBrokerInstalled; }
    public Boolean Initialized { get; }
    public KernelDriverRegistryMode RegistryMode { get; }
    public UInt32 RegisteredDrivers { get; }
    public UInt32 RegisteredDevices { get; }
    public UInt32 BoundDevices { get; }
    public UInt32 StartedDevices { get; }
    public UInt32 DriverCapacity { get; }
    public UInt32 DeviceCapacity { get; }
    public UInt32 MaximumDrivers { get; }
    public UInt32 MaximumDevices { get; }
    public UInt32 MaximumResourcesPerDevice { get; }
    public Boolean InterruptBrokerInstalled { get; }
}

public readonly unsafe struct KernelDriverCallbacks
{
    public readonly delegate*<KernelDriverDeviceContext*, Boolean> Probe;
    public readonly delegate*<KernelDriverDeviceContext*, Boolean> Start;
    public readonly delegate*<KernelDriverDeviceContext*, Boolean> Stop;
    public readonly delegate*<KernelDriverDeviceContext*, Boolean> Remove;
    public readonly delegate*<KernelDriverDeviceContext*, UInt64, Boolean> Interrupt;
    public KernelDriverCallbacks(delegate*<KernelDriverDeviceContext*, Boolean> probe, delegate*<KernelDriverDeviceContext*, Boolean> start, delegate*<KernelDriverDeviceContext*, Boolean> stop, delegate*<KernelDriverDeviceContext*, Boolean> remove, delegate*<KernelDriverDeviceContext*, UInt64, Boolean> interrupt)
    { Probe=probe; Start=start; Stop=stop; Remove=remove; Interrupt=interrupt; }
}
