using System;
using NovaOryn.Kernel.Internal.X64;

namespace NovaOryn.Kernel.Acpi;

/// <summary>Describes one ACPI Generic Address Structure.</summary>
public readonly struct AcpiGenericAddress
{
    internal AcpiGenericAddress(Byte addressSpace, Byte bitWidth, Byte bitOffset, Byte accessSize, UInt64 address)
    { AddressSpace = addressSpace; BitWidth = bitWidth; BitOffset = bitOffset; AccessSize = accessSize; Address = address; }
    /// <summary>Gets the ACPI address-space identifier.</summary>
    public Byte AddressSpace { get; }
    /// <summary>Gets the register width in bits.</summary>
    public Byte BitWidth { get; }
    /// <summary>Gets the register bit offset.</summary>
    public Byte BitOffset { get; }
    /// <summary>Gets the ACPI access-size encoding.</summary>
    public Byte AccessSize { get; }
    /// <summary>Gets the physical memory address or I/O port number.</summary>
    public UInt64 Address { get; }
    /// <summary>Gets whether the structure names a usable register.</summary>
    public Boolean IsPresent() => Address != 0UL && (AddressSpace == 0U || AddressSpace == 1U);
}

/// <summary>Summarizes platform topology discovered from MADT.</summary>
public readonly struct AcpiMadtCapabilities
{
    internal AcpiMadtCapabilities(Boolean initialized, UInt32 processors, UInt32 ioApics, UInt32 overrides, UInt64 localApic)
    { Initialized = initialized; ProcessorCount = processors; IoApicCount = ioApics; InterruptOverrideCount = overrides; LocalApicAddress = localApic; }
    /// <summary>Gets whether a valid MADT was discovered.</summary>
    public Boolean Initialized { get; }
    /// <summary>Gets the enabled processor count.</summary>
    public UInt32 ProcessorCount { get; }
    /// <summary>Gets the I/O APIC count.</summary>
    public UInt32 IoApicCount { get; }
    /// <summary>Gets the interrupt-source override count.</summary>
    public UInt32 InterruptOverrideCount { get; }
    /// <summary>Gets the local APIC physical base.</summary>
    public UInt64 LocalApicAddress { get; }
}

/// <summary>Provides the MADT platform-driver view over ACPI topology.</summary>
public static class KernelAcpiMadt
{
    /// <summary>Gets current MADT capabilities.</summary>
    public static AcpiMadtCapabilities GetCapabilities()
    {
        Boolean present = KernelAcpi.TryGetTable(KernelAcpi.ApicSignature, out _, out UInt32 length) && length >= 44U;
        KernelAcpi.TryGetLocalApicAddress(out UInt64 lapic);
        return new AcpiMadtCapabilities(present, KernelAcpi.GetProcessorCount(), KernelAcpi.GetIoApicCount(), KernelAcpi.GetInterruptOverrideCount(), lapic);
    }
    /// <summary>Gets one enabled processor.</summary>
    public static Boolean TryGetProcessor(UInt32 index, out AcpiProcessorInfo value) => KernelAcpi.TryGetProcessor(index, out value);
    /// <summary>Gets one I/O APIC.</summary>
    public static Boolean TryGetIoApic(UInt32 index, out AcpiIoApicInfo value) => KernelAcpi.TryGetIoApic(index, out value);
    /// <summary>Gets one interrupt-source override.</summary>
    public static Boolean TryGetInterruptOverride(UInt32 index, out AcpiInterruptOverrideInfo value) => KernelAcpi.TryGetInterruptOverride(index, out value);
}

/// <summary>Provides the MCFG PCI Express platform-driver view.</summary>
public static class KernelAcpiMcfg
{
    /// <summary>Gets the number of validated ECAM allocations.</summary>
    public static UInt32 GetSegmentCount() => KernelAcpi.GetPciEcamCount();
    /// <summary>Gets one ECAM allocation.</summary>
    public static Boolean TryGetSegment(UInt32 index, out AcpiPciEcamInfo value) => KernelAcpi.TryGetPciEcam(index, out value);
}

