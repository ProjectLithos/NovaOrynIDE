using System;
using NovaOryn.Kernel.Console;

namespace NovaOryn.Kernel.Acpi;

/// <summary>Reports the result of ACPI root discovery and validation.</summary>
public enum KernelAcpiStatus
{
    /// <summary>ACPI discovery completed successfully.</summary>
    Success = 0,
    /// <summary>The UEFI boot hand-off did not provide an ACPI root pointer.</summary>
    RootPointerUnavailable = 1,
    /// <summary>The RSDP signature or checksum is invalid.</summary>
    InvalidRootPointer = 2,
    /// <summary>No usable RSDT or XSDT was advertised by the RSDP.</summary>
    RootTableUnavailable = 3,
    /// <summary>The selected RSDT or XSDT failed structural or checksum validation.</summary>
    InvalidRootTable = 4,
    /// <summary>The root table contains an unsupported number of entries.</summary>
    RootTableTooLarge = 5
}

/// <summary>Describes one processor advertised by the ACPI Multiple APIC Description Table.</summary>
public readonly struct AcpiProcessorInfo
{
    internal AcpiProcessorInfo(UInt32 apicId, UInt32 acpiUid, Boolean x2Apic, Boolean enabled)
    { ApicId = apicId; AcpiUid = acpiUid; IsX2Apic = x2Apic; IsEnabled = enabled; }
    /// <summary>Gets the local APIC or x2APIC identifier.</summary>
    public UInt32 ApicId { get; }
    /// <summary>Gets the ACPI processor UID.</summary>
    public UInt32 AcpiUid { get; }
    /// <summary>Gets whether this record came from an x2APIC entry.</summary>
    public Boolean IsX2Apic { get; }
    /// <summary>Gets whether firmware marks the processor enabled or online-capable.</summary>
    public Boolean IsEnabled { get; }
}

/// <summary>Describes one I/O APIC advertised by ACPI.</summary>
public readonly struct AcpiIoApicInfo
{
    internal AcpiIoApicInfo(Byte id, UInt32 address, UInt32 globalSystemInterruptBase)
    { Id = id; Address = address; GlobalSystemInterruptBase = globalSystemInterruptBase; }
    /// <summary>Gets the firmware I/O APIC identifier.</summary>
    public Byte Id { get; }
    /// <summary>Gets the physical MMIO base.</summary>
    public UInt32 Address { get; }
    /// <summary>Gets the first global system interrupt routed by this controller.</summary>
    public UInt32 GlobalSystemInterruptBase { get; }
}

/// <summary>Describes one ACPI interrupt source override.</summary>
public readonly struct AcpiInterruptOverrideInfo
{
    internal AcpiInterruptOverrideInfo(Byte bus, Byte source, UInt32 globalSystemInterrupt, UInt16 flags)
    { Bus = bus; Source = source; GlobalSystemInterrupt = globalSystemInterrupt; Flags = flags; }
    /// <summary>Gets the source bus; zero identifies the legacy ISA bus.</summary>
    public Byte Bus { get; }
    /// <summary>Gets the bus-relative interrupt source.</summary>
    public Byte Source { get; }
    /// <summary>Gets the replacement global system interrupt.</summary>
    public UInt32 GlobalSystemInterrupt { get; }
    /// <summary>Gets ACPI polarity and trigger-mode flags.</summary>
    public UInt16 Flags { get; }
}

/// <summary>Describes one PCI Express ECAM segment from the ACPI MCFG table.</summary>
public readonly struct AcpiPciEcamInfo
{
    internal AcpiPciEcamInfo(UInt64 baseAddress, UInt16 segment, Byte startBus, Byte endBus)
    { BaseAddress = baseAddress; SegmentGroup = segment; StartBus = startBus; EndBus = endBus; }
    /// <summary>Gets the physical ECAM base.</summary>
    public UInt64 BaseAddress { get; }
    /// <summary>Gets the PCI segment group.</summary>
    public UInt16 SegmentGroup { get; }
    /// <summary>Gets the first bus covered by this allocation.</summary>
    public Byte StartBus { get; }
    /// <summary>Gets the last bus covered by this allocation.</summary>
    public Byte EndBus { get; }
}

