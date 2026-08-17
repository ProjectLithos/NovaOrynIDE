# NovaOryn 0.5.1

NovaOryn 0.5.1 is a corrective release for roadmap item 12, ACPI and hardware discovery.

The 0.5.0 host-side solution and policy tests compiled successfully, but the later freestanding bootstrap compilation exposed seven C# conversion errors in `NovaOryn.Kernel.Acpi/KernelAcpi.cs`: three MADT-count calls supplied `UInt32` literals to private `Byte` parameters, and the ACPI checksum accumulator relied on compound-assignment narrowing from the arithmetic result back to `Byte`.

This release makes those conversion boundaries explicit. `GetProcessorCount`, `GetIoApicCount`, and `GetInterruptOverrideCount` cast their MADT entry-type selectors to `Byte`; `ChecksumIsZero` explicitly converts the initial value, each accumulated sum, and the zero comparison to `Byte`. The same corrected `KernelAcpi.cs` is present in the authoritative source tree, command-line kernel template, and Visual Studio kernel template.

There are no changes to the ACPI public API, boot-context ABI, RSDP capture, RSDT/XSDT validation, MADT parsing semantics, MCFG discovery, HPET discovery, PMM, VMM, address-space design, or heap behaviour.