/// <summary>Provides the HPET platform-driver view.</summary>
public static class KernelAcpiHpet
{
    /// <summary>Gets the firmware HPET description.</summary>
    public static Boolean TryGetDevice(out AcpiHpetInfo value) => KernelAcpi.TryGetHpet(out value);
}

/// <summary>Describes the Fixed ACPI Description Table power-management registers.</summary>
public readonly struct AcpiFadtInfo
{
    internal AcpiFadtInfo(UInt32 flags, UInt16 sci, UInt32 smiCommand, Byte acpiEnable, Byte acpiDisable, Byte pm1EventLength, Byte pm1ControlLength,
        AcpiGenericAddress pm1aEvent, AcpiGenericAddress pm1bEvent, AcpiGenericAddress pm1aControl, AcpiGenericAddress pm1bControl,
        AcpiGenericAddress reset, Byte resetValue, UInt64 dsdt, Byte centuryRegister)
    { Flags = flags; SciInterrupt = sci; SmiCommandPort = smiCommand; AcpiEnableValue = acpiEnable; AcpiDisableValue = acpiDisable; Pm1EventLength = pm1EventLength; Pm1ControlLength = pm1ControlLength; Pm1aEvent = pm1aEvent; Pm1bEvent = pm1bEvent; Pm1aControl = pm1aControl; Pm1bControl = pm1bControl; ResetRegister = reset; ResetValue = resetValue; DsdtAddress = dsdt; CenturyRegister = centuryRegister; }
    /// <summary>Gets the FADT feature flags.</summary>
    public UInt32 Flags { get; }
    /// <summary>Gets the ACPI SCI interrupt.</summary>
    public UInt16 SciInterrupt { get; }
    /// <summary>Gets the SMI command I/O port.</summary>
    public UInt32 SmiCommandPort { get; }
    /// <summary>Gets the firmware ACPI-enable command value.</summary>
    public Byte AcpiEnableValue { get; }
    /// <summary>Gets the firmware ACPI-disable command value.</summary>
    public Byte AcpiDisableValue { get; }
    /// <summary>Gets the PM1 event-block byte length.</summary>
    public Byte Pm1EventLength { get; }
    /// <summary>Gets the PM1 control-block byte length.</summary>
    public Byte Pm1ControlLength { get; }
    /// <summary>Gets PM1a event registers.</summary>
    public AcpiGenericAddress Pm1aEvent { get; }
    /// <summary>Gets PM1b event registers.</summary>
    public AcpiGenericAddress Pm1bEvent { get; }
    /// <summary>Gets PM1a control registers.</summary>
    public AcpiGenericAddress Pm1aControl { get; }
    /// <summary>Gets PM1b control registers.</summary>
    public AcpiGenericAddress Pm1bControl { get; }
    /// <summary>Gets the FADT reset register.</summary>
    public AcpiGenericAddress ResetRegister { get; }
    /// <summary>Gets the FADT reset value.</summary>
    public Byte ResetValue { get; }
    /// <summary>Gets the DSDT physical address.</summary>
    public UInt64 DsdtAddress { get; }
    /// <summary>Gets the CMOS century register index advertised by ACPI, or zero when absent.</summary>
    public Byte CenturyRegister { get; }
    /// <summary>Gets whether ACPI reset is advertised.</summary>
    public Boolean SupportsReset() => (Flags & (1U << 10)) != 0U && ResetRegister.IsPresent();
}

