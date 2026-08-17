using System;
namespace NovaOryn.Userland.Keyboard;
/// <summary>Compatibility facade for the canonical Userland/Commands keyboard command.</summary>
public static class KeyboardCommand
{
 public const UInt32 KeyboardLayoutService=NovaOryn.Userland.Commands.KeyboardCommand.KeyboardLayoutService;
 public static Boolean TryParseLayout(String value,out UInt32 layout)=>NovaOryn.Userland.Commands.KeyboardCommand.TryParseLayout(value,out layout);
 public static String GetLayoutName(UInt32 layout)=>NovaOryn.Userland.Commands.KeyboardCommand.GetLayoutName(layout);
 public static String GetUsage()=>NovaOryn.Userland.Commands.KeyboardCommand.GetUsage();
 public static String GetInstalledLayouts()=>NovaOryn.Userland.Commands.KeyboardCommand.GetInstalledLayouts();
 public static Int32 Run(String[] args,Func<UInt32,Int64> get,Func<UInt32,UInt64,Int64> set,Action<String> write)=>NovaOryn.Userland.Commands.KeyboardCommand.Run(args,get,set,write);
}
