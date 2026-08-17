# NovaOryn 0.22.1

NovaOryn 0.22.1 is a corrective release for the timer and clock-source work introduced in 0.22.0.

## RTC/CMOS build correction

- Corrects the RTC/CMOS I/O-port and status-bit constant declarations so their compile-time literals match the declared `UInt16` and `Byte` types.
- Keeps the SDK source, command-line kernel template, and Visual Studio kernel template synchronized.
- Preserves the HPET, Local APIC timer, TSC, invariant-TSC, and RTC/CMOS facilities introduced in 0.22.0.

## Validation target

`Build-NovaOryn.bat` must compile `NovaOryn.Kernel.Time` without the CS0266 narrowing-conversion failures reported by 0.22.0.
