# NovaOryn 0.4.12

NovaOryn 0.4.12 corrects Visual Studio design-time SDK-reference synchronization for generated kernels.

The Visual Studio synchronizer now validates and repairs the complete required root-project reference graph instead of repairing only `NovaOryn.Kernel.Heap`. In particular, existing projects that use `KernelAddressSpace` but have a stale or missing `NovaOryn.Kernel.AddressSpace` `ProjectReference` are repaired automatically. The synchronizer also verifies that the address-space and heap SDK project/source files exist before attempting repair, keeps its Visual Studio UI-thread boundary explicit, loads all SDK-owned projects into the active solution, and touches the repaired project file so the project system observes the changed graph.

Template policy now requires generated kernels that use `KernelAddressSpace` to import its namespace, requires both command-line and Visual Studio root projects to reference `NovaOryn.Kernel.AddressSpace`, requires the address-space SDK files to exist in both template trees, and requires the VSIX synchronizer to repair both address-space and heap references.

There are no PMM, VMM, direct-map, address-space-layout, heap-allocation, framebuffer, documentation-generator, or NativeAOT runtime behavior changes in this corrective release.
