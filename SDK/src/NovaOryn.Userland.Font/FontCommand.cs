using System;
namespace NovaOryn.Userland.Font;
/// <summary>Compatibility facade for the canonical Userland/Commands font command.</summary>
public static class FontCommand
{
 public const UInt32 FontPresetService=NovaOryn.Userland.Commands.FontCommand.FontPresetService;
 public static String GetUsage()=>NovaOryn.Userland.Commands.FontCommand.GetUsage();
 public static String GetInstalledPresets()=>NovaOryn.Userland.Commands.FontCommand.GetInstalledPresets();
 public static Int32 Run(String[] args,Func<UInt32,Int64> get,Func<UInt32,UInt64,Int64> set,Action<String> write)=>NovaOryn.Userland.Commands.FontCommand.Run(args,get,set,write);
 public static Boolean TryParsePreset(String value,out UInt32 preset)=>NovaOryn.Userland.Commands.FontCommand.TryParsePreset(value,out preset);
}
