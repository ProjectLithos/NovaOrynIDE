# NovaOryn 0.35.15

NovaOryn 0.35.15 replaces the Visual Studio multi-project template with a catalogue of independent project templates.

## Visual Studio project catalogue

The VSIX now ships eight normal `Type="Project"` templates:

- NovaOryn Kernel
- NovaOryn Kernel Driver
- NovaOryn Kernel Library
- NovaOryn Userland Application
- NovaOryn Userland Service
- NovaOryn Userland Driver
- NovaOryn Userland Library
- NovaOryn Test Project

There is no active `ProjectGroup`, hidden child template, `ProjectTemplateLink`, or `.vstman` dependency. Each template has one visible root `.vstemplate`, a unique `TemplateID`, and its own project file. `Build-NovaOrynVSIX.ps1` builds all eight ZIPs, requires VSSDK to register all eight, and verifies that all eight payloads and manifest assets are physically present in the final VSIX. `Install-NovaOrynVSIX.ps1` additionally installs and validates all eight ZIPs in the Visual Studio user project-template catalogue before rebuilding the Visual Studio template caches.

## Kernel workspace

The main NovaOryn Kernel template remains one normal kernel project but now acts as a workspace root. It contains `NovaOrynProject.json` plus three explicit project areas:

- `KernelProjects` for separately compiled kernel-side projects. Any `KernelProjects\**\*.csproj` is automatically added as a `ProjectReference` to the root kernel project, so kernel drivers and kernel libraries are compiled and linked without a fixed project-count limit.
- `Userland` for independently compiled user-mode applications, services, drivers, and libraries. These are not linked into the kernel assembly.
- `Tests` for independently executable test programs.

`Build-WorkspaceProjects.ps1` discovers and builds every `Userland\**\*.csproj` and `Tests\**\*.csproj` with the repository-pinned .NET SDK before the kernel build.

The Visual Studio extension synchronizer discovers nested workspace projects and places them in `Kernel Projects`, `Userland`, and `Tests` solution folders while deliberately ignoring the copied SDK project tree.

## Adding projects

After installing the VSIX, use **Create a new project** for a new kernel workspace or **Add > New Project** inside an existing solution. Searching for `NovaOryn` exposes all eight templates. Kernel drivers/libraries should be placed under `KernelProjects`; userland projects under `Userland`; and test programs under `Tests` so workspace discovery and build orchestration are automatic.
