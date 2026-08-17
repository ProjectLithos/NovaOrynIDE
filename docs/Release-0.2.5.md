# NovaOryn IDE 0.2.5

## Item 16 - Binary / Symbol Explorer

NovaOryn IDE 0.2.5 adds a first-class Binary / Symbol Explorer under **NovaOryn -> Engineering**.

The explorer inventories OS and bundled-SDK build artifacts, including EFI/PE images, COFF objects, libraries, PDBs, linker maps and `NovaOryn.DebugSymbols.json`. It can inspect PE/COFF architecture, image base, entry point and sections; enumerate native symbols through the bundled LLVM `llvm-nm`; enumerate PDB public/global symbols through `llvm-pdbutil`; and display NovaOryn source-line mappings from the generated debug-symbol map.

Symbol results can be filtered by symbol name or source path. Artifact inspection is restricted to the current NovaOryn OS and bundled SDK artifact roots.
