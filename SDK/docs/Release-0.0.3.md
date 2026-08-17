# Nova Oryn OS SDK 0.0.3

This release fixes `Update-NovaOryn.bat` by replacing the fragile embedded-PowerShell section with:

- a small conventional batch wrapper
- a standalone `Update-NovaOryn.ps1` implementation

The batch forwards an optional archive-folder argument to the PowerShell script. The updater still uses FullSource for the first commit and ChangedFiles for later commits. It does not push or download the toolchain.
