# NovaOryn 0.25.3

Corrective release for stale interactive-console acceptance text.

- Updates `NovaOryn.BootPolicy.Tests` to require the current runtime banner: `Interactive console ready. Type to echo; Up/Down scroll; 1/2/3 change font size.`
- Updates `NovaOryn.TemplatePolicy.Tests` to require the same current generated-kernel banner.
- Updates `NovaOryn.ProjectCreator` migration text so newly migrated projects receive the same current interactive-console banner.
- Updates framebuffer-console documentation to match runtime behaviour.
- No changes to the working PS/2 input implementation, `NovaOryn.String`, or freestanding CoreLib behaviour.
