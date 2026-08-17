# NovaOryn 0.22.0

NovaOryn 0.22.0 completes the x64 HPET/timers clock stack as a coherent SDK facility.

- Keeps ACPI HPET as the bootstrap reference clock and exposes HPET availability, frequency, and femtosecond period through `KernelHpet`.
- Adds explicit x64 TSC capability detection and exposes raw serialized counter reads plus HPET-calibrated TSC frequency through `KernelTsc`.
- Preserves invariant-TSC selection as the preferred monotonic clock source only after CPUID invariant-TSC detection and successful HPET calibration.
- Keeps the Local APIC timer processor-local and exposes calibrated one-shot, periodic, cancel, and frequency operations through `KernelLocalApicTimer`.
- Adds a PC-compatible RTC/CMOS calendar facility with update-in-progress synchronization, stable double sampling, BCD/binary conversion, 12/24-hour conversion, Gregorian validation, and ACPI FADT century-register support.
- Extends `KernelTimeCapabilities` so callers can inspect HPET, TSC, invariant-TSC, RTC/CMOS, and Local APIC timer availability without knowing low-level delivery details.
- Updates generated kernels to report every timing facility and the RTC calendar during startup using high-level SDK APIs only.
- Extends timer methodology tests with RTC conversion, century, 12-hour, and leap-year validation cases.
