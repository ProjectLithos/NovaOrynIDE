# Nova Oryn OS SDK 0.11.4

## Visual Studio synchronization policy correction

NovaOryn 0.11.4 is a corrective release over 0.11.3. The 0.11.3 implementation compiled successfully, including the Processes assembly, but the template-policy test used an overly literal source-text assertion for the Visual Studio SDK refresh path.

- Keeps the existing Visual Studio SDK-tree refresh and project-reference repair implementation unchanged.
- Corrects the template-policy test to validate the semantic components of the refresh implementation instead of requiring one exact `Path.Combine(...)` source expression.
- Continues to require `RefreshSdkTree`, the installed SDK root, `templates/NovaOrynKernel/Sdk`, and preservation of the user-owned `Kernel/Kernel.cs`.
- Retains the complete Console, Platform, AddressSpace, Heap, ACPI, Time, SMP, Scheduler, Protection, SystemCalls, and Processes reference graph checks.

Roadmap item 18 remains Processes and executable loading; no later roadmap scope is introduced.
