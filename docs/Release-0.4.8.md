# NovaOryn IDE 0.4.9

Debug source-map generation no longer sends every native instruction address through `llvm-symbolizer`.

NovaOryn now reads the NativeAOT PDB CodeView line table directly with the pinned `llvm-pdbutil`, converts section-relative line addresses to linked PE/COFF virtual addresses, and emits `NovaOryn.DebugSymbols.json` from actual source sequence points. Large kernels therefore scale with source lines rather than instruction count.

A bounded legacy symbolizer fallback remains for small images only. Large images fail immediately with a diagnostic if their PDB line table is unreadable rather than appearing to hang.
