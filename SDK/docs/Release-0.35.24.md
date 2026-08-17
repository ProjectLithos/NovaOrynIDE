# NovaOryn 0.35.24

NovaOryn 0.35.24 fixes the VSIX compilation errors in the first Project Configuration Pages implementation.

Observed 0.35.23 failures:

- `NovaOrynProjectRecognizer.TryGetProjectDirectory` was referenced but not implemented.
- `DTE.ToolWindows` was used on the base EnvDTE `DTE` interface.
- the first-run `JoinableTaskFactory.RunAsync` result triggered VSTHRD110 because it was not observed.

Corrections:

1. `NovaOrynProjectRecognizer.TryGetProjectDirectory(Project)` resolves `Project.FullName` safely.
2. selected-project discovery uses `DTE.SelectedItems`, then `SelectedItem.Project` / `SelectedItem.ProjectItem.ContainingProject`.
3. the delayed first-run configuration task ends in `.FileAndForget("NovaOryn/ConfigurationPages")`.

The actual Configuration Pages remain the 0.35.23 design:
Architecture, Kernel Model, Work Areas and Summary, persisted into project JSON/MSBuild metadata.
