using System;

namespace NovaOryn.Userland.Commands;

/// <summary>Userland framebuffer-font command grammar and NovaOryn syscall service contract.</summary>
public static class FontCommand
{
    /// <summary>NovaOryn Get/Set service reserved for the console font preset.</summary>
    public const UInt32 FontPresetService = 33U;
    /// <summary>Returns command usage.</summary>
    public static String GetUsage() => "font get | font set 1 | font set 2 | font set 3 | font list";
    /// <summary>Returns the installed font presets.</summary>
    public static String GetInstalledPresets() => "1 = 8 px\n2 = 16 px\n3 = 24 px (default)";
    /// <summary>Executes the font command through supplied NovaOryn Get/Set syscall invokers.</summary>
    public static Int32 Run(String[] args, Func<UInt32, Int64> get, Func<UInt32, UInt64, Int64> set, Action<String> write)
    {
        if (args == null || get == null || set == null || write == null) return 2;
        if (args.Length == 1 && String.Equals(args[0], "get", StringComparison.OrdinalIgnoreCase))
        {
            Int64 value = get(FontPresetService); if (value < 1L || value > 3L) return 1; write(value.ToString()); return 0;
        }
        if (args.Length == 1 && String.Equals(args[0], "list", StringComparison.OrdinalIgnoreCase)) { write(GetInstalledPresets()); return 0; }
        if (args.Length == 2 && String.Equals(args[0], "set", StringComparison.OrdinalIgnoreCase) && TryParsePreset(args[1], out UInt32 preset))
        {
            if (set(FontPresetService, preset) != 0L) return 1; write(preset.ToString()); return 0;
        }
        write(GetUsage()); return 2;
    }
    /// <summary>Parses font presets 1 through 3.</summary>
    public static Boolean TryParsePreset(String value, out UInt32 preset)
    {
        if (String.Equals(value, "1", StringComparison.Ordinal)) { preset = 1U; return true; }
        if (String.Equals(value, "2", StringComparison.Ordinal)) { preset = 2U; return true; }
        if (String.Equals(value, "3", StringComparison.Ordinal)) { preset = 3U; return true; }
        preset = 0U; return false;
    }
}
