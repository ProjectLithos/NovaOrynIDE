# NovaOryn 0.5.0

## Feature

NovaOryn 0.5.0 implements roadmap item 12: ACPI and hardware discovery.

## UEFI boot hand-off

- Captures the ACPI RSDP from the UEFI configuration table before `ExitBootServices`.
- Prefers the ACPI 2.0 configuration-table GUID and falls back to the ACPI 1.0 GUID.
- Extends the native boot context with the RSDP physical address at offset `0x80`.
- Exposes the RSDP through the freestanding `BootContext` without making the user kernel parse UEFI structures.

## ACPI discovery assembly

Adds `NovaOryn.Kernel.Acpi`.

The assembly:
- validates RSDP 1.0 and extended 2.0+ checksums;
- prefers XSDT and falls back to RSDT;
- validates every consumed SDT checksum and length;
- provides generic four-byte-signature table lookup;
- enumerates enabled Local APIC and x2APIC processor entries;
- enumerates I/O APICs and interrupt-source overrides;
- honors MADT Local APIC Address Override entries;
- exposes PCI Express ECAM allocations from MCFG;
- exposes HPET register-discovery information;
- uses bounded, allocation-free table traversal suitable for early freestanding initialization.

## Generated kernels

Both command-line and Visual Studio project templates now:
- include `Sdk/NovaOryn.Kernel.Acpi`;
- reference `NovaOryn.Kernel.Acpi` from the root kernel project;
- initialize `KernelAcpi` before PMM initialization;
- report ACPI status and root table;
- report discovered processor and I/O APIC counts;
- print Local APIC, PCI ECAM, and HPET bases when advertised.

The Visual Studio solution synchronizer repairs missing ACPI project references alongside the existing address-space and heap references.

## Policy coverage

Boot policy now requires the UEFI RSDP capture path and visible ACPI initialization. Template policy verifies authoritative ACPI/BootContext copies, root project references, template SDK files, and Visual Studio repair coverage.

No timer, SMP startup, scheduler, userspace, syscall, process, driver, filesystem, or networking implementation is included in this release. Those remain later roadmap items.
