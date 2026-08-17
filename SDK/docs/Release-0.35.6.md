# NovaOryn 0.35.6

0.35.6 fixes Visual Studio extension packaging for the compressed multi-project template.

The VSSDK build can omit a generated template ZIP from the VSIX even when the file exists and is declared as VSIX content. `Build-NovaOrynVSIX.ps1` now treats VSSDK output as the base container, opens that container in update mode, inserts `ProjectTemplates/NovaOrynKernel.zip` directly, adds an OPC content-type override for the nested ZIP when required, and only then performs the existing structural validation.

This keeps the VSIX manifest's `Microsoft.VisualStudio.ProjectTemplate` asset and the physical payload in sync and avoids depending on VSSDK source-item discovery for the generated multi-project archive.
