using System;
namespace NovaOryn.Userland.Commands;
/// <summary>Standard userland command that prints the installed command catalog.</summary>
public static class HelpCommand
{
    public static String GetUsage()=>"help";
    public static Int32 Run(Action<String> write){if(write==null)return 2;write(GeneralCommands.GetHelp());return 0;}
}
