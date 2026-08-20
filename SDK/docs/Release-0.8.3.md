# NovaOryn 0.10.5

NovaOryn 0.10.5 changes QEMU CPU allocation from a hard-coded single virtual CPU to 50% of the host logical processors.

## QEMU CPU allocation

`NovaOryn.QemuLauncher` reads `Environment.ProcessorCount` and calculates the QEMU processor count as `max(1, ceil(hostLogicalProcessors / 2))`. In integer arithmetic this is `(hostLogicalProcessorCount + 1) / 2` for hosts with more than one available logical processor.

Examples:

- 1 host logical processor -> 1 QEMU CPU
- 4 host logical processors -> 2 QEMU CPUs
- 8 host logical processors -> 4 QEMU CPUs
- 12 host logical processors -> 6 QEMU CPUs
- 16 host logical processors -> 8 QEMU CPUs
- 24 host logical processors -> 12 QEMU CPUs

The launcher prints both counts before QEMU starts and writes `hostLogicalProcessorCount` and `qemuProcessorCount` into `NovaOryn.Run.json` after successful runtime acceptance.

The generated QEMU command now supplies the calculated value to `-smp`, so Visual Studio F5/Ctrl+F5 and normal NovaOryn run flows use the same dynamic CPU allocation.

## Validation

`NovaOryn.BuildPolicy.Tests` now requires dynamic host CPU detection, the 50-percent calculation, and use of the calculated value in `-smp`. It also rejects reintroduction of the old hard-coded `-smp 1` launch.
