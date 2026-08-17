# NovaOryn 0.0.33 — Visual Studio Launch-Service Compilation Correction

This release corrects the two compiler errors reported while building the NovaOryn VSIX.

## Corrections

- `System.Diagnostics.Process` and `System.Diagnostics.ProcessStartInfo` are now fully qualified, preventing collision with `EnvDTE.Process`.
- The `DTE` service is assigned to a local variable before it is used for `File.SaveAll` and active-configuration resolution.
- When the DTE service is unavailable, configuration resolution safely falls back to `Debug`.

## Behaviour retained

- F5 and Ctrl+F5 interception.
- NovaOryn kernel-template creation.
- Build and Run commands in the Visual Studio Tools menu.
- Output streaming to the NovaOryn OS SDK pane.
- Existing NativeAOT, EFI image, QEMU, serial and CPU halt pipeline.

Run `Update-NovaOryn.bat`, then `Install-NovaOrynVSIX.bat`.
