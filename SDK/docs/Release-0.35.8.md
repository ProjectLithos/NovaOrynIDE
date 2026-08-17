# NovaOryn 0.35.8

NovaOryn 0.35.8 fixes Visual Studio project-template discovery after VSIX installation.

Visual Studio 2017 and later use template manifests for extension-installed templates rather than scanning arbitrary extension folders. The NovaOryn `.vstman` now correctly identifies the deployed template as a `ProjectGroup` and points to the compressed `NovaOrynKernel.zip` payload.

`Install-NovaOrynVSIX.ps1` also installs the validated multi-project ZIP into the current user's Visual Studio `Templates\\ProjectTemplates\\NovaOryn` directory. Visual Studio directly indexes ZIP templates from that supported user-template location on startup, providing a deterministic discovery path for both Visual Studio 2022 and Visual Studio 18.
