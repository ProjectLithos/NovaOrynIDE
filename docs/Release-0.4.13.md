# NovaOryn IDE 0.4.13

## Debugger interrupt-stop fix

Debug-mode QEMU no longer places a software breakpoint on `NovaOrynX64InterruptCommon`. That common x64 entry is shared by CPU exceptions and hardware IRQs, so breaking there caused QEMU to repeatedly enter `[Stopped]` on timer, keyboard and device interrupts.

The IDE now arms only the configured vector-specific exception entry stubs (`NovaOrynX64InterruptStub0` through `NovaOrynX64InterruptStub31`). Hardware IRQs therefore run continuously while source, panic and selected CPU-exception breakpoints remain available.
