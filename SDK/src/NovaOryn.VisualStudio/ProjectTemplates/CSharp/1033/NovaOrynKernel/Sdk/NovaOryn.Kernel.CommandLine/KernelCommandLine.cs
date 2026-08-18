using System;
using NovaOryn.Kernel.Console;
using NovaOryn.Kernel.Ps2;
using NovaOryn.Kernel.Heap;
using NovaOryn.Kernel.Memory;
using NovaOryn.Kernel.Drivers;
using NovaOryn.Kernel.Time;
using NovaOryn.Kernel.TimerDispatch;

namespace NovaOryn.Kernel.CommandLine;

/// <summary>Provides the interactive NovaOryn framebuffer-console command line.</summary>
public static unsafe class KernelCommandLine
{
    public const UInt32 MaximumCommandBytes = 256U;
    private static KernelHeapAllocation _inputAllocation;
    private static Byte* _input;
    private static UInt32 _length;
    private static UInt32 _inputTimerHandle;
    private static Boolean _initialized;
    private static Boolean _copyAllArmed;

    /// <summary>Initializes the command line and writes the initial prompt.</summary>
    public static Boolean Initialize()
    {
        if (_initialized) return true;
        if (!KernelHeap.IsInitialized() || !KernelHeap.TryAllocate(MaximumCommandBytes, 16UL, true, out _inputAllocation)) return false;
        _input=(Byte*)(nuint)_inputAllocation.Address;
        _length=0U; _copyAllArmed=false; _initialized=true;
        // Own the PS/2 -> shell bridge in the SDK, not in generated user HAL source.
        // Existing projects therefore receive working input as soon as their SDK bridge is refreshed.
        if (KernelPs2.IsInitialized())
        {
            if (!KernelPs2.SetKeyboardEventHandler(&HandlePs2KeyboardEvent)) return false;
            if (!KernelTimerDispatch.Register(1000000UL, &ServiceInputTimer, 0UL, out _inputTimerHandle)) return false;
        }
        if (!KernelConsole.SetInputService(&ServiceInputNow)) return false;
        if (!KernelConsole.WriteHostControl("SHELL_BEGIN")) return false;
        if(!KernelConsole.WriteLine("Commands: help, clear, echo, info, uptime, memory, drivers, devices, font, buffering, keyboard")) return false;
        if(!KernelConsole.WriteLine("Shell input bridge: SDK-owned PS/2 service active. Ctrl+A then Ctrl+C copies all shell output.")) return false;
        if(!KernelConsole.SetCaretEnabled(true)) return false;
        return WritePrompt();
    }

    /// <summary>Consumes one decoded keyboard character, including Backspace and Enter.</summary>
    public static Boolean HandleCharacter(Char character)
    {
        if(!_initialized) return false;
        if(character=='\r' || character=='\n') return Submit();
        if(character=='\b') return Backspace();
        if(character<' ' || character>'~') return true;
        if(_length>=MaximumCommandBytes-1U) return true;
        _input[_length++]=(Byte)character;
        return KernelConsole.Write((Byte)character);
    }

    /// <summary>Gets the number of bytes currently entered at the prompt.</summary>
    public static UInt32 InputLength => _length;

    private static Boolean ServiceInputTimer(UInt64 cookie) => ServiceInputNow();

    /// <summary>Services the SDK-owned keyboard bridge immediately; also called directly from the interactive idle loop.</summary>
    public static Boolean ServiceInputNow()
    {
        if (!_initialized) return false;
        if (!KernelPs2.IsInitialized()) return true;
        return KernelPs2.Service();
    }

    private static Boolean HandlePs2KeyboardEvent(Ps2KeyboardEvent input)
    {
        if (!input.Pressed) return true;
        if (input.Control && input.Key == Ps2Key.A)
        {
            _copyAllArmed = true;
            return KernelConsole.WriteHostControl("SHELL_SELECT_ALL");
        }
        if (input.Control && input.Key == Ps2Key.C)
        {
            if (_copyAllArmed)
            {
                _copyAllArmed = false;
                return KernelConsole.WriteHostControl("SHELL_COPY_ALL");
            }
            return CancelCurrentCommand();
        }
        _copyAllArmed = false;
        if (input.Key == Ps2Key.Up) return KernelConsole.ScrollUp();
        if (input.Key == Ps2Key.Down) return KernelConsole.ScrollDown();
        if (input.Control && input.Key == Ps2Key.D1) return KernelConsole.SetFramebufferBufferCount(1U);
        if (input.Control && input.Key == Ps2Key.D2) return KernelConsole.SetFramebufferBufferCount(2U);
        if (input.Control && input.Key == Ps2Key.D3) return KernelConsole.SetFramebufferBufferCount(3U);
        if (input.Alt && input.Key == Ps2Key.D1) return KernelConsole.SetFontPreset(1U);
        if (input.Alt && input.Key == Ps2Key.D2) return KernelConsole.SetFontPreset(2U);
        if (input.Alt && input.Key == Ps2Key.D3) return KernelConsole.SetFontPreset(3U);
        return HandleCharacter(input.Character);
    }

