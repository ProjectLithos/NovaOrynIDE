# NovaOryn 0.0.98

NovaOryn 0.0.98 fixes the source-policy false positives reported after the 0.0.97 solution build completed successfully.

## Fixed

- Generated command-line and Visual Studio kernels are now accepted when the exact 32-pixel font size is expressed as `ConsoleFontSize = 32U` and passed to `KernelConsole.Initialize(boot, ConsoleFontSize)`.
- The kernel-sample serial/framebuffer mirroring check now accepts `FramebufferConfiguration.Default(ConsoleFontSize)` when the same source declares `ConsoleFontSize = 32U`.
- The checks still reject any other font-size value; this is a policy-test correction, not a relaxation of the 32-pixel renderer contract.

## Unchanged

- The NovaOryn Mono glyph data and framebuffer renderer are unchanged.
- The command-line and Visual Studio generated `Kernel.cs` files are unchanged and remain user-editable.
- Serial/framebuffer mirroring in `NovaOryn.Kernel.Sample` is unchanged.
- Boot memory-map capture and normalisation are unchanged.

## Version alignment

SDK, assembly, tool, documentation, template, VSIX, image-builder, managed-compiler, QEMU-launcher, and toolchain product versions are aligned to 0.0.98.
