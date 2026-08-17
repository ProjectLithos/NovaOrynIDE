# Nova Oryn OS SDK 0.11.2

## Corrective process-loader build

NovaOryn 0.11.2 corrects the C# fixed-buffer access used by roadmap item 18.

- Removes redundant `fixed` statements around `ProcessRecord*` fixed buffers.
- Uses direct indexed access for process page-table and allocation metadata.
- Applies the same correction to the SDK source, command-line kernel template, and Visual Studio kernel template.
- Retains the 0.11.1 NASM `sysret` correction and all item-18 process/executable-loading functionality.

This is a source-level corrective release over 0.11.1; no roadmap scope is added or removed.
