# Nova Oryn OS SDK 0.0.60

Release 0.0.60 adds the architecture-neutral interrupt-controller layer and its x64 implementation.

## Added

- `NovaOryn.InterruptControllers.Contracts` with opaque routes, vector allocation, routing, masking, affinity, priorities, EOI, IPI, polarity, trigger mode, and MSI message contracts.
- `NovaOryn.InterruptControllers.X64` coordinating legacy PIC disablement, Local APIC, I/O APIC, MSI, MSI-X, and x2APIC.
- Native port-I/O, MSR, and MMIO primitives in `InterruptControllers.obj`.
- Source-policy checks covering controller contracts, implementation stages, native support, build integration, and solution membership.

Drivers depend only on `IInterruptController`; they do not need to know which delivery mechanism was selected.

## Validation boundary

The source tree and release archives are structurally validated. Hardware discovery, ACPI MADT parsing, real multi-CPU APIC startup, MSI/MSI-X device programming, and QEMU runtime delivery still require the repository-pinned Windows toolchain and boot validation.
