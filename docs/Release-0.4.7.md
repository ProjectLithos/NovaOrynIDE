# NovaOryn IDE 0.4.7

NovaOryn IDE 0.4.7 bundles NovaOryn SDK 0.41.1 and fixes a Debug-build stall during NativeAOT source-map generation.

The affected build previously stopped after reporting that the pinned LLVM `llvm-symbolizer` did not support an explicit `--pdb` override. The linker then waited indefinitely if the symbolizer became stuck resolving the EFI CodeView PDB. The updated linker uses a single asynchronous symbolization session, drains child-process output safely, reports progress before source-map generation, and enforces a hard timeout with process-tree termination.