    private static Boolean CancelCurrentCommand()
    {
        _length = 0U;
        if (!KernelConsole.WriteLine("^C")) return false;
        return WritePrompt();
    }

    private static Boolean Backspace()
    {
        if(_length==0U) return true;
        _length--;
        // Erase the visible cell using the traditional BS-space-BS terminal sequence.
        return KernelConsole.Backspace();
    }

    private static Boolean Submit()
    {
        if(!KernelConsole.WriteLine("")) return false;
        Boolean ok=true;
        Trim(_input, ref _length);
        if(_length!=0U) ok=Execute(_input,_length);
        _length=0U;
        if(!ok && !KernelConsole.WriteLine("Command failed.")) return false;
        return WritePrompt();
    }

    private static Boolean Execute(Byte* command, UInt32 length)
    {
        UInt32 split=0U; while(split<length && command[split]!=' ') split++;
        UInt32 arg=split; while(arg<length && command[arg]==' ') arg++;
        if(TokenEquals(command,split,"help")) return Help();
        if(TokenEquals(command,split,"font")) return Font(command+arg,length-arg);
        if(TokenEquals(command,split,"buffering")) return Buffering(command+arg,length-arg);
        if(TokenEquals(command,split,"keyboard")) return Keyboard(command+arg,length-arg);
        if(TokenEquals(command,split,"echo")) return WriteAscii(command+arg,length-arg,true);
        if(TokenEquals(command,split,"clear") || TokenEquals(command,split,"cls")) return Clear();
        if(TokenEquals(command,split,"info") || TokenEquals(command,split,"system")) return Info();
        if(TokenEquals(command,split,"uptime")) return Uptime();
        if(TokenEquals(command,split,"memory")) return Memory();
        if(TokenEquals(command,split,"drivers")) return Drivers();
        if(TokenEquals(command,split,"devices")) return Devices();
        if(!KernelConsole.Write("Unknown command: ")) return false;
        if(!WriteAscii(command,split,false)) return false;
        return KernelConsole.WriteLine("");
    }

    private static Boolean Help()
    {
        return KernelConsole.WriteLine("help") &&
               KernelConsole.WriteLine("font get | font set 1|2|3 | font list") &&
               KernelConsole.WriteLine("buffering get | buffering set auto|1|2|3 | buffering list") &&
               KernelConsole.WriteLine("keyboard get | keyboard set English_UK|English_USA | keyboard list") &&
               KernelConsole.WriteLine("clear | cls") &&
               KernelConsole.WriteLine("echo <text>") &&
               KernelConsole.WriteLine("info | system") &&
               KernelConsole.WriteLine("uptime") &&
               KernelConsole.WriteLine("memory") &&
               KernelConsole.WriteLine("drivers") &&
               KernelConsole.WriteLine("devices");
    }


    private static Boolean Clear()
    {
        if(!KernelConsole.ClearScreen()) return false;
        return true;
    }

    private static Boolean Info()
    {
        KernelDriverCapabilities drivers=KernelDrivers.GetCapabilities();
        KernelPhysicalMemoryStatistics memory=KernelPhysicalMemory.GetStatistics();
        if(!KernelConsole.WriteLine("NovaOryn interactive userland command host")) return false;
        if(!KernelConsole.Write("drivers/devices: ")) return false;
        if(!KernelConsole.WriteUInt64(drivers.RegisteredDrivers)) return false;
        if(!KernelConsole.Write(" / ")) return false;
        if(!KernelConsole.WriteUInt64(drivers.RegisteredDevices)) return false;
        if(!KernelConsole.WriteLine("")) return false;
        if(!KernelConsole.Write("managed memory: ")) return false;
        return KernelConsole.WriteByteSize(memory.ManagedPages*4096UL) && KernelConsole.WriteLine("");
    }

    private static Boolean Uptime()
    {
        if(!KernelConsole.Write("uptime = ")) return false;
        if(!KernelConsole.WriteDurationNanoseconds(KernelTime.GetMonotonicNanoseconds())) return false;
        return KernelConsole.WriteLine("");
    }

    private static Boolean Memory()
    {
        KernelPhysicalMemoryStatistics stats=KernelPhysicalMemory.GetStatistics();
        if(!KernelConsole.Write("managed/free/allocated/reserved = ")) return false;
        if(!KernelConsole.WriteByteSize(stats.ManagedPages*4096UL) || !KernelConsole.Write(" / ")) return false;
        if(!KernelConsole.WriteByteSize(stats.FreePages*4096UL) || !KernelConsole.Write(" / ")) return false;
        if(!KernelConsole.WriteByteSize(stats.AllocatedPages*4096UL) || !KernelConsole.Write(" / ")) return false;
        if(!KernelConsole.WriteByteSize(stats.ReservedPages*4096UL)) return false;
        return KernelConsole.WriteLine("");
    }

    private static Boolean Drivers()
    {
        KernelDriverCapabilities c=KernelDrivers.GetCapabilities();
        if(!KernelConsole.Write("drivers registered/active = ")) return false;
        if(!KernelConsole.WriteUInt64(c.RegisteredDrivers) || !KernelConsole.Write(" / ")) return false;
        if(!KernelConsole.WriteUInt64(c.StartedDevices)) return false;
        return KernelConsole.WriteLine("");
    }

