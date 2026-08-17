# NovaOryn 0.0.90

NovaOryn 0.0.90 corrects a stale source-policy assertion introduced during the serial-I/O separation.

`NovaOryn.Kernel.Console.dll` already initializes and clears the framebuffer, writes each character through the high-level `Native.WriteSerial(Byte)` operation, and mirrors the same character through `FramebufferConsole.Write(Byte)`. The policy test still required the obsolete expression `Native.WritePort8(0x3F8, value)` inside `KernelConsole.cs`. That contradicted the SDK rule that raw port I/O and COM1 addresses must remain hidden in `NovaOryn.Kernel.X64.LowLevel.dll`.

The corrected policy now requires all four real managed-console behaviours:

- `_framebuffer.Initialize(boot)`;
- `_framebuffer.Clear()`;
- `Native.WriteSerial(value)`;
- `_framebuffer.Write(value)`.

The policy continues to reject `WritePort8` and `0x3F8` inside the managed console assembly. No kernel runtime, framebuffer renderer, serial implementation, GDT/TSS, IDT, interrupt-controller, NativeAOT, linker, disk-image, or QEMU behaviour changes in this release.
