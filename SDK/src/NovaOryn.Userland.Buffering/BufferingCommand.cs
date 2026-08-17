using System;
namespace NovaOryn.Userland.Buffering;
/// <summary>Compatibility facade for the canonical Userland/Commands buffering command.</summary>
public static class BufferingCommand
{
 public const UInt32 BufferingPresetService=NovaOryn.Userland.Commands.BufferingCommand.BufferingPresetService;
 public static String GetUsage()=>NovaOryn.Userland.Commands.BufferingCommand.GetUsage();
 public static String GetInstalledPresets()=>NovaOryn.Userland.Commands.BufferingCommand.GetInstalledPresets();
 public static Int32 Run(String[] args,Func<UInt32,Int64> get,Func<UInt32,UInt64,Int64> set,Action<String> write)=>NovaOryn.Userland.Commands.BufferingCommand.Run(args,get,set,write);
 public static Boolean TryParsePreset(String value,out UInt32 preset)=>NovaOryn.Userland.Commands.BufferingCommand.TryParsePreset(value,out preset);
}
