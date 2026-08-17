# NovaOryn 0.0.35

## Visual Studio package XML comment placement correction

This release fixes the VSIX build errors `CS1587` and `CS1591`.

The XML documentation for `NovaOrynPackage` is now placed before the package attributes so that the compiler associates it with the public class declaration. Documentation for `InitializeAsync` and `Dispose` remains unchanged.

No Visual Studio command, project-template, build, run, QEMU, serial, framebuffer, or halt behaviour was changed.

Run:

```bat
Update-NovaOryn.bat
Install-NovaOrynVSIX.bat
```
