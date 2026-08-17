# NovaOryn 0.2.0

## Virtual memory management

0.2.0 implements roadmap item 9, Virtual Memory Management.

### Added

- `NovaOryn.Memory.Virtual.Contracts` with architecture-neutral mapping, range, translation, protection, status, statistics, page-size, and custom-manager contracts.
- `NovaOryn.Memory.Virtual.X64` with four-level x64 canonical-address, index, leaf-entry, large-page, and table-pointer mechanics.
- `NovaOryn.Kernel.VirtualMemory`, a no-heap freestanding manager that attaches to the active x64 CR3 hierarchy.
- Native CR3 read/write, `INVLPG`, NX capability/EFER enabling, and 1 GiB-page capability checks in `native/x64/Paging.asm`.
- 4 KiB, 2 MiB, and 1 GiB mapping, unmapping, protection, and translation operations.
- Active inherited page-table discovery and permanent protection from the physical allocator.
- `KernelPhysicalMemory.TryExcludePage` and reserved-page accounting.
- `NovaOryn.VirtualMemory.Tests` and source-policy coverage.
- VSIX and command-line template integration so generated kernels initialize virtual memory after physical memory.

### Deliberately deferred

The final kernel virtual-address layout is not fixed in 0.2.0. Direct-map placement, MMIO windows, heap region, stack/guard-page regions, user/kernel split, and a permanent page-table access strategy remain the kernel address-space design stage.
