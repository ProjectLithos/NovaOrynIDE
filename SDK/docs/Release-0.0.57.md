# NovaOryn 0.0.57

## Purpose

This release is rebuilt from the complete source snapshot supplied from the active `C:\NovaOryn` repository rather than from an incomplete earlier FullSource archive.

## Corrections

- Documents every public member in `NovaOryn.Architecture.X64/CPU.cs`, preventing CS1591 from becoming the next architecture build failure.
- Retains the documented `ICpu` contract and its required `NovaOryn.Primitives` project reference.
- Adds `NovaOryn.Architecture.Arm64` to `NovaOryn.sln` for Debug and Release Any CPU builds.
- Adds source-policy regression checks for x64 compatibility API documentation and ARM64 solution membership.
- Synchronises SDK, VSIX, project-template, QEMU, documentation, assembly and toolchain versions at 0.0.57.
- Rebuilds FullSource from the complete uploaded source tree.

## Validation boundary

Archive layout, manifest hashes, project-reference consistency, solution membership, XML-documentation coverage and source-policy source assertions were validated in the packaging environment. The packaging environment does not contain the repository-pinned Windows .NET/ILC/LLVM/QEMU toolchain, so the Windows freestanding build and QEMU runtime acceptance must execute through `Build-NovaOryn.bat` after update.
