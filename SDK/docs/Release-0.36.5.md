# NovaOryn 0.36.5

NovaOryn 0.36.5 fixes the remaining existing-project entry/bootstrap migration failure seen after 0.36.4.

## What failed

0.36.4 only recognized an older kernel when its source contained the exact text `KMain(BootContext`. IDE-generated high-level kernels can legally spell the same contract with a namespace-qualified parameter type, such as `KMain(NovaOryn.Kernel.Console.BootContext boot)`. Those sources therefore bypassed migration, compiled into `NovaOryn.Kernel.Bootstrap.dll` under their old namespace, and then caused `NovaOryn.Kernel.Entry.X64` to fail with CS0234 because `NovaOryn.Kernel.Bootstrap.Kernel` was absent.

## Fix

`NovaOryn.ProjectCreator` now recognizes the contract by its semantic source markers — the `Kernel` type, `KMain`, and `BootContext` — rather than requiring one exact parameter spelling. When those markers are present and the namespace is legacy, refresh changes only the namespace declaration to `NovaOryn.Kernel.Bootstrap`; the user-owned kernel body is preserved.

This covers both unqualified and namespace-qualified `BootContext` forms and avoids coupling migration to whitespace or formatting choices.
