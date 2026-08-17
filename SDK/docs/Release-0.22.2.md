# NovaOryn 0.22.2

NovaOryn 0.22.2 is the complete RTC/CMOS type-correction release for the timer and clock-source work introduced in 0.22.0.

## RTC/CMOS .NET type correction

- Removes `UInt32` literal suffixes from RTC register indices, byte masks, byte counters, and byte zero values where the SDK API intentionally uses normal .NET `Byte` values.
- Keeps `UInt32` suffixes only where the storage type is actually `UInt32`, such as the RTC polling limit and polling counter.
- Keeps the SDK source, command-line kernel template, and Visual Studio kernel template byte-for-byte synchronized.
- Preserves HPET, Local APIC timer, TSC, invariant-TSC, and RTC/CMOS facilities introduced in 0.22.0.

## Validation target

`Build-NovaOryn.bat` must compile `NovaOryn.Kernel.Time` without the CS0266 and CS1503 `UInt32`-to-`Byte` conversion failures reported by 0.22.1.