/// <summary>Describes the ACPI HPET hardware block.</summary>
public readonly struct AcpiHpetInfo
{
    internal AcpiHpetInfo(UInt64 baseAddress, Byte addressSpace, UInt16 minimumTick, Byte sequence, UInt32 blockId)
    { BaseAddress = baseAddress; AddressSpace = addressSpace; MinimumTick = minimumTick; Sequence = sequence; EventTimerBlockId = blockId; }
    /// <summary>Gets the timer-register base address.</summary>
    public UInt64 BaseAddress { get; }
    /// <summary>Gets the ACPI generic-address-space identifier.</summary>
    public Byte AddressSpace { get; }
    /// <summary>Gets the minimum periodic tick in clock ticks.</summary>
    public UInt16 MinimumTick { get; }
    /// <summary>Gets the HPET sequence number.</summary>
    public Byte Sequence { get; }
    /// <summary>Gets the event-timer block identifier.</summary>
    public UInt32 EventTimerBlockId { get; }
}

/// <summary>Performs allocation-free ACPI discovery from the UEFI-supplied RSDP.</summary>
public static unsafe class KernelAcpi
{
    /// <summary>Little-endian signature for APIC/MADT.</summary>
    public const UInt32 ApicSignature = 0x43495041U;
    /// <summary>Little-endian signature for the Fixed ACPI Description Table.</summary>
    public const UInt32 FadtSignature = 0x50434146U;
    /// <summary>Little-endian signature for HPET.</summary>
    public const UInt32 HpetSignature = 0x54455048U;
    /// <summary>Little-endian signature for PCI Express MCFG.</summary>
    public const UInt32 McfgSignature = 0x4746434DU;
    private const UInt32 RsdtSignature = 0x54445352U;
    private const UInt32 XsdtSignature = 0x54445358U;
    private const UInt32 MaximumTableLength = 16U * 1024U * 1024U;
    private const UInt32 MaximumRootEntries = 4096U;
    private static Boolean _initialized;
    private static KernelAcpiStatus _status = KernelAcpiStatus.RootPointerUnavailable;
    private static UInt64 _rsdpAddress;
    private static UInt64 _rootAddress;
    private static Boolean _usesXsdt;
    private static UInt32 _rootEntryCount;
    private static Byte _revision;

    /// <summary>Gets whether ACPI discovery completed successfully.</summary>
    public static Boolean IsInitialized() => _initialized;
    /// <summary>Gets the most recent discovery status.</summary>
    public static KernelAcpiStatus GetLastStatus() => _status;
    /// <summary>Gets a freestanding-safe status name.</summary>
    public static String GetLastStatusName()
    {
        if (_status == KernelAcpiStatus.Success) return "Success";
        if (_status == KernelAcpiStatus.RootPointerUnavailable) return "RootPointerUnavailable";
        if (_status == KernelAcpiStatus.InvalidRootPointer) return "InvalidRootPointer";
        if (_status == KernelAcpiStatus.RootTableUnavailable) return "RootTableUnavailable";
        if (_status == KernelAcpiStatus.InvalidRootTable) return "InvalidRootTable";
        if (_status == KernelAcpiStatus.RootTableTooLarge) return "RootTableTooLarge";
        return "Unknown";
    }
    /// <summary>Gets the physical RSDP address captured from the UEFI configuration table.</summary>
    public static UInt64 GetRootPointerAddress() => _rsdpAddress;
    /// <summary>Gets the selected RSDT/XSDT physical address.</summary>
    public static UInt64 GetRootTableAddress() => _rootAddress;
    /// <summary>Gets whether the active root is the 64-bit XSDT.</summary>
    public static Boolean UsesXsdt() => _usesXsdt;
    /// <summary>Gets the ACPI revision advertised by the RSDP.</summary>
    public static Byte GetRevision() => _revision;
    /// <summary>Gets the number of table pointers in the selected root table.</summary>
    public static UInt32 GetRootTableCount() => _rootEntryCount;

