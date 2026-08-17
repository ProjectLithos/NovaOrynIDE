# NovaOryn 0.15.2

NovaOryn 0.15.2 is a corrective build release for the PCI/PCIe and VirtIO driver family introduced in 0.15.0.

## Corrections

- Corrects `VirtioMath.SelectQueueSize` so its initial power-of-two value is explicitly represented as `UInt16` rather than an implicitly wider `UInt32` literal.
- Corrects the VirtIO common-configuration MSI-X vector disable writes so `0xFFFF` is passed to the 16-bit MMIO writer as `UInt16`.
- Applies the same VirtIO corrections to both SDK-generated kernel template copies.

These changes address the four remaining C# compiler errors reported after the 0.15.1 corrective release. PCI, storage, networking, and the rest of the SDK had already compiled successfully in that build log.
