# NovaOryn 0.4.10

NovaOryn 0.4.10 fixes Visual Studio design-time resolution for the kernel heap SDK assembly.

## Changes

- The Visual Studio solution synchronizer verifies the generated `Sdk/NovaOryn.Kernel.Heap` project and its two public source files.
- An older/stale root kernel project that is missing the direct `NovaOryn.Kernel.Heap` `ProjectReference` is repaired automatically when the VSIX synchronizes the solution.
- All SDK-owned projects are then loaded into the active solution, including `NovaOryn.Kernel.Heap`.
- Template policy tests require the CLI and VSIX templates to contain the heap project, heap sources, heap namespace import, and root project reference whenever generated `Kernel.cs` uses the heap APIs.
- No PMM, VMM, direct-map, address-space, heap-allocation, framebuffer, or NativeAOT runtime algorithm changed in this release.
