# NovaOryn 0.0.34

## Visual Studio package documentation correction

This release fixes the VSIX build under the repository-wide `CS1591` warnings-as-errors policy.

The publicly visible `NovaOrynPackage` type and its overridden `InitializeAsync` and `Dispose` members now have complete XML documentation comments. No Visual Studio command, template, build, run, QEMU, serial, framebuffer, or halt behaviour was changed.

Run:

```bat
Update-NovaOryn.bat
Install-NovaOrynVSIX.bat
```
