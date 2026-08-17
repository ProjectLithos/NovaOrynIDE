# NovaOryn IDE 0.1.45

NovaOryn IDE 0.1.45 implements debugger roadmap items 9 and 10.

## Page-table and heap inspection

- Adds active x64 CR3/PML4/PDPT/PD/PT translation for any debugger address expression.
- Reads page-table entries through QEMU physical-memory monitor access and decodes access/cache/NX/global/large-page flags.
- Resolves 4 KiB, 2 MiB and 1 GiB mappings to their final physical byte address.
- Adds a Kernel Heap inspector backed by NativeAOT global symbols and the live first-fit heap metadata table.
- Shows committed, allocated, free and peak bytes plus every allocated/free block and allocation token.

## Crash-dump debugging

- Captures `.nodump.json` files under `<OS>\.novaoryn\crash-dumps`.
- Saves registers, source stop, named locals, mixed disassembly, x64 unwind call stack, page-table state, heap state, and code/stack memory.
- Automatically captures dumps on enabled CPU exceptions and fatal/panic stops.
- Reopens saved dumps in the NovaOryn Debug inspector without a running QEMU instance.

The existing NovaOryn cat application icon and all 0.1.44 CPU/thread/process-context and x64 unwind functionality remain in place.
