# NovaOryn 0.35.21

This release makes filesystem implementations fully end-user selectable.

The generic `NovaOryn.Kernel.Storage` assembly no longer contains `KernelFat32`, `KernelFat32VolumeInfo`, FAT boot-sector parsing or FAT cluster helpers. The generated base kernel initializes storage/VFS but installs no filesystem provider.

The Visual Studio extension now contains nine independent project templates. The new **NovaOryn Filesystem - FatFs** template creates a separately compiled `KernelFileSystem` project intended to live below `KernelProjects`. It links into the kernel only when the end user deliberately adds it, and it still requires an explicit `FatFs.Install()` call after `KernelStorage.Initialize()`.

The FatFs port is freestanding C#/.NET-compatible code over `KernelStorageVolumeHandle`, `KernelStorage.ReadVolumeBlocks`, `KernelStorage.WriteVolumeBlocks`, `KernelStorage.Flush` and `KernelVfs.RegisterFileSystem`.

Initial profile:
- FAT12
- FAT16
- FAT32
- 512-byte through 64 KiB logical sectors
- 8.3 path lookup and subdirectories
- existing-file reads
- VFS seek
- flush
- safe in-place writes inside an already allocated file chain

Not advertised yet:
- exFAT
- long filenames
- file creation
- deletion
- rename
- truncation/extension

Those missing operations require corresponding generic VFS contracts first, so they remain outside the base kernel rather than being simulated with filesystem-specific kernel hooks.
