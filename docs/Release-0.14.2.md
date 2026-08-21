# NovaOryn IDE 0.14.2

0.14.2 fixes generated-build version/state validation between `Build-NovaOrynIDE.bat` and `Run-NovaOrynIDE.bat`.

## Build-state contract

- `VERSION` line 1 is now authoritative for both Build and Run launchers.
- Build stamps `applications/electron/lib/.novaoryn-build-version` without BOM or newline ambiguity.
- Build also stamps `applications/electron/lib/.novaoryn-build-state.json` with IDE, Theia and Electron versions.
- Run validates both markers against `VERSION` and the pinned runtime versions.
- Build invalidates generated/runtime state when the NovaOryn IDE version changes, even when the Theia/Electron pair has not changed.
- User-customised Engineering window locations remain unaffected; Engineering tools still open in the main document area only as their default first-open position.
