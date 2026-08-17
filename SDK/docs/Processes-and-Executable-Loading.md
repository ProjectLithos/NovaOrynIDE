# Processes and executable loading

NovaOryn 0.11.0 implements roadmap item 18 as a freestanding x64 process foundation.

`NovaOryn.Kernel.Processes` accepts an executable already present in memory and validates either a System V x86-64 ELF64 `ET_EXEC` image or a Microsoft x86-64 PE32+ image. Loading from named files is intentionally deferred until the storage/filesystem layer exists.

Each process receives a private lower-half four-level page-table hierarchy. The higher-half kernel entries are shared from the active kernel root, preserving kernel services and the syscall stack while preventing one process from inheriting another process's user mappings. Loadable image pages are backed by PMM allocations, zero-filled before file bytes are copied, and mapped with read/write/execute permissions derived from the executable. A 1 MiB non-executable user stack is created near the top of the canonical user half.

The public API uses ordinary .NET-style value types and `Boolean`/value returns: `KernelProcesses.Initialize`, `TryCreateFromImage`, `TryGetProcess`, `TryTerminate`, and `TryStart`. `ProcessExecutableMath` provides deterministic, allocation-free ELF64/PE32+ inspection and segment decoding for custom loaders and tests.

`TryStart` switches CR3 to the process root and uses the native x64 `IRETQ` ring-3 transition. Once in user mode, the system-call boundary from 0.10.0 remains mapped through the shared kernel half, allowing a loaded program to execute `SYSCALL` and return with `SYSRET`.

Dynamic ELF linking, PE imports, ASLR/relocation processing, named-file loading, multi-process scheduler integration, and driver/file-backed process services are later layers. ELF64 PIE (`ET_DYN`) is rejected rather than being loaded incorrectly without relocation support.
