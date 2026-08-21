# NovaOryn IDE 0.14.5

## Windows launcher argument hardening

0.14.5 fixes the remaining Windows command-line parsing failures seen in 0.14.4.

The root cause was passing `%~dp0` (which ends in a backslash) as a quoted native `powershell.exe` argument together with further named parameters. That can corrupt native argument parsing and make PowerShell treat later parameters incorrectly. Once CMD parsing was destabilised, lines from the `VERSION` manifest could be interpreted as commands.

The build-state, package-lock and runtime-package PowerShell helpers now derive the repository root from `$PSScriptRoot`, read the authoritative version directly from `VERSION`, and read the Theia/Electron pins from the Electron package manifest. The batch files therefore pass no repository-root or version strings to those scripts.

`Run-NovaOrynIDE.bat` also launches npm with an explicit `--prefix` pointing at `.toolchain\NpmWorkspace`. This prevents npm from looking for `C:\NovaOrynIDE\package.json`; the root JSON manifest intentionally lives under `JSON\package.json` and is staged into the toolchain workspace by the build.

The 0.14.0 hardware-abstraction boundaries and the user-customisable Engineering-window layout remain unchanged.
