# NovaOryn 0.0.55

## VSIX installer reliability

- Removed the separate uninstall operation before installing an updated VSIX.
- Removed `/shutdownprocesses`; the script already refuses installation while Visual Studio is running, and Visual Studio 2026 can terminate the installer host with status `0xC000013A` when that switch is used.
- The same extension identity is now upgraded directly with `/force`.
- If quiet installation is interrupted with `0xC000013A`, the installer retries once with its user interface so the real Visual Studio result is visible.
