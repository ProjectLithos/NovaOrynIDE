# NovaOryn 0.11.6

## Visual Studio SDK-reference reconciliation

This corrective release makes the installed kernel template authoritative for Visual Studio design-time references. The solution synchronizer now reads every direct `Sdk\...` `ProjectReference` from the installed `templates\NovaOrynKernel\NovaOrynKernel.csproj` and repairs an existing external kernel project to match it. This removes the hand-maintained subset that could leave ACPI, physical memory, console, platform, or later SDK assemblies unresolved in Visual Studio.

The user-owned `Kernel\Kernel.cs` remains preserved. The SDK-owned `Sdk` tree is refreshed from the installed template before project-reference reconciliation.
