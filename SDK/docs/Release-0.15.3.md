# NovaOryn 0.15.3

NovaOryn 0.15.3 is a corrective build release for the PCI/PCIe and VirtIO driver family introduced in 0.15.0.

## Correction

- Corrects the final `VirtioMath.SelectQueueSize` compiler error by returning an explicit `UInt16` zero rather than the explicitly `UInt32` literal `0U`.
- Applies the same correction to both SDK-generated kernel template copies.

The 0.15.2 build log showed every other NovaOryn project compiling successfully; this was the sole remaining solution-build compiler error.
