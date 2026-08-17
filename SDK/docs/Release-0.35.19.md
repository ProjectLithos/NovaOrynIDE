# NovaOryn 0.35.19

NovaOryn 0.35.19 fixes the remaining empty `Kernel\Kernel.cs` failure in Visual Studio-created kernel projects.

0.35.18 correctly detected the empty source, but the project refresh was still not sufficient on every Visual Studio-created workspace. The build boundary is now authoritative.

Immediately before safety scanning and compiling a selected or external kernel, `Build-NovaOryn.ps1` checks the user `Kernel\Kernel.cs`. If it is missing or contains only whitespace, the build writes the canonical high-level kernel source from `templates\NovaOrynKernel\Kernel\Kernel.cs`, reads it back, verifies it is non-empty, reports its byte count, and only then performs the forbidden-low-level-token scan.

A non-empty user kernel is never replaced.

`NovaOryn.ProjectCreator` now also repeats the repair at the end of project refresh rather than relying only on its template-copy loop, and reports the verified user-kernel byte count before returning success.

The Visual Studio `NovaOryn Kernel` template changes `Kernel\Kernel.cs` from `OpenInEditor=true` to `OpenInEditor=false`. This avoids keeping a newly instantiated source file in an editor buffer while NovaOryn refreshes the workspace on disk.
