# NovaOryn 0.36.1

NovaOryn 0.36.1 fixes a VSIX build regression introduced by 0.36.0.

## Fix

`Build-NovaOrynVSIX.ps1` no longer requires the obsolete `KernelProjects\**\*.csproj` wildcard reference. The VSIX policy now enforces the 0.36 architecture instead:

- the root project must identify itself as `Kernel`;
- the root project must **not** auto-reference `KernelProjects\**\*.csproj`;
- the root project must consume `@(NovaOrynConfiguredKernelProject)`;
- `NovaOryn.Configuration.props` and `NovaOryn.Configuration.targets` must be present.

This makes the build-time policy agree with the configuration-driven project graph introduced in 0.36.0. Stale template documentation that still described wildcard linking has also been corrected.
