# NovaOryn Project Configuration Pages

NovaOryn 0.35.23 adds project-level Visual Studio Configuration Pages. They open automatically for a newly created kernel project until configuration is completed, and can be reopened from `Tools > NovaOryn: Configure Project...`.

## Architecture
Choose x64, arm64 or riscv64. x64 is installed now. Other targets are persisted, but the build stops until the matching architecture pack exists.

## Kernel Model
Choose Monolithic, Microkernel or Hybrid. This becomes project/MSBuild metadata and sets the default execution domain for new optional system projects: Kernel, Userland or Mixed respectively.

## Work Areas
Choose Shell, GUI, Drivers, HAL, Audio, Filesystems, Storage, Networking, USB, Input, Processes, Scheduler, System Calls, Security, Diagnostics and Tests. These are workspace/development selections and do not bake optional components into the kernel.

## Summary / Apply
Apply writes `NovaOryn.Configuration.json`, `NovaOryn.Configuration.props`, updates `NovaOrynProject.json`, and writes `Configuration/WorkAreas.md`.
