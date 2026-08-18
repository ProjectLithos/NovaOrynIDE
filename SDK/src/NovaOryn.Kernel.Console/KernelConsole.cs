using System;
using NovaOryn.Kernel.Internal.X64;
using NovaOryn.Text;

namespace NovaOryn.Kernel.Console;

/// <summary>Provides normal managed C# console output for a freestanding NovaOryn kernel.</summary>
public static class KernelConsole
{
    private const UInt32 DefaultFontSize = BitmapFont.DefaultFontSize;
    private static FramebufferConsole _framebuffer;
    private static Boolean _initialized;
    private static unsafe delegate*<Byte, Boolean> _secondarySerialWriter;

    /// <summary>Gets the exact glyph height used by the framebuffer renderer, in pixels.</summary>
    public static UInt32 FontSize
    {
        get { return _framebuffer.FontSize; }
    }

    /// <summary>Initializes serial output and a 16-pixel framebuffer font.</summary>
    public static Boolean Initialize(BootContext boot)
    {
        return Initialize(boot, DefaultFontSize);
    }

    /// <summary>Initializes serial and framebuffer output with an exact rendered font size.</summary>
    public static Boolean Initialize(BootContext boot, UInt32 fontSize)
    {
        if (!Native.InitializeSerial()) return false;
        if (!_framebuffer.Initialize(boot, fontSize)) return false;
        if (!_framebuffer.Clear()) return false;
        _initialized = true;
        return true;
    }

    /// <summary>Writes a managed string without appending a line terminator.</summary>
    public static Boolean Write(String value)
    {
        if (!_initialized || value == null) return false;
        Int32 length = value.Length;
        Int32 index = 0;
        while (index < length)
        {
            Char character = value[index];
            if ((UInt32)character > 0x7FU) character = (Char)'?';
            if (!WriteRaw((Byte)character)) return false;
            index++;
        }
        return _framebuffer.Flush();
    }

    /// <summary>Writes a managed string followed by a carriage return and line feed as one framebuffer batch.</summary>
    public static Boolean WriteLine(String value)
    {
        if (!_initialized || value == null) return false;
        Int32 length = value.Length;
        Int32 index = 0;
        while (index < length)
        {
            Char character = value[index];
            if ((UInt32)character > 0x7FU) character = (Char)'?';
            if (!WriteRaw((Byte)character)) return false;
            index++;
        }
        if (!WriteRaw((Byte)'\r')) return false;
        if (!WriteRaw((Byte)'\n')) return false;
        return _framebuffer.Flush();
    }

    /// <summary>Writes a Boolean using normal .NET True/False text without allocating a managed string.</summary>
    public static Boolean Write(Boolean value) => Write(StringFormatter.Format(value));

    /// <summary>Writes a Boolean using normal .NET True/False text followed by a line terminator.</summary>
    public static Boolean WriteLine(Boolean value) => WriteLine(StringFormatter.Format(value));

    /// <summary>Writes a text prefix followed by a Boolean without runtime string concatenation.</summary>
    public static Boolean Write(String prefix, Boolean value)
    {
        if (!Write(prefix)) return false;
        return Write(value);
    }

    /// <summary>Writes a text prefix followed by a Boolean and a line terminator without runtime string concatenation.</summary>
    public static Boolean WriteLine(String prefix, Boolean value)
    {
        if (!Write(prefix)) return false;
        return WriteLine(value);
    }


    /// <summary>Writes one unsigned 64-bit value as an ordinary base-10 number.</summary>
    public static unsafe Boolean WriteUInt64(UInt64 value)
    {
        Byte* digits = stackalloc Byte[20];
        Int32 count = 0;
        do
        {
            digits[count] = (Byte)('0' + (Byte)(value % 10UL));
            value /= 10UL;
            count++;
        }
        while (value != 0UL);

        while (count > 0)
        {
            count--;
            if (!WriteRaw(digits[count])) return false;
        }
        return _framebuffer.Flush();
    }

    /// <summary>Writes a byte quantity using B, KiB, MiB, GiB, or TiB as appropriate.</summary>
    public static Boolean WriteByteSize(UInt64 bytes)
    {
        if (bytes >= 1099511627776UL) return WriteScaled(bytes, 1099511627776UL, " TiB");
        if (bytes >= 1073741824UL) return WriteScaled(bytes, 1073741824UL, " GiB");
        if (bytes >= 1048576UL) return WriteScaled(bytes, 1048576UL, " MiB");
        if (bytes >= 1024UL) return WriteScaled(bytes, 1024UL, " KiB");
        if (!WriteUInt64(bytes)) return false;
        return Write(" B");
    }

    /// <summary>Writes a frequency using Hz, kHz, MHz, or GHz as appropriate.</summary>
    public static Boolean WriteFrequency(UInt64 hertz)
    {
        if (hertz >= 1000000000UL) return WriteScaled(hertz, 1000000000UL, " GHz");
        if (hertz >= 1000000UL) return WriteScaled(hertz, 1000000UL, " MHz");
        if (hertz >= 1000UL) return WriteScaled(hertz, 1000UL, " kHz");
        if (!WriteUInt64(hertz)) return false;
        return Write(" Hz");
    }

