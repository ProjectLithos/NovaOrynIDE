# NovaOryn Userland

NovaOryn user-facing functionality is grouped below `Userland` rather than mixed into kernel boot or HAL code.

Projects:

- `NovaOryn.Userland` - aggregate project.
- `Commands` - command grammars and user-facing command contracts.
- `Settings` - user-configurable setting categories.
- `Fonts` - userland font catalog and font-facing contracts.
- `Images` - userland image contracts.
- `Drivers` - user-visible driver/device contracts; privileged MMIO/PIO stays in the kernel/HAL.

The starter command set is `help`, `clear`/`cls`, `echo`, `info`/`system`, `uptime`, `memory`, `drivers`, `devices`, `font`, `buffering`, and `keyboard`.

`NovaOryn.Kernel.CommandLine` remains the privileged terminal/dispatch boundary for the freestanding bootstrap. Userland projects must not directly access hardware ports, MMIO, page tables, interrupt controllers, or other privileged mechanisms.
