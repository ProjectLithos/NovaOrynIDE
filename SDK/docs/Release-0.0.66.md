# NovaOryn 0.0.66

NovaOryn 0.0.66 corrects the freestanding CoreLib introduced in 0.0.65.

## Corrected compiler contract

The C# compiler emits `System.Reflection.DefaultMemberAttribute` for types that expose indexed members. The freestanding `System.String` type exposes its character indexer, so Roslyn requires the attribute constructor while compiling the no-standard-library system module.

This release adds:

- `System.Reflection.DefaultMemberAttribute`;
- its required `DefaultMemberAttribute(String memberName)` constructor;
- the same correction in the Visual Studio/end-user kernel template;
- source-policy regression checks covering both authoritative and template CoreLib copies.

The change does not expose low-level I/O to the end user. `Kernel.cs` continues to use normal managed `KernelConsole.Write` and `KernelConsole.WriteLine` methods, while native ABI declarations remain isolated in `NovaOryn.Kernel.X64.LowLevel.dll`.
