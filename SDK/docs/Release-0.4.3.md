# Nova Oryn OS SDK 0.4.3

## Correction

Release 0.4.3 corrects the direct-map bootstrap crash observed after the VMM attached successfully but before `KernelAddressSpace.Initialize()` could report a status.

The early freestanding PMM previously made non-runtime UEFI `BootServicesCode` and `BootServicesData` immediately allocatable after `ExitBootServices`. Those types are reclaimable in principle, but NovaOryn is still using firmware-provided bootstrap state at this phase. In particular, the active x64 stack can reside in Boot Services data. The 0.4.1 direct-map bootstrap was the first path to allocate and zero page-table pages early enough to overwrite that still-live state.

0.4.3 therefore keeps the final UEFI memory map authoritative while narrowing the **early allocatable** subset to `ConventionalMemory` only. Boot Services code/data are retained as deferred-reclaim memory until a later kernel-owned stack transition permits an explicit safe reclaim operation. The calculated PMM-derived direct-map algorithm remains unchanged.

## Validation rule

Source-policy validation now requires the authoritative PMM and both generated-kernel copies to use the ConventionalMemory-only early boundary and rejects reintroduction of immediate Boot Services allocation.

## Behaviour unchanged

No public PMM/VMM/heap/address-space API is removed. The direct-map layout, heap layout, half-size framebuffer font, portable relative documentation site, and release/update workflow remain unchanged.
