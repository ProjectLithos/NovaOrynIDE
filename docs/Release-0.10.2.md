# NovaOryn IDE 0.10.2

## Panic transport x64 reference fix

`KernelPanicTransport` correctly imports `NovaOryn.Kernel.Internal.X64.Native`, but generated NovaOryn kernel projects disable transitive project references. The kernel project therefore could not compile against the low-level assembly even though `NovaOryn.Kernel.X64.LowLevel.dll` was built.

0.10.2 adds an explicit project reference from the generated kernel bootstrap to `Sdk/NovaOryn.Kernel.X64.LowLevel/NovaOryn.Kernel.X64.LowLevel.csproj`.

Existing OS projects are repaired automatically during the normal SDK refresh before compilation. Both the missing `KernelPanicTransport.cs` compile item and the required x64-low-level project reference are now checked and repaired together.