    private static Boolean Devices()
    {
        KernelDriverCapabilities c=KernelDrivers.GetCapabilities();
        if(!KernelConsole.Write("devices registered/bound/started = ")) return false;
        if(!KernelConsole.WriteUInt64(c.RegisteredDevices) || !KernelConsole.Write(" / ")) return false;
        if(!KernelConsole.WriteUInt64(c.BoundDevices) || !KernelConsole.Write(" / ")) return false;
        if(!KernelConsole.WriteUInt64(c.StartedDevices)) return false;
        return KernelConsole.WriteLine("");
    }

    private static Boolean Font(Byte* arg, UInt32 n)
    {
        if(TokenEquals(arg,n,"get")) { if(!KernelConsole.Write("font = "))return false; if(!KernelConsole.WriteUInt64(KernelConsole.GetFontPreset()))return false; return KernelConsole.WriteLine(""); }
        if(TokenEquals(arg,n,"list")) return KernelConsole.WriteLine("1 = 8 px\n2 = 16 px\n3 = 24 px (default)");
        if(StartsWith(arg,n,"set ")) { Byte v=arg[4]; if(n==5U && v>='1'&&v<='3' && KernelConsole.SetFontPreset((UInt32)(v-'0'))) return KernelConsole.WriteLine("OK"); }
        return KernelConsole.WriteLine("Usage: font get | font set 1 | font set 2 | font set 3 | font list");
    }

    private static Boolean Buffering(Byte* arg, UInt32 n)
    {
        if(TokenEquals(arg,n,"get")) { UInt32 v=KernelConsole.GetFramebufferBufferSetting(); if(!KernelConsole.Write("buffering = "))return false; if(v==0U)return KernelConsole.WriteLine("auto"); if(!KernelConsole.WriteUInt64(v))return false; return KernelConsole.WriteLine(""); }
        if(TokenEquals(arg,n,"list")) return KernelConsole.WriteLine("auto = automatic (double for text; default)\n1 = single\n2 = double\n3 = triple");
        if(StartsWith(arg,n,"set ")) { UInt32 v=99U; if(TokenEquals(arg+4,n-4,"auto")||TokenEquals(arg+4,n-4,"0"))v=0U; else if(n==5U&&arg[4]>='1'&&arg[4]<='3')v=(UInt32)(arg[4]-'0'); if(v<=3U&&KernelConsole.SetFramebufferBufferCount(v))return KernelConsole.WriteLine("OK"); }
        return KernelConsole.WriteLine("Usage: buffering get | buffering set auto | buffering set 1 | buffering set 2 | buffering set 3 | buffering list");
    }

    private static Boolean Keyboard(Byte* arg, UInt32 n)
    {
        if(TokenEquals(arg,n,"get")) { if(!KernelConsole.Write("keyboard = "))return false; return KernelConsole.WriteLine(KeyboardLayouts.GetName(KernelPs2.GetKeyboardLayout())); }
        if(TokenEquals(arg,n,"list")) return KernelConsole.WriteLine("English_UK\nEnglish_USA");
        if(StartsWith(arg,n,"set ")) { KeyboardLayout layout; if(TokenEquals(arg+4,n-4,"English_UK"))layout=KeyboardLayout.English_UK; else if(TokenEquals(arg+4,n-4,"English_USA"))layout=KeyboardLayout.English_USA; else return KernelConsole.WriteLine("Usage: keyboard set English_UK | keyboard set English_USA"); if(KernelPs2.SetKeyboardLayout(layout))return KernelConsole.WriteLine("OK"); return false; }
        return KernelConsole.WriteLine("Usage: keyboard get | keyboard set English_UK | keyboard set English_USA | keyboard list");
    }

    private static Boolean WritePrompt()=>KernelConsole.Write("NovaOryn> ");
    private static Boolean WriteAscii(Byte* p,UInt32 n,Boolean newline){for(UInt32 i=0;i<n;i++)if(!KernelConsole.Write(p[i]))return false;return !newline||KernelConsole.WriteLine("");}
    private static void Trim(Byte* p,ref UInt32 n){UInt32 s=0U;while(s<n&&p[s]==' ')s++;if(s!=0U){for(UInt32 i=s;i<n;i++)p[i-s]=p[i];n-=s;}while(n!=0U&&p[n-1U]==' ')n--;}
    private static Boolean StartsWith(Byte* p,UInt32 n,String literal){if(literal==null||(UInt32)literal.Length>n)return false;for(Int32 i=0;i<literal.Length;i++)if(ToLower(p[(UInt32)i])!=ToLower((Byte)literal[i]))return false;return true;}
    private static Boolean TokenEquals(Byte* p,UInt32 n,String literal){if(literal==null||n!=(UInt32)literal.Length)return false;for(UInt32 i=0;i<n;i++)if(ToLower(p[i])!=ToLower((Byte)literal[(Int32)i]))return false;return true;}
    private static Byte ToLower(Byte c)=>c>='A'&&c<='Z'?(Byte)(c+32):c;
}
