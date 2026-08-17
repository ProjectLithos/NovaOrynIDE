using System;

namespace NovaOryn.Userland.Commands;

/// <summary>Userland keyboard-layout command grammar and syscall service contract.</summary>
public static class KeyboardCommand
{
    /// <summary>NovaOryn Get/Set/Event service number reserved for keyboard layout.</summary>
    public const UInt32 KeyboardLayoutService = 32U;
    /// <summary>Parses an installed layout name exactly as accepted by the keyboard command.</summary>
    public static Boolean TryParseLayout(String value,out UInt32 layout)
    {
        if(String.Equals(value,"English_UK",StringComparison.OrdinalIgnoreCase)){layout=1U;return true;}
        if(String.Equals(value,"English_USA",StringComparison.OrdinalIgnoreCase)){layout=2U;return true;}
        layout=0U;return false;
    }
    /// <summary>Returns the canonical installed-layout name for a kernel layout identifier.</summary>
    public static String GetLayoutName(UInt32 layout)=>layout==1U?"English_UK":layout==2U?"English_USA":"Unknown";
    /// <summary>Gets command usage for a shell or executable loader.</summary>
    public static String GetUsage()=>"keyboard get | keyboard set English_UK | keyboard set English_USA | keyboard list";
    /// <summary>Gets the installed layout list printed by `keyboard list`.</summary>
    public static String GetInstalledLayouts()=>"English_UK\nEnglish_USA";
    /// <summary>Executes the keyboard command through supplied NovaOryn Get/Set syscall invokers.</summary>
    public static Int32 Run(String[] args, Func<UInt32,Int64> get, Func<UInt32,UInt64,Int64> set, Action<String> write)
    {
        if(args==null||get==null||set==null||write==null){return 2;}
        if(args.Length==1&&String.Equals(args[0],"get",StringComparison.OrdinalIgnoreCase)){Int64 value=get(KeyboardLayoutService);if(value<0)return 1;write(GetLayoutName((UInt32)value));return 0;}
        if(args.Length==1&&String.Equals(args[0],"list",StringComparison.OrdinalIgnoreCase)){write(GetInstalledLayouts());return 0;}
        if(args.Length==2&&String.Equals(args[0],"set",StringComparison.OrdinalIgnoreCase)&&TryParseLayout(args[1],out UInt32 layout)){Int64 result=set(KeyboardLayoutService,layout);if(result!=0)return 1;write(GetLayoutName(layout));return 0;}
        write(GetUsage());return 2;
    }

}