    /// <summary>Writes a duration expressed in nanoseconds using ns, us, ms, or s as appropriate.</summary>
    public static Boolean WriteDurationNanoseconds(UInt64 nanoseconds)
    {
        if (nanoseconds >= 1000000000UL) return WriteScaled(nanoseconds, 1000000000UL, " s");
        if (nanoseconds >= 1000000UL) return WriteScaled(nanoseconds, 1000000UL, " ms");
        if (nanoseconds >= 1000UL) return WriteScaled(nanoseconds, 1000UL, " us");
        if (!WriteUInt64(nanoseconds)) return false;
        return Write(" ns");
    }

    private static Boolean WriteScaled(UInt64 value, UInt64 divisor, String suffix)
    {
        UInt64 whole = value / divisor;
        UInt64 remainder = value % divisor;
        UInt64 hundredths = (remainder * 100UL) / divisor;
        if (!WriteUInt64(whole)) return false;
        if (hundredths != 0UL)
        {
            if (!Write(".")) return false;
            if (hundredths < 10UL && !Write("0")) return false;
            if (!WriteUInt64(hundredths)) return false;
        }
        return Write(suffix);
    }

    /// <summary>Writes one unsigned 64-bit value as a fixed-width hexadecimal number prefixed with 0x.</summary>
    /// <returns><see langword="true"/> when every hexadecimal character was written.</returns>
    public static Boolean WriteHex(UInt64 value)
    {
        if (!WriteRaw((Byte)'0') || !WriteRaw((Byte)'x')) return false;
        Int32 shift = 60;
        while (shift >= 0)
        {
            UInt32 nibble = (UInt32)((value >> shift) & 0xFUL);
            Byte character = nibble < 10U ? (Byte)('0' + nibble) : (Byte)('A' + (nibble - 10U));
            if (!WriteRaw(character)) return false;
            shift -= 4;
        }
        return _framebuffer.Flush();
    }


    /// <summary>Gets the byte count required by one complete framebuffer image.</summary>
    public static UInt64 GetFramebufferBufferByteCount()
    {
        return _initialized ? _framebuffer.FrameByteCount : 0UL;
    }

    /// <summary>Gets the active framebuffer buffering mode and available buffer count.</summary>
    public static FramebufferBufferCapabilities GetFramebufferBufferCapabilities()
    {
        FramebufferBufferMode mode = _framebuffer.BufferCount == 3U ? FramebufferBufferMode.Triple : (_framebuffer.BufferCount == 2U ? FramebufferBufferMode.Double : FramebufferBufferMode.Single);
        return new FramebufferBufferCapabilities(mode, _framebuffer.AvailableBufferCount, _framebuffer.FrameByteCount);
    }

    /// <summary>Gets the requested framebuffer buffering setting: 0 = automatic, 1 = single, 2 = double, 3 = triple.</summary>
    public static UInt32 GetFramebufferBufferSetting()
    {
        if (!_initialized) return 0U;
        return _framebuffer.AutomaticBuffering ? 0U : _framebuffer.BufferCount;
    }

    /// <summary>Attaches two heap-backed framebuffer images and automatically selects the best text-console buffering mode (double buffering).</summary>
    public static Boolean ConfigureFramebufferBuffers(UInt64 backBufferA, UInt64 backBufferB, UInt64 bufferByteCount)
    {
        if (!_initialized) return false;
        return _framebuffer.ConfigureBuffers(backBufferA, backBufferB, bufferByteCount);
    }

    /// <summary>Selects automatic (0), single (1), double (2), or triple (3) framebuffer buffering. Automatic currently selects double buffering for the text console.</summary>
    public static Boolean SetFramebufferBufferCount(UInt32 bufferCount)
    {
        if (!_initialized) return false;
        return _framebuffer.SetBufferCount(bufferCount);
    }

    /// <summary>Clears the framebuffer console, retained scrollback, caret area, and redraws an empty live viewport.</summary>
    public static Boolean ClearScreen()
    {
        if (!_initialized) return false;
        return _framebuffer.Clear();
    }

    /// <summary>Scrolls the framebuffer console upward by one visual line of retained output.</summary>
    public static Boolean ScrollUp()
    {
        if (!_initialized) return false;
        return _framebuffer.ScrollUp();
    }

    /// <summary>Scrolls the framebuffer console downward by one visual line toward live output.</summary>
    public static Boolean ScrollDown()
    {
        if (!_initialized) return false;
        return _framebuffer.ScrollDown();
    }

