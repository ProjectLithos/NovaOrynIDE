# NovaOryn IDE 0.1.50

NovaOryn IDE 0.1.50 fixes Kernel Heap inspection so it no longer depends on NativeAOT preserving private static-field names in PDB/link-map output.

The bundled NovaOryn kernel heap now publishes a stable debugger-readable heap diagnostic ABI at a fixed reserved virtual address inside the heap reservation. The live first-fit metadata table itself resides in that region, so the IDE reads authoritative block state directly rather than a copied snapshot.

The diagnostic ABI exposes committed, allocated and peak bytes, live-allocation count, initialization state and all 512 allocator records. Kernels produced before this ABI retain the legacy PDB/map fallback.
