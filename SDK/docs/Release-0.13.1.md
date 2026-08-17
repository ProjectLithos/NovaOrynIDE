# NovaOryn 0.13.1

NovaOryn 0.13.1 is a corrective release for roadmap item 20, storage and filesystems.

## Corrected VFS callback output handling

`KernelVfs.Read` and `KernelVfs.Write` previously attempted to take the address of public `out` parameters when invoking unmanaged filesystem-provider callbacks. C# does not permit taking the address of those unfixed expressions directly, which produced CS0212 during the freestanding storage assembly build.

The VFS now uses ordinary local `UInt32` counters for callback output. Their addresses are stable unmanaged locals inside the unsafe method; after a successful callback the values are copied to `bytesRead` or `bytesWritten` and the file position is advanced from the same local value.

The authoritative SDK source, command-line kernel template, and Visual Studio kernel template contain the identical correction. No storage semantics, filesystem-provider ABI, FAT32 behaviour, request queues, partition discovery, or process executable-loading behaviour changed in this corrective release.
