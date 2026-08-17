# NovaOryn 0.20.3

NovaOryn 0.20.3 corrects the Visual Studio VSIX acceptance script after the dispatch architecture introduced in 0.20.0.

`Build-NovaOrynVSIX.ps1` no longer requires the obsolete generated-kernel `KernelPlatform.Halt()` call. The built template must instead contain the normal interactive dispatch path: `KernelInterruptDispatch.Initialize()`, `KernelTimerDispatch.Initialize()`, `KernelInterruptDispatch.Enable()`, and `KernelConsole.RunInteractive()`.

The generated kernel remains non-polling by default. `NovaOryn.Kernel.Polling` remains an explicit optional methodology, while console input is serviced through `KernelConsole.ServiceInput()` under timer/interrupt dispatch.

The framebuffer-console documentation was also corrected to remove the stale `PollInput()/PAUSE` description.
