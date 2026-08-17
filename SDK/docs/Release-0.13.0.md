# NovaOryn 0.13.0

NovaOryn 0.13.0 implements roadmap item 20: storage and filesystems.

## Added

- `NovaOryn.Kernel.Storage`, a freestanding heap-backed storage/VFS assembly.
- Driver-backed block-device registration with logical/physical geometry and read/write/flush callbacks.
- Heap-backed block-I/O request queues with read/write/flush operations and request lifecycle state.
- Dynamic device, volume, mount and open-file capacity by default, with explicit fixed-capacity policy.
- MBR partition discovery, protective-MBR detection and GPT header/entry discovery.
- Raw whole-device volume fallback for unpartitioned media.
- VFS mount namespaces, filesystem providers, longest-prefix mount routing and file handles.
- VFS open/read/write/seek/flush/close/unmount dispatch.
- `KernelProcesses.TryCreateFromFile(...)` for filesystem-backed ELF64/PE32+ process creation.
- Initial built-in FAT32 support: BPB validation, FAT/data geometry, 8.3 path lookup, subdirectory traversal, FAT chain traversal and file reads.
- Independent `NovaOryn.Storage.Tests`.
- Kernel and Visual Studio templates initialize storage/VFS after drivers and install FAT32.

## FAT32 write policy

The built-in FAT32 provider is read-only in 0.13.0. The generic VFS includes write callbacks for custom providers, but NovaOryn does not claim FAT allocation or metadata updates are crash-safe before those semantics are implemented and tested.

## Roadmap

Item 21 is networking.
