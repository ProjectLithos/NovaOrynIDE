using System;
using NovaOryn.Kernel.Acpi;
using NovaOryn.Kernel.Internal.X64;

namespace NovaOryn.Kernel.Time;

/// <summary>Provides the architecture-neutral kernel monotonic clock and programmable timer API.</summary>
public static class KernelTime
{
    private const UInt64 CalibrationNanoseconds = 10000000UL;
    private const UInt64 HpetConfigOffset = 0x10UL, HpetCounterOffset = 0xF0UL;
    private const UInt64 LocalApicSpurious = 0xF0UL, LocalApicLvtTimer = 0x320UL, LocalApicInitialCount = 0x380UL, LocalApicCurrentCount = 0x390UL, LocalApicDivide = 0x3E0UL;
    private const UInt32 LocalApicMasked = 1U << 16, LocalApicPeriodic = 1U << 17, DivideBy16 = 3U;
    private static Boolean _initialized, _hpet64, _hpetAvailable, _tscAvailable, _invariantTsc, _localApicTimer;
    private static KernelClockSource _source;
    private static UInt64 _hpetBase, _hpetPeriodFs, _hpetStart, _tscStart, _tscFrequency, _localApicBase, _localApicFrequency;

    /// <summary>Initializes HPET-backed monotonic time, invariant-TSC calibration, and Local APIC timer calibration.</summary>
    public static Boolean Initialize()
    {
        _initialized = false; _source = KernelClockSource.None; _hpetAvailable = false; _tscAvailable = Native.SupportsTsc(); _invariantTsc = _tscAvailable && Native.SupportsInvariantTsc(); _localApicTimer = false;
        if (!KernelAcpi.IsInitialized() || !KernelAcpi.TryGetHpet(out AcpiHpetInfo hpet) || hpet.AddressSpace != (Byte)0U) return false;
        _hpetBase = hpet.BaseAddress;
        UInt64 capabilities = Native.ReadMmio64(_hpetBase);
        _hpetPeriodFs = capabilities >> 32;
        _hpet64 = (capabilities & (1UL << 13)) != 0UL;
        if (_hpetPeriodFs == 0UL || _hpetPeriodFs > 100000000UL) return false;
        UInt64 config = Native.ReadMmio64(_hpetBase + HpetConfigOffset);
        if (!Native.WriteMmio64(_hpetBase + HpetConfigOffset, config | 1UL)) return false;
        _hpetStart = ReadHpetCounter();
        _hpetAvailable = true;
        _source = KernelClockSource.Hpet;
        if (_invariantTsc)
        {
            UInt64 startHpet = ReadHpetCounter(); UInt64 startTsc = Native.ReadTimestampCounter();
            if (WaitHpetNanoseconds(CalibrationNanoseconds))
            {
                UInt64 elapsedNs = HpetElapsedNanoseconds(startHpet, ReadHpetCounter());
                UInt64 elapsedTsc = Native.ReadTimestampCounter() - startTsc;
                _tscFrequency = KernelTimeMath.CalibrateFrequency(elapsedTsc, elapsedNs);
                if (_tscFrequency >= 1000000UL) { _tscStart = Native.ReadTimestampCounter(); _source = KernelClockSource.InvariantTsc; }
            }
        }
        if (KernelAcpi.TryGetLocalApicAddress(out _localApicBase)) _localApicTimer = CalibrateLocalApicTimer();
        KernelRtcCmos.Initialize();
        _initialized = true;
        return true;
    }

