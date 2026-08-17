# NovaOryn 0.36.0

NovaOryn 0.36.0 makes Project Configuration authoritative instead of metadata-only.

- `NovaOryn.Configuration.json` is now configuration schema version 2.
- Apply regenerates `NovaOryn.Configuration.props`, `NovaOryn.Configuration.targets`, and `Configuration/WorkspaceProjects.txt`.
- The root kernel project no longer auto-links `KernelProjects\**\*.csproj`; only `@(NovaOrynConfiguredKernelProject)` enters the kernel.
- Monolithic, Microkernel and Hybrid now produce different execution-domain/workspace graphs.
- Microkernel optional work areas are represented as independent userland service projects; Hybrid defaults Drivers/Input to kernel and service-oriented areas to userland.
- Deselecting a work area removes it from the active graph without deleting source files.
- Solution synchronization loads only configured workspace projects instead of every project found beneath workspace folders.
- `Build-WorkspaceProjects.ps1` builds only the configured graph.

This release establishes the authoritative configuration-to-project transformation layer. It intentionally preserves existing user projects on disk when their area becomes inactive so model changes are reversible and user code is not destroyed.
