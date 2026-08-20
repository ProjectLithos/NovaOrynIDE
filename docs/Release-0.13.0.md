# NovaOryn IDE 0.13.0

NovaOryn IDE 0.13.0 makes the SDK's existing deterministic fault-injection engine effective in real kernel subsystem paths.

## Faults now wired into runtime paths

- **Allocation failure** — `KernelHeap.TryAllocate` can deterministically report out-of-memory.
- **I/O timeout** — block read/write/flush operations can return a timeout-style failure before touching the device callback.
- **Dropped interrupt** — a bound driver's interrupt can be acknowledged by the framework without dispatching its handler.
- **Device reset** — an unexpected reset can be injected at interrupt delivery and routed through the driver's real reset lifecycle callback.
- **Bad DMA** — DMA capability acquisition can fail deterministically, and the injector exposes a DMA-address corruption helper for driver tests.
- **Corrupt packet** — queued receive packets can have one selected byte corrupted while the stack processes them; the original buffer is restored afterward.
- **Page fault** — virtual translation can deterministically behave as if the selected mapping were absent.
- **CPU offline** — application-processor startup can leave a selected CPU offline so SMP degradation paths are exercised.
- **Filesystem error** — FatFs open/read/write/flush operations can fail at the filesystem boundary.

## Deterministic rules

Rules select a fault kind and optional subsystem, can wait for N observations before firing, can repeat a bounded number of times, and carry a 64-bit parameter. This keeps failure tests repeatable instead of probabilistic. Up to eight rules can be armed concurrently without allocation, reflection, exceptions or GC dependency in the kernel path.

`KernelFaultInjection.TryArm(kind, subsystem, triggerAfter, repeatCount, parameter, out ruleId)` and `ShouldInject(...)` provide the direct freestanding API. DMA and packet corruption helpers are included for tests that need controlled data damage rather than simple operation failure.

## Test Explorer

The bundled `NovaOryn.FaultInjection.Tests` project is automatically discovered by the existing Test Explorer. It validates all nine fault kinds, trigger-after/repeat/disarm semantics, subsystem scoping, and the DMA/packet corruption helpers.

The fault hooks are present in both the bundled SDK source and the kernel-template copies, so newly generated operating systems receive the same injection points.

## QEMU hardware-matrix reliability correction

- The matrix now begins with a known-good control matching the established NovaOryn QEMU boot path: Q35, TCG, 2 CPUs, 512 MiB, VirtIO block, VirtIO GPU, no optional network device and no xHCI device.
- Balanced testing varies one hardware dimension from that control rather than treating GOP + networking + xHCI as the baseline.
- Full Cartesian testing runs the known-good control before the Cartesian hardware combinations.
- If the control boot fails, remaining cases are skipped instead of generating hundreds of misleading failures.
- Failed cases report the exact QEMU arguments and the serial-log tail directly in matrix output.
- Matrix execution uses SDL like the proven NovaOryn QEMU launcher rather than introducing a headless-display difference into the control.
