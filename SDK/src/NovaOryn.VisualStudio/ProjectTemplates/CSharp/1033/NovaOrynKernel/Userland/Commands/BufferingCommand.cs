using System;

namespace NovaOryn.Userland.Commands;

/// <summary>Userland framebuffer-buffering command grammar and NovaOryn syscall service contract.</summary>
public static class BufferingCommand
{
    /// <summary>NovaOryn Get/Set service reserved for the framebuffer buffering preset.</summary>
    public const UInt32 BufferingPresetService = 34U;
    /// <summary>Returns command usage.</summary>
    public static String GetUsage() => "buffering get | buffering set auto | buffering set 1 | buffering set 2 | buffering set 3 | buffering list";
    /// <summary>Returns available buffering presets.</summary>
    public static String GetInstalledPresets() => "auto = automatic (double buffered for the text console; default)\n1 = single buffered\n2 = double buffered\n3 = triple buffered";
    /// <summary>Executes the buffering command through supplied NovaOryn Get/Set syscall invokers.</summary>
    public static Int32 Run(String[] args, Func<UInt32, Int64> get, Func<UInt32, UInt64, Int64> set, Action<String> write)
    {
        if (args == null || get == null || set == null || write == null) return 2;
        if (args.Length == 1 && String.Equals(args[0], "get", StringComparison.OrdinalIgnoreCase))
        {
            Int64 value = get(BufferingPresetService); if (value < 0L || value > 3L) return 1; write(value == 0L ? "auto" : value.ToString()); return 0;
        }
        if (args.Length == 1 && String.Equals(args[0], "list", StringComparison.OrdinalIgnoreCase)) { write(GetInstalledPresets()); return 0; }
        if (args.Length == 2 && String.Equals(args[0], "set", StringComparison.OrdinalIgnoreCase) && TryParsePreset(args[1], out UInt32 preset))
        {
            if (set(BufferingPresetService, preset) != 0L) return 1; write(preset == 0U ? "auto" : preset.ToString()); return 0;
        }
        write(GetUsage()); return 2;
    }
    /// <summary>Parses automatic buffering or explicit presets 1 through 3.</summary>
    public static Boolean TryParsePreset(String value, out UInt32 preset)
    {
        if (String.Equals(value, "auto", StringComparison.OrdinalIgnoreCase) || String.Equals(value, "0", StringComparison.Ordinal)) { preset = 0U; return true; }
        if (String.Equals(value, "1", StringComparison.Ordinal)) { preset = 1U; return true; }
        if (String.Equals(value, "2", StringComparison.Ordinal)) { preset = 2U; return true; }
        if (String.Equals(value, "3", StringComparison.Ordinal)) { preset = 3U; return true; }
        preset = 0U; return false;
    }
}
