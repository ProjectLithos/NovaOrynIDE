# NovaOryn 0.29.0

NovaOryn 0.29.0 separates fast kernel development builds from exhaustive SDK validation.

## Fast kernel build/run

`Build-NovaOryn.bat`, generated kernel `Build-Kernel.bat` / `Run-Kernel.bat`, and the Visual Studio Build/Run commands now use the normal fast path.

The fast path assembles the required x64 native objects, builds only the host-side compiler/linker/image/QEMU/project-creator tools, compiles the selected kernel project and its MSBuild `ProjectReference` dependency graph, runs direct ILC and native linking, creates the EFI image, and launches QEMU only when requested.

It does **not** regenerate SDK documentation, build the complete `NovaOryn.sln`, or build/run every independent NovaOryn test program before ordinary kernel development.

## Exhaustive validation

`Validate-NovaOryn.bat` invokes `Validate-NovaOryn.ps1`, which retains documentation generation/audit and calls `Build-NovaOryn.ps1 -Validate -NoRun`. The validation mode retains the complete solution build, policy programs, subsystem tests, and kernel-image production required for release/integration confidence.

Visual Studio now reports explicitly that F5/Ctrl+F5 is using the fast kernel path and skipping SDK validation.
