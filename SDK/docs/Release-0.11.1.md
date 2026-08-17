# NovaOryn 0.11.1

NovaOryn 0.11.1 is a corrective release for roadmap item 18, processes and executable loading.

## Corrections

- Corrects the three fixed-buffer declarations in `KernelProcesses` so C# fixed-buffer sizes are explicitly `Int32` constant expressions while the public/process capacity values remain normal .NET `UInt32` values.
- Applies the same correction to both generated-kernel SDK copies so command-line and Visual Studio projects receive identical sources.
- Corrects the x64 NASM syscall return instruction from the unsupported `sysretq` spelling to `sysret`.
- Corrects stale template documentation that still described user processes as a future stage.

## Scope

This release does not change the item-18 public API or executable formats. ELF64 `ET_EXEC` and PE32+ x64 in-memory loading remain the supported initial executable-loading models. Filesystem-backed executable discovery remains part of roadmap item 20.
