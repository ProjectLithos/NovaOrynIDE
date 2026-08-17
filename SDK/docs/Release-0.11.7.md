# NovaOryn 0.11.7

## Visual Studio project-reference policy correction

This corrective release fixes the template-policy acceptance check introduced with the installed-template project-reference reconciliation. The Visual Studio synchronizer itself is unchanged: it continues to read the authoritative direct `Sdk\...` `ProjectReference` set from the installed `templates\NovaOrynKernel\NovaOrynKernel.csproj` and repair existing external kernel projects from that graph.

The policy test now validates the parser semantics (`GetRequiredProjectNames`, the installed root template, the project-reference marker, line parsing, and discovered-name collection) rather than comparing C# source text against an unescaped runtime XML fragment.
