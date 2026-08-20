# NovaOryn IDE 0.10.3

## Kernel panic ACPI reboot fix

The panic transport previously called `KernelAcpiPlatform.Reboot()`. `KernelAcpiPlatform.cs` is the source filename, but the public SDK class implementing ACPI fixed-feature power management is `KernelAcpiPower`.

The panic reboot policy now calls `KernelAcpiPower.Reboot()` in the SDK bootstrap, normal generated OS template, and Visual Studio template.

Existing OS projects receive the corrected `Boot/KernelPanicTransport.cs` automatically through the pre-build SDK refresh introduced in 0.10.1.
