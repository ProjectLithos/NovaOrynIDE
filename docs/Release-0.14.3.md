# NovaOryn IDE 0.14.3

## Build-state command parsing fix

0.14.3 removes the long inline PowerShell expressions used for IDE build-state invalidation and validation.

`Scripts/Manage-NovaOrynIDEBuildState.ps1` now owns three explicit operations:

- `Invalidate` — removes stale npm/Theia/generated state when the IDE, Theia, or Electron version changes.
- `Stamp` — writes the generated version and structured build-state markers after a successful build.
- `Validate` — checks those markers before launch.

Build and Run invoke the script with `powershell.exe -File`, preventing CMD from reparsing nested PowerShell syntax or VERSION manifest text.

The hardware-abstraction boundaries and user-customisable Engineering document docking introduced in 0.14.x remain unchanged.
