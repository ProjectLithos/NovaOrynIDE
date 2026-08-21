# NovaOryn IDE 0.14.17

This release fixes the post-build/post-run command-parser failures seen after a successful 0.14.16 build.

## Version handling

`VERSION` remains the authoritative release file, including its human-readable update manifest. Build and Run no longer redirect that multi-line file directly into CMD. `Scripts/Resolve-NovaOrynIDEVersion.ps1` reads only line 1 and writes a one-line scratch value used by the batch launchers. This prevents manifest headings such as `RULE:`, `Package/application versions`, and `JSON` from ever being interpreted as commands.

## Git publishing

Git publishing has moved out of `Build-NovaOrynIDE.bat` into `Scripts/Publish-NovaOrynIDESource.ps1`. The automated commit disables developer-local Git hooks for that commit, preventing a local hook from recursively invoking the IDE build at commit time. Build still automatically stages, commits when needed, and pushes the source to the configured NovaOrynIDE repository.

## Security audit

`Scripts/Audit-NovaOrynIDE.ps1` now runs `npm audit` through `Start-Process` with separate stdout/stderr files. npm informational/retirement notices on stderr therefore no longer become terminating `NativeCommandError` records while valid audit JSON is available.

The existing Engineering main-document defaults, hardware abstraction boundaries, fault injection, QEMU hardware matrix, dependency-state reuse, and completed-build state checks remain unchanged.
