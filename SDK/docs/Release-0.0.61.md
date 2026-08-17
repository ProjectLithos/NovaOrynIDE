# Nova Oryn OS SDK 0.0.61

Release 0.0.61 corrects the interrupt-controller public documentation failure and makes the reusable sample kernel initialise and demonstrate the public x64 descriptor, interrupt, exception, and controller facilities.

## Corrected

- Added XML documentation to every public interrupt-delivery, polarity, trigger-mode, and APIC delivery-mode enum member.
- Removed the 17 `CS1591` failures reported while building `NovaOryn.InterruptControllers.Contracts`.

## Sample-kernel integration

The sample kernel now:

- creates and installs a processor-local GDT and TSS;
- supplies RSP0 and dedicated double-fault, NMI, and machine-check IST stacks;
- creates and installs all 256 IDT gates;
- registers the essential managed exception handlers;
- provides a serial/framebuffer exception diagnostic sink;
- exercises vector allocation and the opaque MSI route lifecycle, including message creation, masking, affinity, priority, removal, and release.

Local APIC and I/O APIC activation is intentionally not hard-coded in the sample. It must be supplied with addresses discovered from ACPI/MADT rather than assuming QEMU-specific physical addresses.
