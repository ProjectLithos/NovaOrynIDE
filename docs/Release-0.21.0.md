# NovaOryn IDE 0.21.0

NovaOryn IDE 0.21.0 formalises the filesystem VFS contract used by kernels generated with the bundled SDK.

The VFS now owns mount namespaces and mount routing, file handles and synchronous read/write/seek/flush, directory handles and enumeration, effective permissions, provider feature discovery, and safe unmount semantics. Filesystem implementations remain below the VFS through `KernelFileSystemCallbacks`.

FatFs is upgraded to the full provider contract with FAT directory enumeration and read-only/hidden/system permission metadata. Permission mutation is explicitly unsupported by the current FAT provider rather than silently succeeding.

Async I/O is reserved in the ABI vocabulary but is not implemented in 0.21.0. The synchronous interface remains authoritative until a later request/completion model is introduced.
