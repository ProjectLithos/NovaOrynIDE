using System;
using NovaOryn.Kernel.Internal.X64;
using NovaOryn.Kernel.SystemCalls;

namespace NovaOryn.Kernel.Ps2;

/// <summary>Owns the legacy i8042 controller, PS/2 keyboard, mouse, and keyboard layout state.</summary>
public static unsafe class KernelPs2
{
    /// <summary>Gets the decoded keyboard-input contract implemented by this PS/2 driver.</summary>
    public const UInt32 InputContractVersion = 3U;

    private const UInt16 DataPort=0x60, StatusPort=0x64, CommandPort=0x64;
    private const UInt32 KeyboardLayoutService=32U;
    private static Boolean _controller,_keyboard,_mouse,_extended,_shiftL,_shiftR,_controlL,_controlR,_altL,_altR,_caps;
    private static KeyboardLayout _layout=KeyboardLayout.English_UK;
    private static UInt64 _keyboardEvents,_mousePackets,_pressedLow,_pressedHigh;
    private static Byte _mouseIndex; private static Byte _m0,_m1,_m2;
    private static Ps2KeyboardEvent _lastKeyboard; private static Ps2MouseState _mouseState;
    private static delegate*<Ps2KeyboardEvent, Boolean> _keyboardEventHandler;

    /// <summary>Initializes the i8042 controller and both PS/2 ports when present.</summary>
    public static Boolean Initialize()
    {
        // NativeAOT is invoked with --nopreinitstatics, so establish every runtime default explicitly.
        _layout=KeyboardLayout.English_UK;
        _extended=false;_shiftL=false;_shiftR=false;_controlL=false;_controlR=false;_altL=false;_altR=false;_caps=false;
        _pressedLow=0UL;_pressedHigh=0UL;
        Drain();
        if(!WriteCommand(0xAD)||!WriteCommand(0xA7)) return false;
        if(!WriteCommand(0x20)||!ReadData(out Byte config)) return false;
        config=(Byte)(config & ~0x03); // polling mode: keep keyboard/mouse IRQs disabled during controller tests
        if(!WriteCommand(0x60)||!WriteData(config)) return false;
        if(!WriteCommand(0xAA)||!ReadData(out Byte self)||self!=0x55) return false;
        _controller=true;
        if(WriteCommand(0xAB)&&ReadData(out Byte ktest)&&ktest==0x00)
        {
            if(WriteCommand(0xAE)&&SendKeyboard(0xF6)&&SendKeyboard(0xF4)) _keyboard=true;
        }
        if(WriteCommand(0xA9)&&ReadData(out Byte mtest)&&mtest==0x00)
        {
            if(WriteCommand(0xA8)&&SendMouse(0xF6)&&SendMouse(0xF4)) _mouse=true;
        }
        if(!WriteCommand(0x20)||!ReadData(out config)) return false;
        config=(Byte)((config | 0x40) & ~0x03); // translate keyboard Set-2 input to Set-1; service both ports by polling
        if(!WriteCommand(0x60)||!WriteData(config)) return false;
        return KernelSystemCalls.RegisterGet(KeyboardLayoutService,&GetLayoutSyscall) && KernelSystemCalls.RegisterSet(KeyboardLayoutService,&SetLayoutSyscall);
    }

    /// <summary>Services all currently buffered i8042 keyboard and mouse bytes without blocking.</summary>
    public static Boolean Service()
    {
        for(UInt32 n=0;n<64U;n++)
        {
            if(!Native.ReadPort8(StatusPort,out Byte status)) return false;
            if((status&0x01)==0) return true;
            if(!Native.ReadPort8(DataPort,out Byte value)) return false;
            if((status&0x20)!=0) DecodeMouse(value); else if(!DecodeKeyboard(value)) return false;
        }
        return true;
    }
    /// <summary>Enables or disables the i8042 keyboard/mouse hardware IRQ lines after the kernel interrupt routes are installed.</summary>
    public static Boolean SetHardwareInterrupts(Boolean enabled)
    {
        if(!_controller||!WriteCommand(0x20)||!ReadData(out Byte config)) return false;
        if(enabled) config=(Byte)(config|(_keyboard?0x01:0x00)|(_mouse?0x02:0x00)); else config=(Byte)(config&~0x03);
        if(!WriteCommand(0x60)||!WriteData(config)) return false;
        return true;
    }

    /// <summary>Gets current device and layout capabilities.</summary>
    public static Boolean IsInitialized()=>_initialized;
    public static Ps2Capabilities GetCapabilities()=>new(_controller,_keyboard,_mouse,_layout,_keyboardEvents,_mousePackets);
    /// <summary>Gets the active installed keyboard layout.</summary>
    public static KeyboardLayout GetKeyboardLayout()=>_layout;
    /// <summary>Sets the active installed keyboard layout immediately.</summary>
    public static Boolean SetKeyboardLayout(KeyboardLayout layout){if(!KeyboardLayouts.IsInstalled(layout))return false;_layout=layout;return true;}
    /// <summary>Gets the most recently decoded keyboard transition.</summary>
    public static Ps2KeyboardEvent GetLastKeyboardEvent()=>_lastKeyboard;
    /// <summary>Gets whether one logical PS/2 key is currently held down.</summary>
    public static Boolean IsKeyPressed(Ps2Key key)=>IsKeyDown(key);
    /// <summary>Gets the accumulated mouse position/button state.</summary>
    public static Ps2MouseState GetMouseState()=>_mouseState;
    /// <summary>Installs the decoded keyboard-event consumer. The PS/2 driver remains the sole owner of i8042 hardware reads.</summary>
    public static Boolean SetKeyboardEventHandler(delegate*<Ps2KeyboardEvent, Boolean> handler){_keyboardEventHandler=handler;return true;}