/// <summary>Parses and exposes the Fixed ACPI Description Table.</summary>
public static unsafe class KernelAcpiFadt
{
    private static Boolean _initialized;
    private static AcpiFadtInfo _info;
    /// <summary>Initializes the FADT platform driver.</summary>
    public static Boolean Initialize()
    {
        if (_initialized) return true;
        if (!KernelAcpi.TryGetTable(KernelAcpi.FadtSignature, out UInt64 address, out UInt32 length) || length < 116U) return false;
        Byte* t = (Byte*)address;
        UInt64 dsdt = Read32(t + 40U);
        if (length >= 148U) { UInt64 x = Read64(t + 140U); if (x != 0UL) dsdt = x; }
        AcpiGenericAddress pm1aEvent = LegacyGas(Read32(t + 56U), (Byte)1U, t[88]);
        AcpiGenericAddress pm1bEvent = LegacyGas(Read32(t + 60U), (Byte)1U, t[88]);
        AcpiGenericAddress pm1aControl = LegacyGas(Read32(t + 64U), (Byte)1U, t[89]);
        AcpiGenericAddress pm1bControl = LegacyGas(Read32(t + 68U), (Byte)1U, t[89]);
        if (length >= 196U)
        {
            AcpiGenericAddress x1e = ReadGas(t + 148U); if (x1e.IsPresent()) pm1aEvent = x1e;
            AcpiGenericAddress x2e = ReadGas(t + 160U); if (x2e.IsPresent()) pm1bEvent = x2e;
            AcpiGenericAddress x1c = ReadGas(t + 172U); if (x1c.IsPresent()) pm1aControl = x1c;
            AcpiGenericAddress x2c = ReadGas(t + 184U); if (x2c.IsPresent()) pm1bControl = x2c;
        }
        AcpiGenericAddress reset = length >= 129U ? ReadGas(t + 116U) : default;
        Byte resetValue = length >= 129U ? t[128] : (Byte)0U;
        _info = new AcpiFadtInfo(Read32(t + 112U), Read16(t + 46U), Read32(t + 48U), t[52], t[53], t[88], t[89], pm1aEvent, pm1bEvent, pm1aControl, pm1bControl, reset, resetValue, dsdt, length > 108U ? t[108] : (Byte)0U);
        _initialized = true;
        return true;
    }
    /// <summary>Gets whether FADT initialization succeeded.</summary>
    public static Boolean IsInitialized() => _initialized;
    /// <summary>Gets the parsed FADT information.</summary>
    public static AcpiFadtInfo GetInfo() => _info;
    internal static AcpiGenericAddress ReadGas(Byte* p) => new AcpiGenericAddress(p[0], p[1], p[2], p[3], Read64(p + 4U));
    private static AcpiGenericAddress LegacyGas(UInt32 address, Byte space, Byte bytes) => address == 0U ? default : new AcpiGenericAddress(space, (Byte)(bytes * 8U), (Byte)0, bytes == 1U ? (Byte)1 : bytes == 2U ? (Byte)2 : (Byte)3, address);
    private static UInt16 Read16(Byte* p) => (UInt16)(p[0] | ((UInt16)p[1] << 8));
    private static UInt32 Read32(Byte* p) => (UInt32)(p[0] | ((UInt32)p[1] << 8) | ((UInt32)p[2] << 16) | ((UInt32)p[3] << 24));
    private static UInt64 Read64(Byte* p) => (UInt64)Read32(p) | ((UInt64)Read32(p + 4) << 32);
}

/// <summary>Describes an ECDT-discovered ACPI Embedded Controller.</summary>
public readonly struct AcpiEcInfo
{
    internal AcpiEcInfo(AcpiGenericAddress control, AcpiGenericAddress data, UInt32 uid, Byte gpe)
    { Control = control; Data = data; UniqueId = uid; Gpe = gpe; }
    /// <summary>Gets the EC command/status register.</summary>
    public AcpiGenericAddress Control { get; }
    /// <summary>Gets the EC data register.</summary>
    public AcpiGenericAddress Data { get; }
    /// <summary>Gets the EC UID.</summary>
    public UInt32 UniqueId { get; }
    /// <summary>Gets the EC GPE number.</summary>
    public Byte Gpe { get; }
}

