# NovaOryn 0.0.92

NovaOryn 0.0.92 implements the boot memory-map layer required before a physical allocator.

## Final UEFI map retention

The x64 UEFI entry now preserves the image handle and system table, captures GOP first, then obtains the final memory map into a 512 KiB buffer allocated as part of the loaded image. The returned map key is passed immediately to `ExitBootServices`; no allocation and no other firmware operation occurs between those two calls. `EFI_INVALID_PARAMETER` is treated as a stale key and causes a fresh map capture, up to eight attempts. Only the map whose key succeeds is marked final and passed into managed bootstrap.

The native boot context now carries the final map address, byte length, accepted key, descriptor size, descriptor version, capture-attempt count, exit status, and final-map flag. Managed bootstrap refuses to continue when this final map is absent.

## Portable memory contracts

The new `NovaOryn.Memory.Contracts` assembly provides:

- `MemoryDescriptor` with physical start, page count, byte length, type, cache/protection attributes, runtime status, availability, and optional future NUMA node;
- all required memory ownership types, including conventional, loader/kernel, boot services, runtime services, ACPI, framebuffer, MMIO, firmware-reserved, bad, persistent, boot structures, page tables, and early allocations;
- checked descriptor creation and slicing that reject alignment and arithmetic overflow;
- explicit `MemoryReservation` overlays;
- bounded scratch workspaces;
- immutable indexed diagnostics and a forward-only cursor;
- detailed normalisation status and counters.

## Three normalisation implementations

The new `NovaOryn.Boot.Memory` assembly implements three selectable versions behind `IMemoryMapNormaliser`:

1. `StrictMemoryMapNormaliser` rejects incompatible firmware overlaps.
2. `SafetyPriorityMemoryMapNormaliser` selects the safest, highest-priority owner.
3. `ConservativeMemoryMapNormaliser` converts incompatible firmware overlaps into reserved memory while retaining runtime ownership.

All three versions sort boundaries, split overlaps, merge compatible adjacent ranges, preserve mixed runtime code/data ownership, reduce contradictory cache modes to one safe cache mode, and overlay reservations for the kernel image, NovaOryn boot structures, framebuffer, MMIO, active page tables, and early allocator allocations. Explicit reservations do not erase firmware runtime ownership or protection metadata. `MemoryReservationPlan.TryValidateRequiredReservations` verifies mandatory kernel and boot-structure categories plus platform-required framebuffer and MMIO categories.

## Validation

`NovaOryn.Memory.Tests` validates stale-key retry, final-map sealing, invalid provider counts before firmware exit, all three overlap policies, safe cache-mode resolution, mixed runtime ownership, sorting, splitting, adjacent merging, reservation-only regions, mandatory reservation validation, native retained-buffer adaptation, immutable diagnostic traversal, and overflow rejection. The main build now executes these tests after the source-policy suite.
