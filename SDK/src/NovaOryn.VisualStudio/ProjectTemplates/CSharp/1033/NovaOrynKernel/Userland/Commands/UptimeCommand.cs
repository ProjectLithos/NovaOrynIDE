using System;
namespace NovaOryn.Userland.Commands;
/// <summary>Standard userland command that requests monotonic system uptime.</summary>
public static class UptimeCommand
{
    public static String GetUsage()=>"uptime";
    public static Int32 Run(Func<String> query,Action<String> write){if(query==null||write==null)return 2;String value=query();if(value==null)return 1;write(value);return 0;}
}