    /// <summary>Gets whether timing services completed initialization.</summary>
    public static Boolean IsInitialized => _initialized;
    /// <summary>Gets the selected monotonic clock source.</summary>
    public static KernelClockSource GetClockSource() => _source;
    /// <summary>Gets a printable name for the selected monotonic clock source.</summary>
    public static String GetClockSourceName() => _source == KernelClockSource.InvariantTsc ? "InvariantTSC" : _source == KernelClockSource.Hpet ? "HPET" : "None";
    /// <summary>Gets the active clock frequency in ticks per second.</summary>
    public static UInt64 GetClockFrequencyHz() => _source == KernelClockSource.InvariantTsc ? _tscFrequency : HpetFrequencyHz();
    /// <summary>Gets the HPET frequency in hertz when initialized.</summary>
    public static UInt64 GetHpetFrequencyHz() => _hpetAvailable ? HpetFrequencyHz() : 0UL;
    /// <summary>Gets the HPET main-counter period in femtoseconds when initialized.</summary>
    public static UInt64 GetHpetPeriodFemtoseconds() => _hpetAvailable ? _hpetPeriodFs : 0UL;
    /// <summary>Gets the HPET-calibrated TSC frequency when calibration succeeded.</summary>
    public static UInt64 GetTscFrequencyHz() => _tscFrequency;
    /// <summary>Gets the calibrated Local APIC timer frequency after divide-by-16 configuration.</summary>
    public static UInt64 GetLocalApicTimerFrequencyHz() => _localApicFrequency;
    /// <summary>Gets a capability snapshot for scheduling and diagnostics.</summary>
    public static KernelTimeCapabilities GetCapabilities() => new(_source, GetClockFrequencyHz(), _hpetAvailable, _tscAvailable, _invariantTsc, KernelRtcCmos.IsInitialized(), _localApicTimer, _localApicFrequency);

    /// <summary>Gets monotonic nanoseconds elapsed since timing-service initialization.</summary>
    public static UInt64 GetMonotonicNanoseconds()
    {
        if (!_initialized) return 0UL;
        if (_source == KernelClockSource.InvariantTsc) return KernelTimeMath.TicksToNanoseconds(Native.ReadTimestampCounter() - _tscStart, _tscFrequency);
        return HpetElapsedNanoseconds(_hpetStart, ReadHpetCounter());
    }

    /// <summary>Creates an absolute monotonic deadline from a relative nanosecond delay.</summary>
    public static Boolean TryCreateDeadline(UInt64 delayNanoseconds, out UInt64 deadlineNanoseconds)
    {
        deadlineNanoseconds = 0UL; if (!_initialized) return false;
        UInt64 now = GetMonotonicNanoseconds(); if (0xFFFFFFFFFFFFFFFFUL - now < delayNanoseconds) return false;
        deadlineNanoseconds = now + delayNanoseconds; return true;
    }

    /// <summary>Determines whether an absolute monotonic deadline has been reached.</summary>
    public static Boolean HasReached(UInt64 deadlineNanoseconds) => _initialized && GetMonotonicNanoseconds() >= deadlineNanoseconds;

    /// <summary>Performs a bounded monotonic busy delay useful only during early bootstrap.</summary>
    public static Boolean DelayNanoseconds(UInt64 nanoseconds)
    {
        if (!TryCreateDeadline(nanoseconds, out UInt64 deadline)) return false;
        while (!HasReached(deadline)) { }
        return true;
    }

    /// <summary>Arms the calibrated Local APIC timer for one interrupt.</summary>
    public static Boolean TryArmOneShot(Byte vector, UInt64 delayNanoseconds) => ProgramLocalApicTimer(vector, delayNanoseconds, KernelTimerMode.OneShot);
    /// <summary>Arms the calibrated Local APIC timer for periodic interrupts.</summary>
    public static Boolean TryArmPeriodic(Byte vector, UInt64 periodNanoseconds) => ProgramLocalApicTimer(vector, periodNanoseconds, KernelTimerMode.Periodic);
    /// <summary>Stops and masks the Local APIC timer.</summary>
    public static Boolean CancelLocalApicTimer()
    {
        if (!_localApicTimer) return false;
        return Native.WriteMmio32(_localApicBase + LocalApicInitialCount, 0U) && Native.WriteMmio32(_localApicBase + LocalApicLvtTimer, LocalApicMasked);
    }

