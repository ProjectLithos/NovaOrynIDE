# Storage and filesystems

NovaOryn separates **block storage**, **VFS**, and **filesystem implementations**.

`NovaOryn.Kernel.Storage` owns generic block-device registration, MBR/GPT volume discovery, namespaces, mount points and file-handle dispatch. It contains no FAT, ext, NTFS or other filesystem-format implementation.

## End-user selection

The base kernel initializes:

```csharp
if (!KernelStorage.Initialize()) return false;
```

and installs no filesystem automatically.

A user selects a filesystem by adding a kernel-side filesystem project below `KernelProjects` (or deliberately linking it elsewhere) and explicitly installing that provider.

For FatFs:

```csharp
using NovaOryn.Filesystem.FatFs;

if (!KernelStorage.Initialize()) return false;
if (!FatFs.Install()) return false;
```

The Visual Studio SDK supplies **NovaOryn Filesystem - FatFs** as an independent project template.

## FatFs 0.35.21 profile

The initial C#/.NET-compatible port exposes FAT12, FAT16 and FAT32 through generic NovaOryn VFS/block-device callbacks. It supports 8.3 path traversal, file reads, VFS seek, flush and safe in-place writes inside an existing file's allocated cluster chain.

The current VFS does not yet define create/delete/rename/truncate operations. Therefore those capabilities, long file names and exFAT are not advertised in this first port. They remain optional filesystem-module work rather than hidden base-kernel functionality.
