# NovaOryn 0.35.18

NovaOryn 0.35.18 fixes the Visual Studio Run failure that occurred immediately after `NovaOryn.ProjectCreator` refreshed a newly created kernel workspace.

The observed sequence ended with:

`[ OK ] Project manifest: ...\NovaOrynProject.json`

followed by:

`You cannot call a method on a null-valued expression.`

`Build-NovaOryn.ps1` then reads `Kernel\Kernel.cs` and scans the high-level user kernel for forbidden low-level implementation tokens. On Windows PowerShell, `Get-Content -Raw` can yield `$null` for a zero-length source file, making the subsequent `.IndexOf(...)` call fail with the generic null-valued-expression error.

The correction has two layers.

1. `NovaOryn.ProjectCreator` treats only a completely empty/whitespace `Kernel\Kernel.cs` as an incomplete template instantiation and re-seeds it from the SDK's high-level kernel template. A non-empty `Kernel\Kernel.cs` remains user-owned and is never overwritten during refresh.
2. `Build-NovaOryn.ps1` now reads user-kernel source with `System.IO.File.ReadAllText`, checks `String.IsNullOrWhiteSpace`, and only then performs the low-level-token scan. This is applied to both selected-project and legacy external-project refresh paths.

`NovaOryn.ProjectCreator` also verifies that the required user kernel exists and is non-empty before it reports project creation/refresh success, while the template policy test requires the shipped Visual Studio kernel template to contain a non-empty `Kernel\Kernel.cs`.
