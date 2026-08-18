# NovaOryn SDK 0.41.1

## NativeAOT source-debug symbolizer stall fix

Debug source-map generation now resolves the complete linked instruction-address set in one `llvm-symbolizer` process instead of repeatedly reloading the same NativeAOT PDB in batches.

The linker drains stdout/stderr before sending symbolizer input, sends input asynchronously, tolerates a child-process broken pipe so the real LLVM diagnostic is retained, and enforces a 120-second hard timeout. If LLVM becomes stuck while loading or resolving a CodeView PDB, NovaOryn terminates the child process and reports an explicit failure instead of leaving the OS build waiting forever.

The linker also reports the number of instruction addresses being resolved before symbolization starts so long-running debug-map work is visible in the IDE build output.
