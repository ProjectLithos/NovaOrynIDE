# NovaOryn Filesystem - FatFs

This filesystem project is optional. NovaOryn's base kernel installs no filesystem.

Add this project below `KernelProjects`, then after `KernelStorage.Initialize()` call:

```csharp
using NovaOryn.Filesystem.FatFs;
if (!FatFs.Install()) return false;
```

Initial profile: FAT12/FAT16/FAT32, 8.3 path traversal, reads, VFS seek/flush,
and safe in-place writes inside already allocated files.

The current VFS has no create/delete/rename/truncate contract, so those operations,
long filenames and exFAT are deliberately not advertised yet.
