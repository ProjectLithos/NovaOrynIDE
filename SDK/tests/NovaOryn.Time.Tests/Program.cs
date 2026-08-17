using NovaOryn.Kernel.Time;

static void Assert(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("[FAIL] " + name);
    Console.WriteLine("[ OK ] " + name);
}

Assert(KernelTimeMath.HpetTicksToNanoseconds(10_000_000, 100_000_000) == 1_000_000_000, "HPET femtosecond periods convert to nanoseconds.");
Assert(KernelTimeMath.CalibrateFrequency(25_000_000, 10_000_000) == 2_500_000_000, "Calibration derives source frequency from elapsed time.");
Assert(KernelTimeMath.NanosecondsToTicksCeiling(1, 1_000_000_000) == 1, "Timer conversion rounds upward for nonzero delays.");
Assert(KernelTimeMath.NanosecondsToTicksCeiling(1_500_000, 1_000_000) == 1500, "Timer conversion handles millisecond-scale periods.");
Assert(KernelTimeMath.TicksToNanoseconds(2_500_000_000, 2_500_000_000) == 1_000_000_000, "Calibrated ticks convert back to monotonic nanoseconds.");
Assert(KernelTimeMath.NanosecondsToTicksCeiling(0, 1000) == 0, "Zero-duration timer requests remain zero ticks.");
Assert(KernelRtcMath.BcdToBinary(0x59) == 59, "RTC packed BCD converts to binary.");
Assert(KernelRtcMath.DecodeHour(0x92, false, false) == 12, "RTC 12-hour PM values convert to 24-hour time.");
Assert(KernelRtcMath.DecodeHour(0x12, false, false) == 0, "RTC 12 AM converts to hour zero.");
Assert(KernelRtcMath.DecodeYear(0x26, 0x20, true, false) == 2026, "RTC ACPI century register forms a full Gregorian year.");
Assert(KernelRtcMath.DecodeYear(0x99, 0, false, false) == 1999, "RTC fallback pivot handles legacy two-digit years.");
Assert(KernelRtcMath.IsValid(2024, 2, 29, 23, 59, 59), "RTC validation accepts Gregorian leap day.");
Assert(!KernelRtcMath.IsValid(2100, 2, 29, 0, 0, 0), "RTC validation rejects non-leap-century February 29.");
Console.WriteLine("[ OK ] HPET, Local APIC timer, TSC, RTC/CMOS, and invariant-TSC methodology tests passed.");
