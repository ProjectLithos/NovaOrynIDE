# NovaOryn SDK 0.41.2

Native source-debug map generation now consumes CodeView `DEBUG_S_LINES` records directly through `llvm-pdbutil dump -l`.

The linker maps each PDB section:offset entry through the PE32+ section table and image base to obtain the exact linked address used by NovaOryn's debugger relocation model. The previous 100,000+-instruction `llvm-symbolizer` path is retained only as a bounded fallback for small images.
