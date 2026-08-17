# NovaOryn 0.36.4

NovaOryn 0.36.4 fixes the entry/bootstrap contract for projects created before the current high-level kernel namespace was introduced.

During `Build.bat`, `NovaOryn.ProjectCreator` refreshes SDK-owned project files but intentionally preserves a non-empty user-owned `Kernel\Kernel.cs`. That preservation could leave an older `Kernel` class in a legacy namespace. The root kernel project would still compile to `NovaOryn.Kernel.Bootstrap.dll`, but `NovaOryn.Kernel.Entry.X64` then failed because `global::NovaOryn.Kernel.Bootstrap.Kernel` did not exist.

The project refresh now recognizes the existing high-level `Kernel.KMain(BootContext)` contract and changes only its namespace to `NovaOryn.Kernel.Bootstrap`, preserving the rest of the user source. If the preserved file no longer exposes the required high-level contract, refresh stops immediately with a specific diagnostic instead of allowing a later `KernelEntry.cs` namespace error.
