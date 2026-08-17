# NovaOryn 0.0.59

## IDT and CPU exceptions

This release adds the complete x64 interrupt-descriptor and exception-dispatch foundation.

- Adds `NovaOryn.Interrupts.Contracts` with a stable managed/native `InterruptContext`, handler results, registration handles, vector allocation and diagnostic contracts.
- Adds `NovaOryn.Architecture.X64.Interrupts` with all 256 IDT gates, configurable DPL, interrupt/trap gates, IST selection, registration, removal and fatal defaults.
- Adds a driver-vector allocator for vectors `0x40` through `0xEF`.
- Adds 256 native entry stubs and normalises exceptions with and without architectural error codes.
- Captures RIP, CS, RFLAGS, RSP, SS, RAX-R15, CR0, CR2, CR3, CR4, processor ID and privilege-transition state.
- Uses the TSS emergency IST assignments for double fault, NMI and optional machine check.
- Adds readable fatal diagnostics for divide error, invalid opcode, GPF, page fault, double fault, stack-segment fault, NMI and machine check.
- Unhandled and fatal vectors stop through a non-returning `CLI`/`HLT` loop.
- Records explicit placeholders for thread/process identity and stack unwinding until those subsystems exist.

## Installation policy

Extract `NovaOryn-ChangedFiles-0.0.59.zip` into `C:\NovaOryn`, commit and push the source changes, and only then run the normal build/toolchain workflow.
