# Nova Oryn OS SDK 0.11.3

## Visual Studio SDK project repair

NovaOryn 0.11.3 corrects Visual Studio design-time failures in existing generated kernel projects after SDK assemblies were added or updated.

- Refreshes the SDK-owned `Sdk` tree in an existing NovaOryn kernel project from the installed `templates/NovaOrynKernel/Sdk` tree when the solution is opened.
- Preserves the user-owned `Kernel/Kernel.cs` while updating SDK implementation projects and source files.
- Removes stale SDK-owned files that are no longer present in the installed template.
- Repairs direct root-project references for `NovaOryn.Kernel.Console`, `NovaOryn.Kernel.Platform.X64`, `NovaOryn.Kernel.Processes`, and the other required kernel SDK assemblies.
- Loads refreshed SDK projects into the Visual Studio solution after synchronization.
- Extends template policy coverage so future releases must retain the Visual Studio SDK refresh and complete reference graph.

This is a corrective release over 0.11.2. Roadmap item 18 remains Processes and executable loading; no later roadmap scope is introduced.