    private static Boolean ProgramLocalApicTimer(Byte vector, UInt64 nanoseconds, KernelTimerMode mode)
    {
        if (!_localApicTimer || vector < (Byte)32U || nanoseconds == 0UL) return false;
        UInt64 ticks = KernelTimeMath.NanosecondsToTicksCeiling(nanoseconds, _localApicFrequency);
        if (ticks == 0UL || ticks > 0xFFFFFFFFU) return false;
        UInt32 lvt = vector; if (mode == KernelTimerMode.Periodic) lvt |= LocalApicPeriodic;
        return Native.WriteMmio32(_localApicBase + LocalApicDivide, DivideBy16) && Native.WriteMmio32(_localApicBase + LocalApicLvtTimer, lvt) && Native.WriteMmio32(_localApicBase + LocalApicInitialCount, (UInt32)ticks);
    }

    private static Boolean CalibrateLocalApicTimer()
    {
        if (_localApicBase == 0UL) return false;
        UInt32 spurious = Native.ReadMmio32(_localApicBase + LocalApicSpurious);
        if ((spurious & 0xFFU) < 0x10U) spurious = (spurious & 0xFFFFFF00U) | 0xFFU;
        if (!Native.WriteMmio32(_localApicBase + LocalApicSpurious, spurious | 0x100U)) return false;
        if (!Native.WriteMmio32(_localApicBase + LocalApicDivide, DivideBy16)) return false;
        if (!Native.WriteMmio32(_localApicBase + LocalApicLvtTimer, LocalApicMasked)) return false;
        if (!Native.WriteMmio32(_localApicBase + LocalApicInitialCount, 0xFFFFFFFFU)) return false;
        UInt64 startHpet = ReadHpetCounter();
        if (!WaitHpetNanoseconds(CalibrationNanoseconds)) return false;
        UInt32 current = Native.ReadMmio32(_localApicBase + LocalApicCurrentCount);
        if (!Native.WriteMmio32(_localApicBase + LocalApicInitialCount, 0U)) return false;
        UInt64 elapsedNs = HpetElapsedNanoseconds(startHpet, ReadHpetCounter());
        UInt64 elapsedTicks = (UInt64)(0xFFFFFFFFU - current);
        _localApicFrequency = KernelTimeMath.CalibrateFrequency(elapsedTicks, elapsedNs);
        return _localApicFrequency != 0UL;
    }

    private static Boolean WaitHpetNanoseconds(UInt64 nanoseconds)
    {
        UInt64 requiredTicks = HpetTicksForNanoseconds(nanoseconds); if (requiredTicks == 0UL) return false;
        UInt64 start = ReadHpetCounter(); UInt64 maximumIterations = 100000000UL;
        for (UInt64 iteration = 0UL; iteration < maximumIterations; iteration++) if (HpetDelta(start, ReadHpetCounter()) >= requiredTicks) return true;
        return false;
    }

    private static UInt64 ReadHpetCounter()
    {
        UInt64 value = Native.ReadMmio64(_hpetBase + HpetCounterOffset);
        return _hpet64 ? value : value & 0xFFFFFFFFUL;
    }
    private static UInt64 HpetDelta(UInt64 start, UInt64 end) => _hpet64 ? end - start : (UInt32)end - (UInt32)start;
    private static UInt64 HpetElapsedNanoseconds(UInt64 start, UInt64 end) => KernelTimeMath.HpetTicksToNanoseconds(HpetDelta(start, end), _hpetPeriodFs);
    private static UInt64 HpetFrequencyHz() => _hpetPeriodFs == 0UL ? 0UL : 1000000000000000UL / _hpetPeriodFs;
    private static UInt64 HpetTicksForNanoseconds(UInt64 nanoseconds)
    {
        if (_hpetPeriodFs == 0UL) return 0UL;
        UInt64 whole = nanoseconds / 1000000UL; UInt64 remainder = nanoseconds % 1000000UL;
        if (whole > 0xFFFFFFFFFFFFFFFFUL / 1000000000000UL) return 0xFFFFFFFFFFFFFFFFUL;
        UInt64 femtoseconds = whole * 1000000000000UL + remainder * 1000000UL;
        UInt64 ticks = femtoseconds / _hpetPeriodFs; if ((femtoseconds % _hpetPeriodFs) != 0UL) ticks++;
        return ticks;
    }
}
