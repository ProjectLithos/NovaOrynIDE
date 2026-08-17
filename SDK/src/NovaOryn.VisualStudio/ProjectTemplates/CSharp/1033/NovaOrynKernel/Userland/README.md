# NovaOryn Userland

`Userland` is the workspace root for independently compiled NovaOryn user-mode projects. These projects are deliberately not linked into the kernel assembly.

The starter workspace includes Commands, Settings, Fonts, Images, Drivers, and an aggregate `NovaOryn.Userland` project. `Build-WorkspaceProjects.ps1` discovers every `Userland\**\*.csproj` project and builds it independently before the kernel build.

In Visual Studio use **Add > New Project**, search for **NovaOryn**, and choose **NovaOryn Userland Application**, **NovaOryn Userland Service**, **NovaOryn Userland Driver**, or **NovaOryn Userland Library**. Place the project below this `Userland` directory so the NovaOryn workspace build and Visual Studio synchronizer discover it automatically.

Privileged MMIO, PIO, page-table, interrupt-controller, and other kernel mechanisms remain in kernel/HAL projects.
