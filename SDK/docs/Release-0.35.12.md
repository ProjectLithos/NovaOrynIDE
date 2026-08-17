# NovaOryn 0.35.12

NovaOryn 0.35.12 addresses the remaining Visual Studio 2026 New Project catalogue failure.

The 0.35.11 installer proved that the VSIX, template ZIP and visible root metadata were installed correctly, but Visual Studio still returned no result for `NovaOryn`. The correction therefore moves from payload validation to catalogue registration.

Changes:

- Build `NovaOryn.VisualStudio.csproj` with the selected Visual Studio installation's `MSBuild.exe` rather than `dotnet build`.
- Require the VSSDK `GenerateTemplatesManifest` stage to create `obj\Release\templateFiles.json`.
- Fail the VSIX build if that generated registration manifest does not reference `NovaOrynKernel`.
- Install the direct user-template fallback at `Templates\ProjectTemplates\NovaOrynKernel.zip` rather than under `Templates\ProjectTemplates\Visual C#`.
- Remove the old 0.35.11 `Visual C#\NovaOrynKernel.zip` copy.
- Clear `ProjectTemplatesCache`, `ItemTemplatesCache`, `ComponentModelCache`, and `InstalledTemplates.json` for Visual Studio 18.
- Run `devenv /updateconfiguration` and then `devenv /installvstemplates`.

This makes installation fail early if Visual Studio's own VSSDK template-registration stage did not run, rather than reporting success for a VSIX that contains a ZIP but is absent from the New Project catalogue.
