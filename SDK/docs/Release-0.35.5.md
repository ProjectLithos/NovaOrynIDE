# NovaOryn 0.35.5

Fixes Visual Studio extension packaging of the compressed NovaOryn multi-project template. `Build-NovaOrynVSIX.ps1` now creates `ProjectTemplates\NovaOrynKernel.zip` before invoking MSBuild, ensuring the VSSDK sees the archive when it snapshots VSIX source items. The Visual Studio project also generates the same stable template archive before `PrepareForBuild` for direct project builds, and VSIX validation confirms the nested archive is actually embedded.
