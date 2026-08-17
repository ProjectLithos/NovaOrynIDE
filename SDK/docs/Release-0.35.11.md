# NovaOryn 0.35.11

NovaOryn 0.35.11 fixes the Visual Studio New Project catalogue path rather than only reinstalling the VSIX.

The previous template metadata had two discovery hazards:

- the visible `ProjectGroup`, its `.vstman` header, and the hidden kernel child reused the same `TemplateID`;
- the `.vstman` file used `TemplateType` instead of the schema-defined `VSTemplateType` attribute.

The installer now places the validated NovaOryn ZIP in the documented C# user-template directory (`Templates\ProjectTemplates\Visual C#`), removes old NovaOryn layout copies and positively identified legacy Oryn templates, clears stale project-template caches for the selected Visual Studio major version, and runs `devenv /installvstemplates`.

The build also validates that the visible root template has no `TemplateID`, embeds the corrected `.vstman` in the VSIX, and verifies that both the compressed template and template manifest are physically present in the final package.
