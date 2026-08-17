using System;
namespace NovaOryn.Userland.Commands;
/// <summary>Standard userland command that requests the active terminal to clear.</summary>
public static class ClearCommand
{
    public static String GetUsage()=>"clear | cls";
    public static Int32 Run(Func<Boolean> clear){if(clear==null)return 2;return clear()?0:1;}
}
