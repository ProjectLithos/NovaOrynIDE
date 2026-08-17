# NovaOryn 0.15.1

NovaOryn 0.15.1 is the corrective release for the PCI/PCIe and VirtIO driver introduction in 0.15.0.

## Build fixes

- Corrects XML documentation lines that accidentally placed C# declarations after `///`, causing the compiler to treat PCI properties and contextual storage/network callback fields as comments.
- Uses the existing ACPI `AcpiPciEcamInfo.SegmentGroup` property when consuming MCFG ECAM allocations.
- Corrects `sizeof(...)` conversions passed to the PCI and VirtIO byte-clear helpers.
- Keeps the SDK template copies byte-for-byte aligned with the canonical PCI, VirtIO, storage and networking sources.

## Regression protection

`NovaOryn.ReleasePolicy.Tests` now rejects any C# source declaration placed on the same physical line after an XML `/// <summary>...</summary>` comment.
