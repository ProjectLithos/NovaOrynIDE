# NovaOryn 0.35.25

NovaOryn 0.35.25 fixes the last reported VSIX build analyzer error from the Project Configuration Pages work.

The 0.35.24 build reached `NovaOrynProjectRecognizer.TryGetProjectDirectory(Project)` and VSTHRD010 correctly reported that `EnvDTE.Project.FullName` is a Visual Studio automation/COM property that must be accessed on the main thread.

The helper now begins with:

`ThreadHelper.ThrowIfNotOnUIThread();`

and imports `Microsoft.VisualStudio.Shell`.

The configuration service already invokes the helper from UI-thread guarded paths, so this makes the analyzer contract explicit without changing runtime behavior.
