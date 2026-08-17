# NovaOryn IDE 0.2.1

Base: NovaOryn IDE 0.1.53 (latest source at implementation time).

## Item 14 — Target Manager

- Adds a first-class Target Manager under NovaOryn > Engineering.
- Persists target definitions in each OS as `NovaOryn.Targets.json` (schema v1).
- Supports QEMU, physical-machine and remote-debug target definitions.
- Supports x86_64, ARM64 and RISC-V 64 target metadata.
- QEMU profiles include CPU count, RAM, machine, accelerator and display.
- Physical profiles include GDB host/port and serial transport metadata.
- One target is active at a time and the active target survives IDE restarts.
- Run/Debug validates the active target and exports the target contract to the SDK process.
- x86_64 QEMU debugger launch consumes CPU/RAM/machine/accelerator/display from the active target.
- Unsupported architecture or physical/remote launches fail cleanly rather than silently using QEMU defaults.
- Adds `Verify-NovaOrynIDETargetManager.cjs` to the build verification pipeline.

Physical transport execution itself remains item 22; 0.2.1 deliberately establishes the persistent target model now so item 22 can plug into it without changing project format.
