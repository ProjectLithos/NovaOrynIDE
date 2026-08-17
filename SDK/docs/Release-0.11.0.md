# NovaOryn 0.11.0

Roadmap item 18: Processes and executable loading.

- New `NovaOryn.Kernel.Processes` SDK assembly.
- Bounded process table and lifecycle snapshots.
- Private per-process lower-half x64 page tables with the higher-half kernel mapping shared.
- PMM-backed executable segments and a 1 MiB user stack.
- ELF64 x86-64 `ET_EXEC` validation and loading.
- PE32+ x86-64 validation and loading.
- Page permissions derived from executable segment/section flags.
- Native `IRETQ` transition into ring 3.
- Existing SYSCALL/SYSRET boundary remains available after process entry.
- Independent process/executable parser tests and template integration.

Driver framework is roadmap item 19.
