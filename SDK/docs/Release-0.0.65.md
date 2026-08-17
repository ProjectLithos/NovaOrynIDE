# NovaOryn 0.0.65

NovaOryn 0.0.65 separates the freestanding kernel into explicit managed assemblies.

- `NovaOryn.Freestanding.CoreLib.dll` is the shared no-standard-library system module.
- `NovaOryn.Kernel.X64.LowLevel.dll` owns every native import and low-level x64 operation used by the bootstrap.
- `NovaOryn.Kernel.Console.dll` provides normal managed `Write` and `WriteLine` methods and hides serial/framebuffer mirroring.
- `NovaOryn.Kernel.Platform.X64.dll` provides high-level descriptor, interrupt-controller, and halt operations.
- `NovaOryn.Kernel.Bootstrap.dll` now contains only the kernel entry and high-level managed calls.

The kernel project template uses the same separation, and project creation refreshes SDK-owned infrastructure while preserving an existing user `Kernel.cs`.
