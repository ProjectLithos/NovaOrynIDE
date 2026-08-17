using System;
using NovaOryn.Kernel.Ps2;
using NovaOryn.Userland.Keyboard;

internal static class Program
{
    private static Int32 Main()
    {
        if(KeyboardLayouts.Translate(KeyboardLayout.English_USA,Ps2Key.D2,true,false,false)!='@') return 1;
        if(KeyboardLayouts.Translate(KeyboardLayout.English_UK,Ps2Key.D2,true,false,false)!='"') return 2;
        if(KeyboardLayouts.Translate(KeyboardLayout.English_UK,Ps2Key.D3,true,false,false)!='£') return 3;
        if(KeyboardLayouts.Translate(KeyboardLayout.English_UK,Ps2Key.Apostrophe,true,false,false)!='@') return 4;
        if(KeyboardLayouts.Translate(KeyboardLayout.English_USA,Ps2Key.Apostrophe,true,false,false)!='"') return 5;
        if(KeyboardLayouts.Translate(KeyboardLayout.English_UK,Ps2Key.D4,false,false,true)!='€') return 6;
        if(!KeyboardCommand.TryParseLayout("English_UK",out UInt32 uk)||uk!=1U) return 7;
        if(!KeyboardCommand.TryParseLayout("English_USA",out UInt32 us)||us!=2U) return 8;
        Console.WriteLine("NovaOryn PS/2 + UK/US keyboard layout tests passed."); return 0;
    }
}
