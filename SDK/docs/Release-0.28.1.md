# NovaOryn 0.28.1

NovaOryn 0.28.1 is a build-correction release for the display-driver subsystem introduced in 0.28.0.

## Fixes

- `NovaOryn.Graphics.Tests` now enables unsafe compilation because the test intentionally links the graphics and driver contract source files, which contain unsafe function-pointer declarations. This removes the two CS0227 failures without changing the contracts.
- `NovaOryn.Kernel.Virtio.Gpu.KernelVirtioGpu` now declares the VirtIO vendor, modern GPU, and transitional GPU PCI identifiers with `UInt16`-compatible constant literals. This removes the three CS0266 failures without changing their numeric values.
- The VirtIO GPU source correction is mirrored into both SDK kernel template trees.
