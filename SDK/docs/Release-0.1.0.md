# NovaOryn 0.1.0

NovaOryn 0.1.0 implements roadmap item 8: physical memory management.

## Physical-memory contracts

The new `NovaOryn.Memory.Physical.Contracts` assembly defines an architecture-independent ownership API for physical 4 KiB frames:

- `IPhysicalMemoryManager` is the replaceable allocator boundary used by later virtual-memory, heap, DMA, process, and driver layers.
- `PhysicalAllocationRequest` carries contiguous page count, power-of-two page alignment, inclusive minimum physical address, exclusive maximum physical address, and ownership purpose.
- `PhysicalAllocation` and `PhysicalReservation` retain opaque non-zero release tokens so stale handles and double release are rejected deterministically.
- `PhysicalMemoryStatistics` reports total managed, free, allocated, reserved, largest-free-run, and active-record counters.
- `PhysicalAllocatorWorkspace` points at caller-owned writable allocator metadata. The built-in allocators do not allocate their own backing arrays or depend on the future kernel heap.
- `PhysicalMemoryStatus` distinguishes invalid input, workspace exhaustion, address-constraint failure, ordinary physical exhaustion, stale handles, occupied reservation ranges, and bounded record-table exhaustion.

All public operations continue the NovaOryn API rule that functions return `bool` or a value; there are no public `void` methods.

## Three selectable allocator methodologies

The new `NovaOryn.Memory.Physical` assembly supplies three real implementations behind the common contract.

### Bitmap

`BitmapPhysicalMemoryManager` uses one ownership bit per frame in the physical span between the lowest and highest immediately allocatable frame. All bits begin unavailable and only normalised `AvailableAfterExitBootServices` ranges are cleared. Firmware holes and non-allocatable ranges therefore remain unavailable even inside the bitmap span.

It supports exact page counts, arbitrary power-of-two alignment, minimum/maximum physical-address constraints, exact fixed reservations, one-time release validation, and largest-free-run statistics.

### Buddy

`BuddyPhysicalMemoryManager` decomposes usable ranges into aligned power-of-two blocks. Allocation selects an order of:

```text
max(ceil(log2(requestedPages)), ceil(log2(alignmentPages)))
```

and recursively splits a larger free block when necessary. Release finds the equal-order buddy using XOR and recursively coalesces free pairs.

The returned allocation preserves both the requested page count and the actual rounded block size, making buddy internal fragmentation explicit. Exact arbitrary reservations remain supported by splitting affected free blocks down to individual pages and coalescing them again on release.

### Extent

`ExtentPhysicalMemoryManager` maintains sorted free physical ranges. It aligns the first candidate within each free extent, removes the exact requested interval, retains any leading/trailing fragments, and merges adjacent ranges when allocations or reservations are released.

Its metadata scales with extent fragmentation rather than the size of the physical address span, making it suitable for sparse maps and large contiguous regions.

## Boot-order boundary

0.1.0 deliberately sits between boot memory-map normalisation and virtual memory:

```text
final UEFI map
-> normalisation
-> physical memory manager
-> kernel address-space design
-> virtual memory manager
-> early allocator / kernel heap
```

The implementation does not introduce virtual mappings, a kernel heap, ACPI reclaim, NUMA placement, per-CPU caches, or DMA/IOMMU translation. Those remain later roadmap stages.

## Validation

The new `NovaOryn.PhysicalMemory.Tests` project validates all three implementations for:

- workspace sizing and initialisation;
- physical-page accounting;
- contiguous aligned allocation;
- minimum/maximum physical-address constraints;
- exact reservations;
- buddy request-versus-rounded ownership;
- allocation and reservation release;
- double-release rejection;
- too-small metadata workspace rejection;
- bounded allocation-record exhaustion;
- custom `IPhysicalMemoryManager` compatibility.

The main build executes these tests immediately after the existing boot-memory tests. Source-policy tests also require the new assemblies/tests in the authoritative solution and check that the built-in allocators use caller-owned metadata rather than allocating backing storage internally.

## Documentation and SDK integration

`docs/Physical-Memory-Management.md` documents the dependency order, common contract, bitmap formula, buddy order/coalescing formulas, extent selection formula, custom implementation path, reservations, statistics, and deliberate 0.1.0 boundaries.

The public documentation configuration includes both new assemblies. Solution, build pipeline, product metadata, Visual Studio extension/template names, managed compiler, image builder, QEMU launcher, toolchain manifest, README, source manifest, and release manifests are aligned to 0.1.0.