    private static Int64 GetLayoutSyscall(KernelSystemCallFrame* frame)=>unchecked((Int64)(UInt64)_layout);
    private static Int64 SetLayoutSyscall(KernelSystemCallFrame* frame)=>SetKeyboardLayout((KeyboardLayout)(UInt32)frame->Argument0)?0L:(Int64)KernelSystemCallError.InvalidArgument;
    private static Boolean DecodeKeyboard(Byte code)
    {
        if(code==0xE0){_extended=true;return true;}
        Boolean released=(code&0x80)!=0;
        Byte make=(Byte)(code&0x7F);
        Ps2Key key=MapKey(make,_extended);
        _extended=false;
        if(key==Ps2Key.None)return true;

        Boolean pressed=!released;
        Boolean wasPressed=IsKeyDown(key);

        // PS/2 typematic repeat produces duplicate make codes while a key remains down.
        // NovaOryn owns repeat policy in software, so only genuine state transitions
        // leave this driver. The break code can therefore cancel repeat immediately.
        if(pressed==wasPressed)return true;
        if(!SetKeyDown(key,pressed))return true;

        if(key==Ps2Key.LeftShift)_shiftL=pressed;
        else if(key==Ps2Key.RightShift)_shiftR=pressed;
        else if(key==Ps2Key.LeftControl)_controlL=pressed;
        else if(key==Ps2Key.RightControl)_controlR=pressed;
        else if(key==Ps2Key.LeftAlt)_altL=pressed;
        else if(key==Ps2Key.RightAlt)_altR=pressed;
        else if(key==Ps2Key.CapsLock&&pressed)_caps=!_caps;

        Char ch=pressed?KeyboardLayouts.Translate(_layout,key,_shiftL||_shiftR,_caps,_altR):'\0';
        _lastKeyboard=new Ps2KeyboardEvent(key,pressed,ch,_shiftL||_shiftR,_controlL||_controlR,_altL||_altR,_caps);
        _keyboardEvents++;
        return _keyboardEventHandler==null || _keyboardEventHandler(_lastKeyboard);
    }
    private static Boolean IsKeyDown(Ps2Key key)
    {
        UInt32 index=(UInt32)key;
        if(index==0U||index>=128U)return false;
        UInt64 mask=1UL<<(Int32)(index&63U);
        return index<64U?(_pressedLow&mask)!=0UL:(_pressedHigh&mask)!=0UL;
    }
    private static Boolean SetKeyDown(Ps2Key key,Boolean pressed)
    {
        UInt32 index=(UInt32)key;
        if(index==0U||index>=128U)return false;
        UInt64 mask=1UL<<(Int32)(index&63U);
        if(index<64U){if(pressed)_pressedLow|=mask;else _pressedLow&=~mask;}
        else {if(pressed)_pressedHigh|=mask;else _pressedHigh&=~mask;}
        return true;
    }
    private static Ps2Key MapKey(Byte c,Boolean e)
    {
        if(e){switch(c){case 0x1C:return Ps2Key.KeypadEnter;case 0x1D:return Ps2Key.RightControl;case 0x35:return Ps2Key.KeypadDivide;case 0x38:return Ps2Key.RightAlt;case 0x47:return Ps2Key.Home;case 0x48:return Ps2Key.Up;case 0x49:return Ps2Key.PageUp;case 0x4B:return Ps2Key.Left;case 0x4D:return Ps2Key.Right;case 0x4F:return Ps2Key.End;case 0x50:return Ps2Key.Down;case 0x51:return Ps2Key.PageDown;case 0x52:return Ps2Key.Insert;case 0x53:return Ps2Key.Delete;default:return Ps2Key.None;}}
        switch(c){case 0x01:return Ps2Key.Escape;case 0x02:return Ps2Key.D1;case 0x03:return Ps2Key.D2;case 0x04:return Ps2Key.D3;case 0x05:return Ps2Key.D4;case 0x06:return Ps2Key.D5;case 0x07:return Ps2Key.D6;case 0x08:return Ps2Key.D7;case 0x09:return Ps2Key.D8;case 0x0A:return Ps2Key.D9;case 0x0B:return Ps2Key.D0;case 0x0C:return Ps2Key.Minus;case 0x0D:return Ps2Key.Equals;case 0x0E:return Ps2Key.Backspace;case 0x0F:return Ps2Key.Tab;case 0x10:return Ps2Key.Q;case 0x11:return Ps2Key.W;case 0x12:return Ps2Key.E;case 0x13:return Ps2Key.R;case 0x14:return Ps2Key.T;case 0x15:return Ps2Key.Y;case 0x16:return Ps2Key.U;case 0x17:return Ps2Key.I;case 0x18:return Ps2Key.O;case 0x19:return Ps2Key.P;case 0x1A:return Ps2Key.LeftBracket;case 0x1B:return Ps2Key.RightBracket;case 0x1C:return Ps2Key.Enter;case 0x1D:return Ps2Key.LeftControl;case 0x1E:return Ps2Key.A;case 0x1F:return Ps2Key.S;case 0x20:return Ps2Key.D;case 0x21:return Ps2Key.F;case 0x22:return Ps2Key.G;case 0x23:return Ps2Key.H;case 0x24:return Ps2Key.J;case 0x25:return Ps2Key.K;case 0x26:return Ps2Key.L;case 0x27:return Ps2Key.Semicolon;case 0x28:return Ps2Key.Apostrophe;case 0x29:return Ps2Key.Grave;case 0x2A:return Ps2Key.LeftShift;case 0x2B:return Ps2Key.Backslash;case 0x2C:return Ps2Key.Z;case 0x2D:return Ps2Key.X;case 0x2E:return Ps2Key.C;case 0x2F:return Ps2Key.V;case 0x30:return Ps2Key.B;case 0x31:return Ps2Key.N;case 0x32:return Ps2Key.M;case 0x33:return Ps2Key.Comma;case 0x34:return Ps2Key.Period;case 0x35:return Ps2Key.Slash;case 0x36:return Ps2Key.RightShift;case 0x37:return Ps2Key.KeypadMultiply;case 0x38:return Ps2Key.LeftAlt;case 0x39:return Ps2Key.Space;case 0x3A:return Ps2Key.CapsLock;case 0x3B:return Ps2Key.F1;case 0x3C:return Ps2Key.F2;case 0x3D:return Ps2Key.F3;case 0x3E:return Ps2Key.F4;case 0x3F:return Ps2Key.F5;case 0x40:return Ps2Key.F6;case 0x41:return Ps2Key.F7;case 0x42:return Ps2Key.F8;case 0x43:return Ps2Key.F9;case 0x44:return Ps2Key.F10;case 0x45:return Ps2Key.NumLock;case 0x46:return Ps2Key.ScrollLock;case 0x47:return Ps2Key.Keypad7;case 0x48:return Ps2Key.Keypad8;case 0x49:return Ps2Key.Keypad9;case 0x4A:return Ps2Key.KeypadMinus;case 0x4B:return Ps2Key.Keypad4;case 0x4C:return Ps2Key.Keypad5;case 0x4D:return Ps2Key.Keypad6;case 0x4E:return Ps2Key.KeypadPlus;case 0x4F:return Ps2Key.Keypad1;case 0x50:return Ps2Key.Keypad2;case 0x51:return Ps2Key.Keypad3;case 0x52:return Ps2Key.Keypad0;case 0x53:return Ps2Key.KeypadDecimal;case 0x56:return Ps2Key.Oem102;case 0x57:return Ps2Key.F11;case 0x58:return Ps2Key.F12;default:return Ps2Key.None;}
    }
    private static void DecodeMouse(Byte b){if(_mouseIndex==0){if((b&0x08)==0)return;_m0=b;_mouseIndex=1;return;}if(_mouseIndex==1){_m1=b;_mouseIndex=2;return;}_m2=b;_mouseIndex=0;Int32 dx=(SByte)_m1,dy=(SByte)_m2;_mouseState=new Ps2MouseState(_mouseState.X+dx,_mouseState.Y-dy,(_m0&1)!=0,(_m0&2)!=0,(_m0&4)!=0,_mouseState.Wheel);_mousePackets++;}
    private static Boolean SendKeyboard(Byte command){if(!WriteData(command)||!ReadData(out Byte ack))return false;return ack==0xFA;}
    private static Boolean SendMouse(Byte command){if(!WriteCommand(0xD4)||!WriteData(command)||!ReadData(out Byte ack))return false;return ack==0xFA;}
    private static Boolean WriteCommand(Byte value){if(!WaitInputEmpty())return false;return Native.WritePort8(CommandPort,value);}
    private static Boolean WriteData(Byte value){if(!WaitInputEmpty())return false;return Native.WritePort8(DataPort,value);}
    private static Boolean ReadData(out Byte value){for(UInt32 i=0;i<100000U;i++){if(!Native.ReadPort8(StatusPort,out Byte s)){value=0;return false;}if((s&1)!=0)return Native.ReadPort8(DataPort,out value);}value=0;return false;}
    private static Boolean WaitInputEmpty(){for(UInt32 i=0;i<100000U;i++){if(!Native.ReadPort8(StatusPort,out Byte s))return false;if((s&2)==0)return true;}return false;}
    private static void Drain(){for(UInt32 i=0;i<32U;i++){if(!Native.ReadPort8(StatusPort,out Byte s)||(s&1)==0)return;Native.ReadPort8(DataPort,out _);}}
}