/// <summary>Implements the ACPI Embedded Controller byte command protocol for ECDT-described controllers.</summary>
public static unsafe class KernelAcpiEc
{
    private const UInt32 EcdtSignature = 0x54444345U;
    private const Byte ReadCommand = 0x80;
    private const Byte WriteCommand = 0x81;
    private const Byte OutputBufferFull = 0x01;
    private const Byte InputBufferFull = 0x02;
    private const UInt32 WaitLimit = 1000000U;
    private static Boolean _initialized;
    private static AcpiEcInfo _info;
    /// <summary>Initializes the first ECDT-described embedded controller.</summary>
    public static Boolean Initialize()
    {
        if (_initialized) return true;
        if (!KernelAcpi.TryGetTable(EcdtSignature, out UInt64 address, out UInt32 length) || length < 65U) return false;
        Byte* t = (Byte*)address;
        AcpiGenericAddress control = KernelAcpiFadt.ReadGas(t + 36U);
        AcpiGenericAddress data = KernelAcpiFadt.ReadGas(t + 48U);
        if (!control.IsPresent() || !data.IsPresent()) return false;
        _info = new AcpiEcInfo(control, data, Read32(t + 60U), t[64]);
        _initialized = true;
        return true;
    }
    /// <summary>Gets whether an ECDT embedded controller is available.</summary>
    public static Boolean IsInitialized() => _initialized;
    /// <summary>Gets the ECDT controller information.</summary>
    public static AcpiEcInfo GetInfo() => _info;
    /// <summary>Reads one byte from EC address space.</summary>
    public static Boolean TryRead(Byte address, out Byte value)
    {
        value = (Byte)0; if (!_initialized || !WaitInputEmpty() || !AcpiRegisterAccess.Write(_info.Control, ReadCommand) || !WaitInputEmpty() || !AcpiRegisterAccess.Write(_info.Data, address) || !WaitOutputFull()) return false;
        return AcpiRegisterAccess.Read(_info.Data, out UInt64 raw) && SetByte(raw, out value);
    }
    /// <summary>Writes one byte to EC address space.</summary>
    public static Boolean TryWrite(Byte address, Byte value)
    {
        if (!_initialized || !WaitInputEmpty() || !AcpiRegisterAccess.Write(_info.Control, WriteCommand) || !WaitInputEmpty() || !AcpiRegisterAccess.Write(_info.Data, address) || !WaitInputEmpty()) return false;
        return AcpiRegisterAccess.Write(_info.Data, value);
    }
    private static Boolean WaitInputEmpty() { for (UInt32 i=0U;i<WaitLimit;i++) if (AcpiRegisterAccess.Read(_info.Control,out UInt64 s) && (((Byte)s & InputBufferFull)==0U)) return true; return false; }
    private static Boolean WaitOutputFull() { for (UInt32 i=0U;i<WaitLimit;i++) if (AcpiRegisterAccess.Read(_info.Control,out UInt64 s) && (((Byte)s & OutputBufferFull)!=0U)) return true; return false; }
    private static Boolean SetByte(UInt64 raw, out Byte value) { value=(Byte)raw; return true; }
    private static UInt32 Read32(Byte* p) => (UInt32)(p[0] | ((UInt32)p[1]<<8) | ((UInt32)p[2]<<16) | ((UInt32)p[3]<<24));
}

/// <summary>Describes ACPI fixed-feature power-management capability.</summary>
public readonly struct AcpiPowerCapabilities
{
    internal AcpiPowerCapabilities(Boolean initialized, Boolean button, Boolean reset, Boolean shutdown, Byte s5a, Byte s5b)
    { Initialized=initialized; PowerButtonAvailable=button; ResetAvailable=reset; ShutdownAvailable=shutdown; S5TypeA=s5a; S5TypeB=s5b; }
    /// <summary>Gets whether FADT power management initialized.</summary>
    public Boolean Initialized { get; }
    /// <summary>Gets whether the fixed-feature power button can be polled.</summary>
    public Boolean PowerButtonAvailable { get; }
    /// <summary>Gets whether the FADT reset register is available.</summary>
    public Boolean ResetAvailable { get; }
    /// <summary>Gets whether S5 shutdown values were discovered from AML.</summary>
    public Boolean ShutdownAvailable { get; }
    /// <summary>Gets the PM1a S5 sleep type.</summary>
    public Byte S5TypeA { get; }
    /// <summary>Gets the PM1b S5 sleep type.</summary>
    public Byte S5TypeB { get; }
}

