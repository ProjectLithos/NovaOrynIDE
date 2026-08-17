# Nova Oryn OS SDK 0.11.5

## Visual Studio template-policy escaping correction

NovaOryn 0.11.5 is a corrective release over 0.11.4. The complete solution and the Processes assembly compile successfully, but the Visual Studio synchronization acceptance test still produced a false negative because it scanned C# source text while matching the runtime form of an escaped path.

- Keeps the Visual Studio SDK-tree refresh and project-reference repair implementation unchanged.
- Corrects the template-policy assertion to match the C# source spelling of the preserved user kernel path (`Kernel\\Kernel.cs`) rather than the runtime spelling (`Kernel\Kernel.cs`).
- Continues to require `RefreshSdkTree`, the installed SDK root, `templates/NovaOrynKernel/Sdk`, and explicit preservation of the user-owned kernel source.
- Retains the complete Console, Platform, AddressSpace, Heap, ACPI, Time, SMP, Scheduler, Protection, SystemCalls, and Processes reference graph checks.

Roadmap item 18 remains Processes and executable loading; no later roadmap scope is introduced.
