# NovaOryn 0.4.11

NovaOryn 0.4.11 fixes the Visual Studio threading-analyzer regression in the heap-reference design-time synchronizer.

## Changes

- `NovaOrynSolutionSynchronizer.RepairHeapProjectReference` now calls `ThreadHelper.ThrowIfNotOnUIThread()` before any `NovaOrynOutputPane.WriteLine` call.
- This makes the helper's UI-thread contract explicit to the Visual Studio threading analyzer and fixes `VSTHRD010` during `Build-NovaOrynVSIX.bat`.
- `NovaOryn.TemplatePolicy.Tests` now verifies that the heap-reference repair helper retains an explicit UI-thread assertion.
- No PMM, VMM, direct-map, address-space, heap-allocation, framebuffer, documentation-site, or NativeAOT runtime algorithm changed in this release.