    /// <summary>Validates the RSDP and root table supplied by firmware.</summary>
    /// <returns><see langword="true"/> when a checksummed RSDT or XSDT is ready for table discovery.</returns>
    public static Boolean Initialize(BootContext boot)
    {
        if (_initialized) return true;
        _rsdpAddress = boot.GetAcpiRootPointerAddress();
        if (_rsdpAddress == 0UL) { _status = KernelAcpiStatus.RootPointerUnavailable; return false; }
        Byte* rsdp = (Byte*)_rsdpAddress;
        if (!HasRsdpSignature(rsdp) || !ChecksumIsZero(rsdp, 20U)) { _status = KernelAcpiStatus.InvalidRootPointer; return false; }
        _revision = rsdp[15];
        UInt32 rsdt = Read32(rsdp + 16);
        UInt64 xsdt = 0UL;
        if (_revision >= 2)
        {
            UInt32 length = Read32(rsdp + 20);
            if (length < 36U || length > 4096U || !ChecksumIsZero(rsdp, length)) { _status = KernelAcpiStatus.InvalidRootPointer; return false; }
            xsdt = Read64(rsdp + 24);
        }
        _usesXsdt = xsdt != 0UL;
        _rootAddress = _usesXsdt ? xsdt : rsdt;
        if (_rootAddress == 0UL) { _status = KernelAcpiStatus.RootTableUnavailable; return false; }
        Byte* root = (Byte*)_rootAddress;
        UInt32 expected = _usesXsdt ? XsdtSignature : RsdtSignature;
        if (Read32(root) != expected || !TryValidateTable(root, out UInt32 rootLength)) { _status = KernelAcpiStatus.InvalidRootTable; return false; }
        UInt32 entrySize = _usesXsdt ? 8U : 4U;
        UInt32 payload = rootLength - 36U;
        if (payload % entrySize != 0U) { _status = KernelAcpiStatus.InvalidRootTable; return false; }
        _rootEntryCount = payload / entrySize;
        if (_rootEntryCount > MaximumRootEntries) { _status = KernelAcpiStatus.RootTableTooLarge; return false; }
        _initialized = true;
        _status = KernelAcpiStatus.Success;
        return true;
    }

    /// <summary>Finds the first validated ACPI table with the requested four-byte signature.</summary>
    /// <returns><see langword="true"/> when a checksummed table was found.</returns>
    public static Boolean TryGetTable(UInt32 signature, out UInt64 address, out UInt32 length)
    {
        address = 0UL; length = 0U;
        if (!_initialized) return false;
        Byte* root = (Byte*)_rootAddress;
        UInt32 entrySize = _usesXsdt ? 8U : 4U;
        for (UInt32 index = 0U; index < _rootEntryCount; index++)
        {
            Byte* entry = root + 36U + index * entrySize;
            UInt64 candidateAddress = _usesXsdt ? Read64(entry) : Read32(entry);
            if (candidateAddress == 0UL) continue;
            Byte* candidate = (Byte*)candidateAddress;
            if (Read32(candidate) != signature) continue;
            if (!TryValidateTable(candidate, out UInt32 candidateLength)) continue;
            address = candidateAddress; length = candidateLength; return true;
        }
        return false;
    }

    /// <summary>Gets the count of enabled or online-capable processors in the MADT.</summary>
    public static UInt32 GetProcessorCount() => CountMadtEntries((Byte)0U, (Byte)9U, true);
    /// <summary>Gets the count of I/O APIC entries in the MADT.</summary>
    public static UInt32 GetIoApicCount() => CountMadtEntries((Byte)1U, (Byte)0xFFU, false);
    /// <summary>Gets the count of interrupt-source overrides in the MADT.</summary>
    public static UInt32 GetInterruptOverrideCount() => CountMadtEntries((Byte)2U, (Byte)0xFFU, false);

