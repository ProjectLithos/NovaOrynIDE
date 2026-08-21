# NovaOryn IDE 0.14.6

## Dependency build-pipeline correction

0.14.6 removes the redundant second npm dependency reinstall fallback that could terminate the Windows batch build immediately after a successful Theia/Electron dependency check.

The build now performs one deterministic dependency installation, captures the installed-dependency verifier exit code explicitly, and either fails immediately on a real mismatch or continues through security checks, TypeScript/Theia compilation, release verification, and build-state stamping.

`Run-NovaOrynIDE.bat` remains launch-only and will only start a fully stamped build.
