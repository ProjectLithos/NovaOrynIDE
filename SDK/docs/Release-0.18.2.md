# NovaOryn 0.18.2

NovaOryn 0.18.2 is a compile-fix patch for the ACPI platform services introduced in 0.18.0.

## Fixed

- Corrected the remaining ten `UInt32`-suffixed numeric literals used in `Byte` assignment or constructor-argument positions in `KernelAcpiPlatform.cs`.
- Corrected EC read output initialization, ACPI Generic Address Structure byte fields used by legacy/FADT power-button registers, and AML `_S5` byte-output initialization.
- Synchronized the authoritative ACPI source with the standalone generated-kernel template and Visual Studio generated-kernel template.
- No ACPI runtime semantics or public API shapes were changed.

The fix keeps GAS `AddressSpace`, `BitWidth`, `BitOffset`, and `AccessSize` fields byte-sized as defined by ACPI, rather than widening those interfaces to accommodate incorrectly typed literals.
