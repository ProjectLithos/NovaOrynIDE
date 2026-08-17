# NovaOryn 0.36.6

NovaOryn 0.36.6 fixes the remaining entry/bootstrap compatibility failure for existing IDE-generated projects.

## What failed

0.36.5 returned early from bootstrap migration whenever `Kernel\Kernel.cs` already declared `namespace NovaOryn.Kernel.Bootstrap`. That checked only the namespace, not the complete symbol consumed by the x64 entry project. An IDE-generated kernel could therefore already use the canonical namespace but expose its high-level `KMain(BootContext)` method on a differently named class. The bootstrap assembly itself built successfully, but `NovaOryn.Kernel.Entry.X64` then failed with CS0234 because `NovaOryn.Kernel.Bootstrap.Kernel` did not exist.

## Fix

`NovaOryn.ProjectCreator` now treats the bootstrap entry as one atomic contract: `NovaOryn.Kernel.Bootstrap.Kernel.KMain(BootContext)`. Refresh no longer returns success for the namespace alone. It validates `KMain`/`BootContext`, migrates a legacy namespace when required, and safely renames the class that owns `KMain` to `Kernel` when the user source has a different generated entry-class name. The KMain method body and the rest of the user-owned kernel source are preserved.

After refresh, ProjectCreator prints an explicit contract-verification line. If the contract cannot be established safely, refresh fails immediately with a targeted diagnostic instead of allowing a later CS0234 from `KernelEntry.cs`.