    /// <summary>Gets one enabled processor record by logical discovery index.</summary>
    public static Boolean TryGetProcessor(UInt32 requestedIndex, out AcpiProcessorInfo processor)
    {
        processor = default;
        if (!TryGetTable(ApicSignature, out UInt64 address, out UInt32 length) || length < 44U) return false;
        Byte* table = (Byte*)address; UInt32 offset = 44U; UInt32 found = 0U;
        while (offset + 2U <= length)
        {
            Byte type = table[offset]; Byte entryLength = table[offset + 1U];
            if (entryLength < 2U || offset + entryLength > length) return false;
            if (type == 0U && entryLength >= 8U)
            {
                UInt32 flags = Read32(table + offset + 4U); Boolean enabled = (flags & 3U) != 0U;
                if (enabled && found++ == requestedIndex) { processor = new AcpiProcessorInfo(table[offset + 3U], table[offset + 2U], false, true); return true; }
            }
            else if (type == 9U && entryLength >= 16U)
            {
                UInt32 flags = Read32(table + offset + 8U); Boolean enabled = (flags & 3U) != 0U;
                if (enabled && found++ == requestedIndex) { processor = new AcpiProcessorInfo(Read32(table + offset + 4U), Read32(table + offset + 12U), true, true); return true; }
            }
            offset += entryLength;
        }
        return false;
    }

    /// <summary>Gets one I/O APIC record by discovery index.</summary>
    public static Boolean TryGetIoApic(UInt32 requestedIndex, out AcpiIoApicInfo ioApic)
    {
        ioApic = default;
        if (!TryGetTable(ApicSignature, out UInt64 address, out UInt32 length) || length < 44U) return false;
        Byte* table = (Byte*)address; UInt32 offset = 44U; UInt32 found = 0U;
        while (offset + 2U <= length)
        {
            Byte type = table[offset]; Byte entryLength = table[offset + 1U];
            if (entryLength < 2U || offset + entryLength > length) return false;
            if (type == 1U && entryLength >= 12U && found++ == requestedIndex)
            { ioApic = new AcpiIoApicInfo(table[offset + 2U], Read32(table + offset + 4U), Read32(table + offset + 8U)); return true; }
            offset += entryLength;
        }
        return false;
    }

    /// <summary>Gets one interrupt-source override by discovery index.</summary>
    public static Boolean TryGetInterruptOverride(UInt32 requestedIndex, out AcpiInterruptOverrideInfo interruptOverride)
    {
        interruptOverride = default;
        if (!TryGetTable(ApicSignature, out UInt64 address, out UInt32 length) || length < 44U) return false;
        Byte* table = (Byte*)address; UInt32 offset = 44U; UInt32 found = 0U;
        while (offset + 2U <= length)
        {
            Byte type = table[offset]; Byte entryLength = table[offset + 1U];
            if (entryLength < 2U || offset + entryLength > length) return false;
            if (type == 2U && entryLength >= 10U && found++ == requestedIndex)
            { interruptOverride = new AcpiInterruptOverrideInfo(table[offset + 2U], table[offset + 3U], Read32(table + offset + 4U), Read16(table + offset + 8U)); return true; }
            offset += entryLength;
        }
        return false;
    }

    /// <summary>Gets the count of PCI Express ECAM allocations in the MCFG table.</summary>
    public static UInt32 GetPciEcamCount()
    {
        if (!TryGetTable(McfgSignature, out UInt64 address, out UInt32 length) || length < 44U) return 0U;
        return (length - 44U) / 16U;
    }

    /// <summary>Gets one PCI Express ECAM allocation by discovery index.</summary>
    public static Boolean TryGetPciEcam(UInt32 index, out AcpiPciEcamInfo ecam)
    {
        ecam = default;
        if (!TryGetTable(McfgSignature, out UInt64 address, out UInt32 length) || length < 44U) return false;
        UInt32 count = (length - 44U) / 16U; if (index >= count) return false;
        Byte* entry = (Byte*)address + 44U + index * 16U;
        Byte startBus = entry[10]; Byte endBus = entry[11]; if (startBus > endBus) return false;
        ecam = new AcpiPciEcamInfo(Read64(entry), Read16(entry + 8U), startBus, endBus); return true;
    }

