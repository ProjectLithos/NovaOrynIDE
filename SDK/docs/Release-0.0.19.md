# Nova Oryn OS SDK 0.0.20

## NativeAOT runtime-library integration

This release corrects the native link model discovered by the 0.0.18 Windows build.

The application static library produced by ILC does not contain the complete NativeAOT runtime. NovaOryn.ManagedCompiler now discovers the installed NativeAOT runtime libraries from the NuGet package cache and the publish intermediate directory and records them in `NovaOryn.Compile.json`.

NovaOryn.Linker now links those real runtime libraries before classifying unresolved `Rh*` and `Rhp*` symbols as missing NovaOryn runtime contracts.

Temporary compatibility handling remains limited to residual operating-system imports retained by the stock Windows NativeAOT runtime pack. It is not the final NovaOryn freestanding runtime pack.
