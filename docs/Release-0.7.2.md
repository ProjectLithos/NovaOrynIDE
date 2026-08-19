# NovaOryn IDE 0.7.2

Patch release for the 0.7.0 structured kernel logging release. The professional-tools build verifier now validates the canonical unified Hardware / Device Tree (PCI, USB, ACPI, platform, virtual and logical devices) instead of the obsolete pre-0.6.0 `Storage controllers` label.

The unified device model and structured kernel logging implementation are otherwise unchanged from 0.7.0.

## Patch correction
- Generated Boot/HAL startup diagnostics now use the structured kernel logging path.
- Existing OS refresh treats Boot/HAL as SDK-owned generated sources while preserving user Kernel\Kernel.cs.
- IDE serial output buffers incomplete kernel lines so polling cannot split a single kernel record into multiple `[KERNEL]` fragments.