    /// <summary>Gets HPET register-discovery information when the HPET table is present.</summary>
    public static Boolean TryGetHpet(out AcpiHpetInfo hpet)
    {
        hpet = default;
        if (!TryGetTable(HpetSignature, out UInt64 address, out UInt32 length) || length < 56U) return false;
        Byte* table = (Byte*)address;
        UInt64 baseAddress = Read64(table + 44U); if (baseAddress == 0UL) return false;
        hpet = new AcpiHpetInfo(baseAddress, table[40], Read16(table + 53U), table[52], Read32(table + 36U)); return true;
    }

    /// <summary>Gets the Local APIC physical base advertised by MADT.</summary>
    public static Boolean TryGetLocalApicAddress(out UInt64 address)
    {
        address = 0UL;
        if (!TryGetTable(ApicSignature, out UInt64 tableAddress, out UInt32 length) || length < 44U) return false;
        address = Read32((Byte*)tableAddress + 36U);
        Byte* table = (Byte*)tableAddress; UInt32 offset = 44U;
        while (offset + 2U <= length)
        {
            Byte type = table[offset]; Byte entryLength = table[offset + 1U];
            if (entryLength < 2U || offset + entryLength > length) return false;
            if (type == 5U && entryLength >= 12U) address = Read64(table + offset + 4U);
            offset += entryLength;
        }
        return address != 0UL;
    }

    private static UInt32 CountMadtEntries(Byte firstType, Byte secondType, Boolean onlyEnabledProcessors)
    {
        if (!TryGetTable(ApicSignature, out UInt64 address, out UInt32 length) || length < 44U) return 0U;
        Byte* table = (Byte*)address; UInt32 offset = 44U; UInt32 count = 0U;
        while (offset + 2U <= length)
        {
            Byte type = table[offset]; Byte entryLength = table[offset + 1U];
            if (entryLength < 2U || offset + entryLength > length) return count;
            if (type == firstType || type == secondType)
            {
                if (!onlyEnabledProcessors) count++;
                else if (type == 0U && entryLength >= 8U && (Read32(table + offset + 4U) & 3U) != 0U) count++;
                else if (type == 9U && entryLength >= 16U && (Read32(table + offset + 8U) & 3U) != 0U) count++;
            }
            offset += entryLength;
        }
        return count;
    }

    private static Boolean TryValidateTable(Byte* table, out UInt32 length)
    {
        length = 0U; if (table == null) return false;
        UInt32 candidateLength = Read32(table + 4U);
        if (candidateLength < 36U || candidateLength > MaximumTableLength) return false;
        if (!ChecksumIsZero(table, candidateLength)) return false;
        length = candidateLength; return true;
    }

    private static Boolean HasRsdpSignature(Byte* value)
    {
        return value != null && value[0] == 0x52U && value[1] == 0x53U && value[2] == 0x44U && value[3] == 0x20U &&
            value[4] == 0x50U && value[5] == 0x54U && value[6] == 0x52U && value[7] == 0x20U;
    }

    private static Boolean ChecksumIsZero(Byte* value, UInt32 length)
    {
        Byte sum = (Byte)0U; for (UInt32 index = 0U; index < length; index++) sum = (Byte)(sum + value[index]); return sum == (Byte)0U;
    }
    private static UInt16 Read16(Byte* value) => (UInt16)(value[0] | ((UInt16)value[1] << 8));
    private static UInt32 Read32(Byte* value) => (UInt32)(value[0] | ((UInt32)value[1] << 8) | ((UInt32)value[2] << 16) | ((UInt32)value[3] << 24));
    private static UInt64 Read64(Byte* value) => (UInt64)Read32(value) | ((UInt64)Read32(value + 4) << 32);
}
