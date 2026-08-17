# NovaOryn 0.4.6

## Direct-map bootstrap workspace correction

0.4.5 could return `DirectMapInitializationFailed` on OVMF because the post-ExitBootServices bootstrap searched ordinary free ConventionalMemory for pages that happened to remain identity-mapped and writable. UEFI does not guarantee that such a free page exists.

0.4.6 removes that accidental firmware-page-table dependency. Before the final `GetMemoryMap` / `ExitBootServices` pair, the native x64 entry takes a planning memory-map snapshot, calculates a conservative page-table requirement for the ConventionalMemory direct-map extents, reserves exactly that calculated workspace with UEFI `AllocatePages` as `EfiLoaderData`, and records its physical base/count in the native boot context. The subsequent final memory map therefore records the workspace as loader-owned memory rather than ordinary free RAM.

After ExitBootServices, `KernelPhysicalMemory` retains the workspace separately from ordinary ConventionalMemory accounting. `KernelVirtualMemory` consumes those already-reachable pages while adopting its private PML4 and creating the first direct-map hierarchy. Once the direct map is complete, later page-table pages return to ordinary PMM allocation and are accessed through the direct map.

The generated kernel now prints the underlying virtual-memory status whenever `KernelAddressSpace.Initialize()` fails, which makes future address-space failures diagnosable without guessing.

No fixed compile-time page-table pool is introduced: the workspace page count is derived from the firmware memory map and allocated before ExitBootServices.
