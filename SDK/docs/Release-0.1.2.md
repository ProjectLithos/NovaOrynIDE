# NovaOryn 0.1.2

NovaOryn 0.1.2 is a build-correctness patch for the freestanding physical-memory integration introduced in 0.1.1.

## Fixed CS0169 build failure

`NovaOryn.Kernel.Memory.KernelPhysicalMemory` stores its early allocator metadata in a static unsafe `State` structure containing fixed buffers. The allocator accesses those buffers through `fixed` statements, but Roslyn can still report the containing `_state` field as unused (`CS0169`). NovaOryn deliberately enables `TreatWarningsAsErrors`, so that warning prevented the authoritative solution from compiling.

0.1.2 adds a narrowly scoped `#pragma warning disable CS0169` / `#pragma warning restore CS0169` pair around only the `_state` field. Repository-wide warning enforcement remains unchanged. The same correction is present in the command-line editable template and the Visual Studio template so generated kernels compile under the same policy.

## Regression policy

`NovaOryn.SourcePolicy.Tests` now checks all three copies of the freestanding physical-memory source and requires the local CS0169 suppression around the fixed-buffer state field. This prevents a future template refresh from silently dropping the build correction.

## Behaviour

There are no allocator-policy, boot-map, allocation, release, VSIX-reference-loading or public-API changes in 0.1.2. The default physical-memory manager still initializes from the retained final UEFI map after `ExitBootServices`, remains heap-independent, and exposes the same bounded contiguous allocation/release behaviour added in 0.1.1.

The next architecture stage remains kernel address-space design, followed by virtual memory management.
