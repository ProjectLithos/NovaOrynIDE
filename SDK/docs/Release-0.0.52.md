# NovaOryn 0.0.52

## VSIX installer correction

Release 0.0.52 corrects the Visual Studio extension installation path introduced by the prior release.

- Removed the hard-coded `0.0.44` artifact path and messages from `Install-NovaOrynVSIX.ps1`.
- The installer now reads the extension identifier and version directly from `source.extension.vsixmanifest`.
- The exact newly built versioned artifact is selected from `Artifacts\VisualStudio`.
- Installation stops with a clear message when Visual Studio is still running instead of forcing the IDE to close and risking unsaved work.
- VSIX installation uses shutdown-process and force switches only after confirming that no `devenv.exe` process is active.
- Windows interruption status `0xC000013A` is reported explicitly rather than appearing only as the signed decimal exit code `-1073741510`.
- General nonzero failures now identify the usual temporary-log location.

The architecture contracts and implementations introduced in 0.0.51 are unchanged.