/// <summary>Implements ACPI fixed-feature power-button, reset and S5 shutdown services.</summary>
public static unsafe class KernelAcpiPower
{
    private const UInt16 PowerButtonBit = (UInt16)(1U << 8);
    private const UInt16 SleepEnableBit = (UInt16)(1U << 13);
    private static Boolean _initialized;
    private static Boolean _hasS5;
    private static Byte _s5a;
    private static Byte _s5b;
    /// <summary>Initializes FADT power management and discovers AML _S5 sleep types.</summary>
    public static Boolean Initialize()
    {
        if (_initialized) return true;
        if (!KernelAcpiFadt.Initialize()) return false;
        AcpiFadtInfo f = KernelAcpiFadt.GetInfo();
        if (f.SmiCommandPort != 0U && f.AcpiEnableValue != 0U && f.SmiCommandPort <= 0xFFFFU) Native.WritePort8((UInt16)f.SmiCommandPort, f.AcpiEnableValue);
        _hasS5 = TryFindS5(f.DsdtAddress, out _s5a, out _s5b);
        _initialized = true;
        EnablePowerButton();
        return true;
    }
    /// <summary>Gets fixed-feature power-management capabilities.</summary>
    public static AcpiPowerCapabilities GetCapabilities()
    {
        AcpiFadtInfo f=KernelAcpiFadt.GetInfo();
        return new AcpiPowerCapabilities(_initialized, _initialized && f.Pm1aEvent.IsPresent(), _initialized && f.SupportsReset(), _initialized && _hasS5 && f.Pm1aControl.IsPresent(), _s5a, _s5b);
    }
    /// <summary>Gets whether the fixed-feature power button has asserted its status bit and clears it when set.</summary>
    public static Boolean TryConsumePowerButton(out Boolean pressed)
    {
        pressed=false; if(!_initialized) return false; AcpiFadtInfo f=KernelAcpiFadt.GetInfo(); if(!f.Pm1aEvent.IsPresent() || f.Pm1EventLength<4U) return false;
        AcpiGenericAddress status=new AcpiGenericAddress(f.Pm1aEvent.AddressSpace,(Byte)16,(Byte)0,(Byte)2,f.Pm1aEvent.Address);
        if(!AcpiRegisterAccess.Read(status,out UInt64 raw)) return false; pressed=(((UInt16)raw)&PowerButtonBit)!=0U;
        if(pressed && !AcpiRegisterAccess.Write(status,PowerButtonBit)) return false; return true;
    }
    /// <summary>Requests firmware-defined ACPI reset through the FADT reset register.</summary>
    public static Boolean Reboot()
    {
        if(!_initialized) return false; AcpiFadtInfo f=KernelAcpiFadt.GetInfo(); if(!f.SupportsReset()) return false; return AcpiRegisterAccess.Write(f.ResetRegister,f.ResetValue);
    }
    /// <summary>Requests ACPI S5 soft-off using AML-discovered sleep types.</summary>
    public static Boolean Shutdown()
    {
        if(!_initialized || !_hasS5) return false; AcpiFadtInfo f=KernelAcpiFadt.GetInfo(); if(!f.Pm1aControl.IsPresent()) return false;
        if(!AcpiRegisterAccess.Read(f.Pm1aControl,out UInt64 a)) return false; UInt16 av=(UInt16)(((UInt16)a & ~(UInt16)(7U<<10)) | ((UInt16)(_s5a & 7U)<<10) | SleepEnableBit);
        if(!AcpiRegisterAccess.Write(f.Pm1aControl,av)) return false;
        if(f.Pm1bControl.IsPresent()) { if(!AcpiRegisterAccess.Read(f.Pm1bControl,out UInt64 b)) return false; UInt16 bv=(UInt16)(((UInt16)b & ~(UInt16)(7U<<10)) | ((UInt16)(_s5b & 7U)<<10) | SleepEnableBit); if(!AcpiRegisterAccess.Write(f.Pm1bControl,bv)) return false; }
        return true;
    }
    private static Boolean EnablePowerButton()
    {
        AcpiFadtInfo f=KernelAcpiFadt.GetInfo(); if(!f.Pm1aEvent.IsPresent() || f.Pm1EventLength<4U) return false;
        AcpiGenericAddress enable=new AcpiGenericAddress(f.Pm1aEvent.AddressSpace,(Byte)16,(Byte)0,(Byte)2,f.Pm1aEvent.Address+(UInt64)(f.Pm1EventLength/2U));
        if(!AcpiRegisterAccess.Read(enable,out UInt64 raw)) return false; return AcpiRegisterAccess.Write(enable,(UInt16)((UInt16)raw|PowerButtonBit));
    }
    private static Boolean TryFindS5(UInt64 dsdtAddress,out Byte a,out Byte b)
    {
        a=(Byte)0;b=(Byte)0;if(dsdtAddress==0UL)return false;Byte* t=(Byte*)dsdtAddress;UInt32 length=Read32(t+4U);if(length<36U||length>16U*1024U*1024U)return false;
        for(UInt32 i=36U;i+5U<length;i++) if(t[i]==0x5FU&&t[i+1U]==0x53U&&t[i+2U]==0x35U&&t[i+3U]==0x5FU)
        { UInt32 p=i+4U; if(p<length&&t[p]==0x12U){p++; if(!SkipPkgLength(t,length,ref p)||p>=length)return false; p++; if(!ReadAmlInteger(t,length,ref p,out UInt64 x)||!ReadAmlInteger(t,length,ref p,out UInt64 y))return false; a=(Byte)x;b=(Byte)y;return true;} }
        return false;
    }
    private static Boolean SkipPkgLength(Byte* t,UInt32 length,ref UInt32 p){if(p>=length)return false;Byte lead=t[p++];UInt32 follow=(UInt32)(lead>>6);if(follow==0U)return true;if(p+follow>length)return false;p+=follow;return true;}
    private static Boolean ReadAmlInteger(Byte* t,UInt32 length,ref UInt32 p,out UInt64 v){v=0UL;if(p>=length)return false;Byte op=t[p++];if(op==0x00U){v=0;return true;}if(op==0x01U){v=1;return true;}if(op==0x0AU&&p<length){v=t[p++];return true;}if(op==0x0BU&&p+2U<=length){v=(UInt64)(t[p]|((UInt16)t[p+1U]<<8));p+=2U;return true;}if(op==0x0CU&&p+4U<=length){v=Read32(t+p);p+=4U;return true;}return false;}
    private static UInt32 Read32(Byte* p)=>(UInt32)(p[0]|((UInt32)p[1]<<8)|((UInt32)p[2]<<16)|((UInt32)p[3]<<24));
}

