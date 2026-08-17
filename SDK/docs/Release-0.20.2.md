# NovaOryn 0.20.2

NovaOryn 0.20.2 is a policy-correction patch for the dispatch architecture introduced in 0.20.0 and the CoreLib compatibility fix in 0.20.1.

## Corrected build policy

`NovaOryn.BuildPolicy.Tests` no longer requires the framebuffer console to contain the removed `PollInput()` background polling path. The policy now requires `KernelConsole.ServiceInput()` and `KernelConsole.RunInteractive()` and explicitly rejects reintroduction of `PollInput()`.

This matches the intended architecture: normal generated kernels service console input through timer/interrupt dispatch while `NovaOryn.Kernel.Polling` remains an opt-in alternative methodology.

No runtime implementation or public API behavior changes in this patch.
