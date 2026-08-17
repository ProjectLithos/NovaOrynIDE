# NovaOryn 0.3.0

NovaOryn 0.3.0 implements roadmap item 10: kernel address-space design.

## Added

- `NovaOryn.Memory.AddressSpace.Contracts` with replaceable region/layout policy contracts.
- `NovaOryn.Memory.AddressSpace.X64` with the standard four-level x64 layout and direct-map transforms.
- `NovaOryn.Kernel.AddressSpace` for freestanding layout validation after VMM initialization.
- `NovaOryn.AddressSpace.Tests` covering canonical placement, region separation, null guard, and direct-map transforms.
- `docs/Kernel-Address-Space-Design.md` documenting the complete standard layout and methodology.
- Generated-kernel and Visual Studio template integration.

## Standard reservations

The standard policy defines a 64 KiB low-address guard, low-half user region, 4 GiB higher-half kernel-image window, 1 TiB heap reservation, 1 TiB stack arena, 64 TiB direct physical map, 16 TiB MMIO window, and 512 GiB page-table access window.

0.3.0 defines and validates these ranges but intentionally does not allocate the kernel heap or eagerly remap the live UEFI-inherited address space. Those actions belong to later allocator/address-space activation work.
