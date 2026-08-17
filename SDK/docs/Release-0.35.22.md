# NovaOryn 0.35.22

0.35.22 fixes stale source retention during the migration from built-in FAT32 to selectable filesystem projects.

The observed failure compiled `src\NovaOryn.Kernel.Storage\KernelFat32.cs` against the new generic 0.35.21 storage contracts, producing missing `KernelFat32VolumeInfo` errors. That file should have been deleted by the 0.35.21 release, but an overlay extraction can add/replace files without removing obsolete files.

The fix has three layers:

1. `Build-NovaOryn.ps1` removes obsolete `KernelFat32.cs` copies before any managed compilation.
2. `NovaOryn.Kernel.Storage.csproj` explicitly excludes `KernelFat32.cs` from `Compile` as a migration guard.
3. `Update-NovaOryn.ps1` now has explicit declared-deletion handling for `NovaOryn-Changes.json`.

No FAT code is added back to `NovaOryn.Kernel.Storage`. `NovaOryn.Filesystem.FatFs` remains an optional end-user-selected project.
