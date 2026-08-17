# NovaOryn 0.0.67

NovaOryn 0.0.67 corrects direct ILC compilation after the freestanding kernel was split into multiple managed assemblies.

## Corrected ILC assembly input

Roslyn correctly produced the bootstrap, console, platform, low-level, and freestanding CoreLib DLLs. The direct ILC command, however, supplied only `NovaOryn.Kernel.Bootstrap.dll`. Consequently, ILC could not locate the configured `NovaOryn.Freestanding.CoreLib` system module.

This release:

- verifies that the configured system-module DLL exists in `ManagedIL`;
- supplies every managed DLL produced for the bootstrap project to direct ILC;
- keeps the bootstrap assembly first in the deterministic input order;
- records the complete managed input set in `NovaOryn.Compile.json`;
- adds a source-policy regression check for the multi-assembly ILC invocation.

The kernel remains separated into high-level managed kernel, console and platform assemblies, with native ABI declarations isolated in `NovaOryn.Kernel.X64.LowLevel.dll`.
