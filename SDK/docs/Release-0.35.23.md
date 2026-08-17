# NovaOryn 0.35.23

This release adds real Visual Studio project Configuration Pages.

The extension provides `Tools > NovaOryn: Configure Project...` and automatically opens the pages for a newly created NovaOryn kernel project until configuration is completed.

Pages:
1. Architecture — x64, ARM64, RISC-V 64.
2. Kernel Model — Monolithic, Microkernel, Hybrid.
3. Work Areas — Shell, GUI, Drivers, HAL, Audio, Filesystems, Storage, Networking, USB, Input, Processes, Scheduler, System Calls, Security, Diagnostics and Tests.
4. Summary — effective configuration before Apply.

Apply writes `NovaOryn.Configuration.json`, `NovaOryn.Configuration.props`, updates `NovaOrynProject.json`, and creates `Configuration/WorkAreas.md`.

Kernel model supplies a default execution domain for new optional system projects: Monolithic=Kernel, Microkernel=Userland, Hybrid=Mixed. The current architecture pack is x64; ARM64/RISC-V targets are retained as configuration intent but the build stops until their packs exist. Optional subsystems remain separately selected projects.
