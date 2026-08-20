# NovaOryn 0.10.9

Roadmap item 17: System calls.

- x64 SYSCALL/SYSRET entry and return path.
- SWAPGS per-CPU syscall state and dedicated 32 KiB kernel syscall stack.
- Shared managed dispatcher exported directly through NativeAOT.
- NovaOryn Get/Set/Event methodology.
- Linux-style numeric syscall methodology with negative errno-like results.
- Microsoft Windows/NT-style numeric service methodology with NTSTATUS-style results.
- Bounded custom handler registration for all three methodologies.
- Guarded user copy APIs and SMAP activation when supported.
- Independent system-call methodology tests.

Process creation, executable loading and a complete Linux/NT compatibility personality remain later layers.
