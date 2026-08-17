# NovaOryn IDE 0.2.6

## 17. Memory-map Visualiser

NovaOryn IDE 0.2.6 adds a runtime Memory-map Visualiser under **NovaOryn > Engineering**.

The visualiser reads the authoritative retained final UEFI memory map from a paused NovaOryn Debug kernel. It resolves the relocated `NovaOrynBootContext`, validates the boot-context ABI and `ExitBootServices` state, reads the firmware descriptor buffer through GDB, and displays:

- UEFI descriptor type and NovaOryn category
- physical start/end addresses
- virtual address, page count, byte count and attributes
- total described and immediately usable memory
- memory composition by category
- framebuffer, bootstrap page-table workspace and AP trampoline reservations
- descriptor metadata, map key and capture attempts

The visualiser does not infer runtime memory from QEMU target settings. If the kernel is running, it asks the developer to pause it before reading memory.