internal static unsafe class AcpiRegisterAccess
{
    internal static Boolean Read(AcpiGenericAddress gas,out UInt64 value)
    {
        value=0UL;if(!gas.IsPresent()||gas.BitOffset!=0U)return false;Byte width=gas.BitWidth==0U?AccessWidth(gas.AccessSize):gas.BitWidth;
        if(gas.AddressSpace==1U){if(gas.Address>0xFFFFUL)return false;UInt16 port=(UInt16)gas.Address;if(width<=8U){if(!Native.ReadPort8(port,out Byte v))return false;value=v;return true;}if(width<=16U){if(!Native.ReadPort16(port,out UInt16 v))return false;value=v;return true;}if(width<=32U){if(!Native.ReadPort32(port,out UInt32 v))return false;value=v;return true;}return false;}
        Byte* p=(Byte*)gas.Address;if(width<=8U){value=*p;return true;}if(width<=16U){value=*(UInt16*)p;return true;}if(width<=32U){value=*(UInt32*)p;return true;}if(width<=64U){value=*(UInt64*)p;return true;}return false;
    }
    internal static Boolean Write(AcpiGenericAddress gas,UInt64 value)
    {
        if(!gas.IsPresent()||gas.BitOffset!=0U)return false;Byte width=gas.BitWidth==0U?AccessWidth(gas.AccessSize):gas.BitWidth;
        if(gas.AddressSpace==1U){if(gas.Address>0xFFFFUL)return false;UInt16 port=(UInt16)gas.Address;if(width<=8U)return Native.WritePort8(port,(Byte)value);if(width<=16U)return Native.WritePort16(port,(UInt16)value);if(width<=32U)return Native.WritePort32(port,(UInt32)value);return false;}
        Byte* p=(Byte*)gas.Address;if(width<=8U){*p=(Byte)value;return true;}if(width<=16U){*(UInt16*)p=(UInt16)value;return true;}if(width<=32U){*(UInt32*)p=(UInt32)value;return true;}if(width<=64U){*(UInt64*)p=value;return true;}return false;
    }
    private static Byte AccessWidth(Byte accessSize)=>accessSize==1U?(Byte)8U:accessSize==2U?(Byte)16U:accessSize==3U?(Byte)32U:accessSize==4U?(Byte)64U:(Byte)8U;
}
