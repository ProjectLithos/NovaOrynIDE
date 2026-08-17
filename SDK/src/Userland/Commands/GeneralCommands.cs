using System;

namespace NovaOryn.Userland.Commands;

/// <summary>Defines the non-hardware userland command names and usage shipped by NovaOryn.</summary>
public static class GeneralCommands
{
    /// <summary>Returns help for the standard command set.</summary>
    public static String GetHelp() =>
        "help\nclear | cls\necho <text>\ninfo | system\nuptime\nmemory\ndrivers\ndevices\nfont ...\nbuffering ...\nkeyboard ...";

    /// <summary>Returns the canonical clear-screen aliases.</summary>
    public static String GetClearAliases() => "clear\ncls";
    /// <summary>Returns usage for echo.</summary>
    public static String GetEchoUsage() => "echo <text>";
    /// <summary>Returns usage for system information.</summary>
    public static String GetInfoUsage() => "info | system";
    /// <summary>Returns usage for uptime.</summary>
    public static String GetUptimeUsage() => "uptime";
    /// <summary>Returns usage for physical-memory information.</summary>
    public static String GetMemoryUsage() => "memory";
    /// <summary>Returns usage for driver information.</summary>
    public static String GetDriversUsage() => "drivers";
    /// <summary>Returns usage for device information.</summary>
    public static String GetDevicesUsage() => "devices";
}
