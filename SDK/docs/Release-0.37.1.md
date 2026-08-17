# NovaOryn 0.37.1

NovaOryn 0.37.1 makes NativeAOT source-line symbolization compatible with the repository-pinned LLVM 22.1.8 toolchain. The linker probes `llvm-symbolizer --help` and only supplies an explicit `--pdb` option when the installed tool supports it. Otherwise it symbolizes the linked EFI image and allows llvm-symbolizer to follow the PE/COFF CodeView PDB reference embedded by lld-link.

This preserves bounded positional address batching from 0.37.0 while removing the `unknown argument --pdb=...` failure seen with LLVM 22.1.8.
