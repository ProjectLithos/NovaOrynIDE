using System;
namespace NovaOryn.Userland.Commands;
/// <summary>Standard userland command that writes its supplied text.</summary>
public static class EchoCommand
{
    public static String GetUsage()=>"echo <text>";
    public static Int32 Run(String text,Action<String> write){if(text==null||write==null)return 2;write(text);return 0;}
}
