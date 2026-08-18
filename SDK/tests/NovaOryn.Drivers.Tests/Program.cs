using NovaOryn.Kernel.Drivers;

static void Assert(bool condition, string name) { if (!condition) throw new InvalidOperationException("[FAIL] " + name); Console.WriteLine("[ OK ] " + name); }

KernelDeviceIdentifier nvme = new(KernelDeviceBus.Pci, 0x8086, 0xF1A5, 0, 0, 0x010802, 1, 0x00010000);
KernelDriverMatchRule exact = new(KernelDeviceBus.Pci, true, 0x8086, true, 0xF1A5, true, 0, 0);
Assert(KernelDriverMath.Matches(exact, nvme), "Exact PCI vendor/device matching succeeds.");

KernelDriverMatchRule storageClass = new(KernelDeviceBus.Pci, true, 0, false, 0, false, 0x010800, 0xFFFF00);
Assert(KernelDriverMath.Matches(storageClass, nvme), "Class-mask matching supports bus-family drivers.");

KernelDriverMatchRule wrongVendor = new(KernelDeviceBus.Pci, true, 0x1234, true, 0, false, 0, 0);
Assert(!KernelDriverMath.Matches(wrongVendor, nvme), "Non-matching vendor identifiers are rejected.");

Assert(KernelDriverMath.IsValidResource(new(KernelDeviceResourceType.Memory, 0xF0000000, 0x4000, 0)), "MMIO resource ranges are accepted.");
Assert(!KernelDriverMath.IsValidResource(new(KernelDeviceResourceType.Memory, ulong.MaxValue - 3, 8, 0)), "Overflowing resource ranges are rejected.");
Assert(KernelDriverMath.IsValidResource(new(KernelDeviceResourceType.Interrupt, 17, 0, 0)), "Interrupt resources may use a zero byte length.");

KernelDriverInterruptRequest interrupt = new(new KernelDeviceHandle(1), 17, 8, 0, true, true, 42);
Assert(KernelDriverMath.IsValidInterruptRequest(interrupt), "Transport-neutral interrupt requests are accepted.");
KernelDriverInterruptRequest invalidPriority = new(new KernelDeviceHandle(1), 17, 16, 0, true, true, 42);
Assert(!KernelDriverMath.IsValidInterruptRequest(invalidPriority), "Out-of-range interrupt priorities are rejected.");

KernelDriverFrameworkOptions dynamicOptions = KernelDriverFrameworkOptions.DynamicDefault;
Assert(KernelDriverMath.IsValidOptions(dynamicOptions), "Default driver registry options are valid and dynamic.");
Assert(dynamicOptions.RegistryMode == KernelDriverRegistryMode.Dynamic, "Normal driver registry mode grows from the kernel heap.");
Assert(KernelDriverMath.NextCapacity(64, uint.MaxValue) == 128, "Dynamic driver capacity doubles when full.");
Assert(KernelDriverMath.NextCapacity(128, 192) == 192, "Dynamic growth respects an explicit maximum when configured.");
KernelDriverFrameworkOptions fixedOptions = KernelDriverFrameworkOptions.Fixed(32, 64);
Assert(KernelDriverMath.IsValidOptions(fixedOptions) && fixedOptions.RegistryMode == KernelDriverRegistryMode.Fixed, "Deterministic fixed-capacity mode remains an explicit policy option.");


KernelDriverCapabilityDeclaration declared = new(KernelDriverCapability.Mmio | KernelDriverCapability.Interrupt | KernelDriverCapability.PciConfig | KernelDriverCapability.Dma | KernelDriverCapability.Timers | KernelDriverCapability.Networking | KernelDriverCapability.Filesystem);
Assert(declared.Contains(KernelDriverCapability.Mmio) && !declared.Contains(KernelDriverCapability.PortIo), "Driver capability declarations are explicit bit sets.");
Assert(KernelDriverMath.IsValidCapabilityRequest(new(KernelDriverCapability.Mmio, 0xF0000000, 0x1000, KernelDriverCapabilityAccess.ReadWrite)), "MMIO capability requests require a concrete range.");
Assert(!KernelDriverMath.IsValidCapabilityRequest(new(KernelDriverCapability.Mmio, 0, 0, KernelDriverCapabilityAccess.ReadWrite)), "Range-less MMIO capability requests are rejected.");
Assert(KernelDriverMath.IsValidCapabilityRequest(new(KernelDriverCapability.PciConfig, 0, 0, KernelDriverCapabilityAccess.ReadWrite)), "PCI configuration capability requests are service-scoped.");
Assert(!KernelDriverMath.IsValidCapabilityRequest(new(KernelDriverCapability.Mmio | KernelDriverCapability.Dma, 0, 0, KernelDriverCapabilityAccess.Read)), "A capability grant request must name exactly one capability.");
Assert(KernelDriverMath.RangeContains(0x1000, 0x1000, 0x1800, 0x100), "Capability range validation accepts contained ranges.");
Assert(!KernelDriverMath.RangeContains(0x1000, 0x1000, 0x1F00, 0x200), "Capability range validation rejects escaping ranges.");

Console.WriteLine("[ OK ] Driver framework tests passed.");
