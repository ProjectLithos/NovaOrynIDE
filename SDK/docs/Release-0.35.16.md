# NovaOryn 0.35.16

NovaOryn 0.35.16 fixes the VSIX packaging failure that occurred after all eight independent project templates had already been generated and registered by VSSDK.

The observed 0.35.15 build reached:

`[ OK ] VSSDK registered all 8 NovaOryn project templates.`

but then failed because `ProjectTemplates/NovaOrynKernel.zip` was not physically present inside the completed VSIX.

The Visual Studio project still declares all generated ZIPs as `Content`, now with `CopyToOutputDirectory=PreserveNewest`, `IncludeInVSIX=true`, `VSIXSubPath=ProjectTemplates`, and the canonical target path.

In addition, the VSIX build no longer depends solely on `CreateVsixContainer` to transfer externally generated ZIP files. After the normal VSSDK build, `Embed-NovaOrynProjectTemplates.ps1` opens the VSIX as an OPC/ZIP package and deterministically:

1. embeds all eight project-template ZIP files under `ProjectTemplates/`;
2. ensures exactly one `Microsoft.VisualStudio.ProjectTemplate` asset exists for each canonical path;
3. registers every nested ZIP as `application/zip` in `[Content_Types].xml`;
4. reopens the completed VSIX and verifies every physical payload and registration.

The build then performs its existing identity, version, asset-count and payload checks before copying the VSIX to `Artifacts\VisualStudio`.
