# NovaOryn 0.4.2

Corrective release for the 0.4.1 source-policy audit after the freestanding VMM was split across partial-class source files.

- Keeps the 0.4.1 PMM-derived, identity-reachable bootstrap allocation and calculated direct-map implementation unchanged.
- Makes the source-policy audit evaluate both `KernelVirtualMemory.cs` and `KernelVirtualMemory.DirectMap.cs` as one freestanding VMM implementation.
- Retains checks for ordinary post-direct-map `KernelPhysicalMemory.TryAllocate`, exact bootstrap `TryAllocateAt`, inherited page-table protection, mapping, translation, CR3 access, and TLB invalidation.
- Adds an explicit regression check proving that split direct-map implementation files remain inside policy coverage.
- Makes no public API, heap, address-space layout, framebuffer-font, or documentation navigation behaviour changes.
