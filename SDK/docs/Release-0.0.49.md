# NovaOryn 0.0.50

## VSIX version validation correction

- Removed the obsolete hard-coded 0.0.44 VSIX version check.
- `Build-NovaOrynVSIX.ps1` now reads the expected extension version from `source.extension.vsixmanifest`.
- The built VSIX identity and version are read from the packaged `extension.vsixmanifest` and compared with the source manifest.
- The copied artifact filename now uses the resolved current version automatically.
- Future NovaOryn releases no longer require another script edit merely to advance the VSIX version.
