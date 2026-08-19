# NovaOryn 0.8.2

NovaOryn 0.8.2 is a corrective release for roadmap item 15.

## Fix

The command-line and Visual Studio kernel templates already initialize `KernelConsole` through its high-level default overload. That overload resolves `DefaultFontSize` through `BitmapFont.DefaultFontSize`, which is 16 pixels. The 0.8.0 template-policy test still recognized only the older explicit `ConsoleFontSize = 16U` or `Initialize(boot, 16U)` patterns, so it rejected otherwise-correct templates.

The policy now validates the complete default-font contract for each generated-project template: `Kernel.cs` uses `KernelConsole.Initialize(boot)`, `KernelConsole` resolves its default through `BitmapFont.DefaultFontSize`, and `BitmapFont.DefaultFontSize` is `16U`.

No scheduler/thread API, x64 context-switch ABI, timer behavior, SMP behavior, or generated kernel runtime behavior changes in this release.
