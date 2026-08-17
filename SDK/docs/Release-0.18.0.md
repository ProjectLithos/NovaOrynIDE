# Nova Oryn OS SDK 0.18.0

NovaOryn 0.18.0 extends the existing ACPI discovery foundation into platform services for interrupt topology, PCI Express configuration, HPET discovery, fixed ACPI power management, embedded-controller access, power-button status, firmware reset, and S5 soft-off.

## ACPI platform services

- `KernelAcpiMadt` exposes enabled processors, I/O APICs, interrupt-source overrides, and the Local APIC base from MADT.
- `KernelAcpiMcfg` exposes validated PCI Express ECAM allocations from MCFG.
- `KernelAcpiHpet` exposes the firmware HPET hardware block.
- `KernelAcpiFadt` parses SCI/SMI fields, PM1 event/control blocks, extended GAS registers, reset register/value, FADT feature flags, and DSDT address.
- `KernelAcpiEc` implements the standard byte-oriented ACPI Embedded Controller command protocol for ECDT-described controllers.
- `KernelAcpiPower` enables fixed-feature power-button status handling, FADT reset, and ACPI S5 shutdown.

Shutdown does not hard-code emulator sleep values. NovaOryn reads the DSDT and decodes the `_S5` package values required by ACPI before programming PM1 control registers.

Actual S1/S3/S4 sleep-state transition support remains a later step; 0.18.0 deliberately implements S5 soft-off only.

## Low-level x64 support

The private x64 ABI now provides byte, word, and dword I/O-port access so ACPI Generic Address Structures can be serviced at their defined register width. Raw I/O remains below the high-level ACPI public surface.
