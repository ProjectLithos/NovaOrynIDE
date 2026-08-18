# NovaOryn IDE 0.3.3

NovaOryn IDE 0.3.3 implements roadmap item **Physical-machine debugger transport**.

## Physical-machine debugging

- Physical x86_64 targets configured in Target Manager can now be used by the normal IDE **Debug** workflow.
- The backend connects directly to a hardware debugger or target GDB stub using the GDB Remote Serial Protocol over a configurable host/port.
- A transport probe verifies that the endpoint is reachable before a debug session.
- The Debug build's existing `NovaOrynDebugImageAnchor` rendezvous is reused on hardware. `Entry.asm` deliberately leaves the relocated anchor address in `R9`; the IDE reads it from the target, derives the EFI relocation delta, arms source/exception/panic breakpoints, moves RIP to `NovaOrynDebugResume`, and releases the kernel before `KMain`.
- Existing debugger facilities therefore work through the same transport abstraction: Pause/Continue/Step, C# source breakpoints, registers, memory, call stack, page-table inspection, memory-map/APIC/syscall explorers, crash dumps and panic/exception stops.
- Optional Windows COM-port capture reads physical serial output at the target's configured baud rate and feeds it into NovaOryn run output and structured telemetry ingestion.
- Physical Release Run is rejected cleanly because the IDE cannot power-cycle or boot arbitrary hardware. The user boots the newly-built image on the target, while Debug performs build + attach.

## IDE

- Added **NovaOryn → Engineering → Physical-machine Debugger Transport**.
- The view lists physical targets, shows GDB/serial endpoints, can activate a target and can test GDB transport reachability.
- Target Manager now documents physical debugging as available rather than reserved for future work.
