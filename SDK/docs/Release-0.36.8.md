# NovaOryn 0.36.8

NovaOryn 0.36.8 adds source-accurate Debug symbol production for IDE kernel debugging.

- Debug builds force portable managed PDB generation and disable Roslyn optimization for the bootstrap graph.
- Direct ILC receives `-g`, matching NativeAOT's debug-symbol mode.
- LLD links `MinimalKernel.pdb` with `/debug:full`.
- The linker uses LLVM `llvm-objdump` and `llvm-symbolizer` to create `NovaOryn.DebugSymbols.json`, mapping exact source files and lines to linked native addresses.
- Debug `Entry.asm` exports `NovaOrynDebugImageAnchor` and emits a one-byte `int3` anchor. The IDE consumes this stop internally to calculate the UEFI image relocation delta before arming user breakpoints.
- Release builds contain no debug anchor and continue to omit the native source-debug pipeline.
