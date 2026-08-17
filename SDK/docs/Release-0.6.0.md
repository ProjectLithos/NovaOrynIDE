# NovaOryn 0.6.0 — Timers and clocks

NovaOryn 0.6.0 implements roadmap item 13 on top of the complete 0.5.1 source tree.

## Added

- `NovaOryn.Kernel.Time`, a freestanding high-level timing assembly.
- ACPI HPET initialization using the hardware block discovered by `KernelAcpi`.
- HPET period validation and monotonic counter conversion to nanoseconds.
- invariant-TSC capability detection through CPUID and HPET-based TSC calibration.
- automatic monotonic source selection: calibrated invariant TSC when available, otherwise HPET.
- monotonic deadlines and deadline testing.
- bounded early-bootstrap busy delay based on the selected monotonic clock.
- Local APIC timer calibration against HPET using divide-by-16 operation.
- one-shot and periodic Local APIC timer programming with an architecture-neutral public API.
- timer cancellation/masking and timing capability reporting.
- overflow-aware integer conversion helpers for HPET periods, calibrated frequencies, timer ticks, and nanoseconds.
- independent `NovaOryn.Time.Tests` methodology tests.
- generated-kernel and Visual Studio template copies of the complete timing assembly.
- visible kernel diagnostics for clock source, frequency, Local APIC timer calibration, and timing-service readiness.

## Native boundary

`NovaOryn.Kernel.X64.LowLevel` now owns the new `RDTSC`, invariant-TSC CPUID probe, and 64-bit MMIO entry points. Existing 32-bit MMIO exports remain owned by `InterruptControllers.asm`; the timing service reuses those symbols instead of defining duplicates.

## Public behavior

`KernelTime.Initialize()` requires successful ACPI initialization and an MMIO HPET. A missing or unusable HPET is reported as initialization failure because HPET is the calibration reference for this stage. Once initialized, consumers may read monotonic nanoseconds, create deadlines, busy-delay during early bootstrap, inspect capabilities, and arm or cancel Local APIC one-shot/periodic timers.

Timer interrupt delivery remains independent of driver-facing PIC/I/O-APIC/MSI/MSI-X routing. Callers supply an allocated IDT vector and the Local APIC timer delivers that vector locally.
