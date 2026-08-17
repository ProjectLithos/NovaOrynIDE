using System;

namespace NovaOryn.Kernel.Time;

/// <summary>Provides deterministic conversion and validation helpers for RTC/CMOS calendar values.</summary>
public static class KernelRtcMath
{
    /// <summary>Converts one packed BCD byte to binary.</summary>
    public static Byte BcdToBinary(Byte value) => (Byte)(((value >> 4) * 10U) + (value & 0x0FU));

    /// <summary>Converts a CMOS hour value to a 24-hour binary value.</summary>
    public static Byte DecodeHour(Byte value, Boolean binaryMode, Boolean twentyFourHourMode)
    {
        Boolean pm = (value & 0x80U) != 0U;
        Byte hour = (Byte)(value & 0x7FU);
        if (!binaryMode) hour = BcdToBinary(hour);
        if (twentyFourHourMode) return hour;
        hour = (Byte)(hour % 12U);
        return pm ? (Byte)(hour + 12U) : hour;
    }

    /// <summary>Combines CMOS year and optional century values into a Gregorian year.</summary>
    public static UInt16 DecodeYear(Byte year, Byte century, Boolean hasCentury, Boolean binaryMode)
    {
        UInt16 y = binaryMode ? year : BcdToBinary(year);
        if (hasCentury)
        {
            UInt16 c = binaryMode ? century : BcdToBinary(century);
            return (UInt16)(c * 100U + y);
        }
        return (UInt16)(y >= 70U ? 1900U + y : 2000U + y);
    }

    /// <summary>Gets whether a Gregorian calendar date and time has valid field ranges.</summary>
    public static Boolean IsValid(UInt16 year, Byte month, Byte day, Byte hour, Byte minute, Byte second)
    {
        if (year < 1601U || year > 9999U || month < 1U || month > 12U || day < 1U || hour > 23U || minute > 59U || second > 59U) return false;
        return day <= DaysInMonth(year, month);
    }

    /// <summary>Gets the number of days in a Gregorian calendar month.</summary>
    public static Byte DaysInMonth(UInt16 year, Byte month)
    {
        if (month == 2U) return IsLeapYear(year) ? (Byte)29U : (Byte)28U;
        return month == 4U || month == 6U || month == 9U || month == 11U ? (Byte)30U : (Byte)31U;
    }

    /// <summary>Gets whether a Gregorian year is a leap year.</summary>
    public static Boolean IsLeapYear(UInt16 year) => (year % 4U) == 0U && ((year % 100U) != 0U || (year % 400U) == 0U);
}