    /// <summary>Gets the active framebuffer font preset: 1 = 8 px, 2 = 16 px, 3 = 24 px.</summary>
    public static UInt32 GetFontPreset()
    {
        UInt32 size = _framebuffer.FontSize;
        if (size == FramebufferConsole.SmallFontSize) return 1U;
        if (size == FramebufferConsole.MediumFontSize) return 2U;
        if (size == FramebufferConsole.LargeFontSize) return 3U;
        return 0U;
    }

    /// <summary>Selects framebuffer font preset 1 (8 px), 2 (16 px), or 3 (24 px) and redraws retained output.</summary>
    public static Boolean SetFontPreset(UInt32 preset)
    {
        if (!_initialized) return false;
        return _framebuffer.SetFontPreset(preset);
    }

    /// <summary>Gets the active framebuffer font face independently of its rendered size.</summary>
    public static ConsoleFontInformation GetFontInformation() => ConsoleFont.GetInformation();

    /// <summary>Installs a PSF2 font from kernel-accessible memory and redraws retained QEMU/GOP framebuffer output.</summary>
    public static Boolean InstallPsf2Font(UInt64 address, UInt64 length)
    {
        if (!_initialized || !ConsoleFont.InstallPsf2(address, length)) return false;
        if (_framebuffer.ReloadFontFace()) return true;
        ConsoleFont.UseEmbedded();
        _framebuffer.ReloadFontFace();
        return false;
    }

    /// <summary>Restores the guaranteed embedded NovaOryn console font and redraws retained framebuffer output.</summary>
    public static Boolean UseEmbeddedFont()
    {
        if (!_initialized || !ConsoleFont.UseEmbedded()) return false;
        return _framebuffer.ReloadFontFace();
    }

    /// <summary>Erases the last framebuffer-console character and mirrors a terminal backspace sequence to serial output.</summary>
    public static unsafe Boolean Backspace()
    {
        if (!_initialized) return false;
        if (!Native.WriteSerial((Byte)'\b') || !Native.WriteSerial((Byte)' ') || !Native.WriteSerial((Byte)'\b')) return false;
        if (_secondarySerialWriter != null)
        {
            if (!_secondarySerialWriter((Byte)'\b') || !_secondarySerialWriter((Byte)' ') || !_secondarySerialWriter((Byte)'\b')) return false;
        }
        return _framebuffer.Backspace();
    }


    /// <summary>Enables or disables the framebuffer command caret.</summary>
    public static Boolean SetCaretEnabled(Boolean enabled)
    {
        if (!_initialized) return false;
        return _framebuffer.SetCaretEnabled(enabled);
    }

    /// <summary>Advances the visual caret blink timer; intended for the timer-dispatch service.</summary>
    public static Boolean TickCaret()
    {
        if (!_initialized) return false;
        return _framebuffer.TickCaret();
    }

    /// <summary>Confirms that the console is ready for decoded input delivered by the active input driver.</summary>
    public static Boolean ServiceInput() => _initialized;

    private static unsafe delegate*<Boolean> _inputService;

    /// <summary>Installs the SDK-owned decoded-input service used by the interactive shell.</summary>
    public static unsafe Boolean SetInputService(delegate*<Boolean> service)
    {
        _inputService = service;
        return true;
    }

    /// <summary>Writes one IDE control record to the primary serial/debug channel without rendering it on the guest framebuffer.</summary>
    public static Boolean WriteHostControl(String command)
    {
        if (!_initialized || command == null) return false;
        const String prefix = "[[NOVAORYN:";
        const String suffix = "]]\r\n";
        for (Int32 i = 0; i < prefix.Length; i++) if (!Native.WriteSerial((Byte)prefix[i])) return false;
        for (Int32 i = 0; i < command.Length; i++) if (!Native.WriteSerial((Byte)command[i])) return false;
        for (Int32 i = 0; i < suffix.Length; i++) if (!Native.WriteSerial((Byte)suffix[i])) return false;
        return true;
    }

    /// <summary>Runs the post-boot interrupt-driven idle loop while servicing the SDK-owned input bridge before each halt.</summary>
    public static unsafe Boolean RunInteractive()
    {
        if (!_initialized) return false;
        while (true)
        {
            if (_inputService != null && !_inputService()) return false;
            if (!Native.WaitForInterrupt()) return false;
        }
    }

    /// <summary>Attaches an optional post-boot serial mirror while preserving COM1 as the primary debug transport.</summary>
    public static unsafe Boolean SetSecondarySerialWriter(delegate*<Byte, Boolean> writer)
    {
        _secondarySerialWriter = writer;
        return true;
    }

    /// <summary>Writes one character to every configured console target and presents the completed glyph/line region.</summary>
    public static unsafe Boolean Write(Byte value)
    {
        if (!WriteRaw(value)) return false;
        return _framebuffer.Flush();
    }

    private static unsafe Boolean WriteRaw(Byte value)
    {
        if (!_initialized) return false;
        if (!Native.WriteSerial(value)) return false;
        if (_secondarySerialWriter != null && !_secondarySerialWriter(value)) return false;
        return _framebuffer.Write(value);
    }
}
