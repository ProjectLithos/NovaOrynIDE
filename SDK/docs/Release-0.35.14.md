# NovaOryn 0.35.14

NovaOryn 0.35.14 fixes the remaining Visual Studio New Project catalogue problem by correcting the physical layout of the multi-project template ZIP.

Visual Studio's multi-project template format requires a visible root `ProjectGroup` `.vstemplate` at the ZIP root and each child project template in its own subdirectory. NovaOryn previously placed both:

- `NovaOrynKernel.vstemplate`
- `KernelProject.vstemplate`

at the ZIP root.

The hidden kernel child has now moved to:

`KernelProject\KernelProject.vstemplate`

and all files owned by that child project (`NovaOrynKernel.csproj`, `Kernel`, `Boot`, `HAL`, `Sdk`, build/run scripts, project JSON and kernel README) live under the same `KernelProject` template directory. The root ProjectGroup links to that child with `KernelProject\KernelProject.vstemplate`.

The root archive now contains exactly one `.vstemplate`: `NovaOrynKernel.vstemplate`.

`Build-NovaOrynVSIX.ps1` validates this invariant in the final compressed template and fails the build if another root `.vstemplate` is introduced.
