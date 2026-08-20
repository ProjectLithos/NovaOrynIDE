using System;
using NovaOryn.Kernel.Contracts;
using NovaOryn.Kernel.Internal.X64;
using NovaOryn.Kernel.Memory;

namespace NovaOryn.Kernel.VirtualMemory;

/// <summary>Reports the result of a freestanding virtual-memory operation.</summary>
public enum KernelVirtualMemoryStatus
{
    /// <summary>The operation completed successfully.</summary>
    Success = 0,
    /// <summary>The supplied address, page size, or protection was invalid.</summary>
    InvalidParameter = 1,
    /// <summary>The virtual-memory manager has not been initialized.</summary>
    NotInitialized = 2,
    /// <summary>The virtual-memory manager was already initialized.</summary>
    AlreadyInitialized = 3,
    /// <summary>The supplied virtual address is not canonical for four-level x64 paging.</summary>
    NonCanonicalAddress = 4,
    /// <summary>A leaf mapping already occupies the requested address.</summary>
    AlreadyMapped = 5,
    /// <summary>No present leaf mapping covers the requested address.</summary>
    NotMapped = 6,
    /// <summary>The physical-memory manager could not supply a page-table page.</summary>
    PhysicalAllocationFailed = 7,
    /// <summary>The bounded bootstrap page-table discovery table is full.</summary>
    PageTableCapacityExhausted = 8,
    /// <summary>A required x64 control-register or TLB invalidation operation failed.</summary>
    ArchitectureOperationFailed = 9,
    /// <summary>The requested page size is not supported by this bootstrap manager.</summary>
    UnsupportedPageSize = 10,
    /// <summary>The requested permissions cannot be represented without widening an existing ancestor entry.</summary>
    UnsupportedProtection = 11
}

/// <summary>Identifies a leaf page size supported by the freestanding x64 virtual-memory manager.</summary>
public enum KernelVirtualPageSize : ulong
{
    /// <summary>A standard 4 KiB page.</summary>
    Page4KiB = 4096UL,
    /// <summary>A 2 MiB large page.</summary>
    Page2MiB = 2097152UL,
    /// <summary>A 1 GiB large page.</summary>
    Page1GiB = 1073741824UL
}

/// <summary>Defines bit-combinable access and cache intent for a freestanding virtual mapping.</summary>
public enum KernelVirtualMemoryProtection : ulong
{
    /// <summary>No access permission is selected.</summary>
    None = 0UL,
    /// <summary>The mapping may be read.</summary>
    Read = 1UL << 0,
    /// <summary>The mapping may be written.</summary>
    Write = 1UL << 1,
    /// <summary>Instructions may execute from the mapping.</summary>
    Execute = 1UL << 2,
    /// <summary>User mode may access the mapping subject to its other permissions.</summary>
    User = 1UL << 3,
    /// <summary>The translation may remain global across address-space switches.</summary>
    Global = 1UL << 4,
    /// <summary>The mapping requests uncached device-memory semantics.</summary>
    Device = 1UL << 5,
    /// <summary>The mapping requests write-through caching.</summary>
    WriteThrough = 1UL << 6
}

/// <summary>Reports one exact freestanding virtual-to-physical translation.</summary>
public readonly struct KernelVirtualTranslation
{
    internal KernelVirtualTranslation(UInt64 virtualAddress, UInt64 physicalAddress, KernelVirtualPageSize pageSize, KernelVirtualMemoryProtection protection)
    {
        VirtualAddress = virtualAddress;
        PhysicalAddress = physicalAddress;
        PageSize = pageSize;
        Protection = protection;
    }

    /// <summary>Gets the queried virtual byte address.</summary>
    public UInt64 VirtualAddress { get; }
    /// <summary>Gets the translated physical byte address including the leaf offset.</summary>
    public UInt64 PhysicalAddress { get; }
    /// <summary>Gets the page size of the leaf that supplied the translation.</summary>
    public KernelVirtualPageSize PageSize { get; }
    /// <summary>Gets the decoded leaf permissions and cache intent.</summary>
    public KernelVirtualMemoryProtection Protection { get; }
}

/// <summary>Provides a snapshot of bootstrap x64 virtual-memory accounting.</summary>
public readonly struct KernelVirtualMemoryStatistics
{
    internal KernelVirtualMemoryStatistics(UInt64 protectedBootTables, UInt64 createdTables, UInt64 mapped4KiB, UInt64 mapped2MiB, UInt64 mapped1GiB)
    {
        ProtectedBootPageTables = protectedBootTables;
        CreatedPageTables = createdTables;
        MappedPages4KiB = mapped4KiB;
        MappedPages2MiB = mapped2MiB;
        MappedPages1GiB = mapped1GiB;
    }

    /// <summary>Gets the number of inherited active page-table pages protected from physical allocation.</summary>
    public UInt64 ProtectedBootPageTables { get; }
    /// <summary>Gets the number of new page-table pages allocated by this manager.</summary>
    public UInt64 CreatedPageTables { get; }
    /// <summary>Gets the number of 4 KiB leaf mappings installed through this manager.</summary>
    public UInt64 MappedPages4KiB { get; }
    /// <summary>Gets the number of 2 MiB leaf mappings installed through this manager.</summary>
    public UInt64 MappedPages2MiB { get; }
    /// <summary>Gets the number of 1 GiB leaf mappings installed through this manager.</summary>
    public UInt64 MappedPages1GiB { get; }
}

/// <summary>Provides the default no-heap x64 virtual-memory manager used by the editable kernel template.</summary>
/// <remarks>
/// Version 0.2.0 attaches to the active UEFI-created four-level address space, protects every discovered
/// page-table page from the physical allocator, and supports 4 KiB, 2 MiB, and 1 GiB leaf operations.
/// Until kernel address-space design is introduced, page-table physical pages must remain identity-accessible.
/// </remarks>
public static unsafe partial class KernelVirtualMemory
{
    private const UInt64 Present = 1UL << 0;
    private const UInt64 Writable = 1UL << 1;
    private const UInt64 User = 1UL << 2;
    private const UInt64 WriteThrough = 1UL << 3;
    private const UInt64 CacheDisable = 1UL << 4;
    private const UInt64 LargePage = 1UL << 7;
    private const UInt64 Global = 1UL << 8;
    private const UInt64 NoExecute = 1UL << 63;
    private const UInt64 AddressMask4KiB = 0x000FFFFFFFFFF000UL;
    private const UInt64 AddressMask2MiB = 0x000FFFFFFFE00000UL;
    private const UInt64 AddressMask1GiB = 0x000FFFFFC0000000UL;
    private const Int32 MaximumProtectedTables = 4096;

    private unsafe struct State
    {
        internal fixed UInt64 ProtectedTables[MaximumProtectedTables];
    }

#pragma warning disable CS0169 // Fixed-buffer member access is not counted as use of the containing freestanding state field.
    private static State _state;
#pragma warning restore CS0169
    private static Int32 _protectedTableCount;
    private static UInt64 _rootPhysicalAddress;
    private static UInt64 _createdPageTables;
    private static UInt64 _mapped4KiB;
    private static UInt64 _mapped2MiB;
    private static UInt64 _mapped1GiB;
    private static Boolean _executeDisableEnabled;
    private static Boolean _page1GiBSupported;
    private static Boolean _initialized;
    private static KernelVirtualMemoryStatus _lastStatus;

    /// <summary>Gets whether the bootstrap virtual-memory manager is initialized.</summary>
    public static Boolean IsInitialized() => _initialized;

    /// <summary>Gets the status produced by the most recent virtual-memory operation.</summary>
    public static KernelVirtualMemoryStatus GetLastStatus() => _lastStatus;

    /// <summary>Gets a freestanding-safe symbolic name for the latest virtual-memory status.</summary>
    /// <returns>A stable status name that requires no enum formatting support.</returns>
    public static String GetLastStatusName()
    {
        if (_lastStatus == KernelVirtualMemoryStatus.Success) return "Success";
        if (_lastStatus == KernelVirtualMemoryStatus.InvalidParameter) return "InvalidParameter";
        if (_lastStatus == KernelVirtualMemoryStatus.NotInitialized) return "NotInitialized";
        if (_lastStatus == KernelVirtualMemoryStatus.AlreadyInitialized) return "AlreadyInitialized";
        if (_lastStatus == KernelVirtualMemoryStatus.NonCanonicalAddress) return "NonCanonicalAddress";
        if (_lastStatus == KernelVirtualMemoryStatus.AlreadyMapped) return "AlreadyMapped";
        if (_lastStatus == KernelVirtualMemoryStatus.NotMapped) return "NotMapped";
        if (_lastStatus == KernelVirtualMemoryStatus.PhysicalAllocationFailed) return "PhysicalAllocationFailed";
        if (_lastStatus == KernelVirtualMemoryStatus.PageTableCapacityExhausted) return "PageTableCapacityExhausted";
        if (_lastStatus == KernelVirtualMemoryStatus.ArchitectureOperationFailed) return "ArchitectureOperationFailed";
        if (_lastStatus == KernelVirtualMemoryStatus.UnsupportedPageSize) return "UnsupportedPageSize";
        if (_lastStatus == KernelVirtualMemoryStatus.UnsupportedProtection) return "UnsupportedProtection";
        return "Unknown";
    }

    /// <summary>Gets the current x64 root page-table physical address.</summary>
    public static UInt64 GetRootPhysicalAddress() => _rootPhysicalAddress;

    /// <summary>Initializes virtual memory by attaching to and protecting the active x64 page-table hierarchy.</summary>
    /// <returns><see langword="true"/> when the active root and all reachable page-table pages were accepted.</returns>
    public static Boolean Initialize()
    {
        if (_initialized) return SetFailure(KernelVirtualMemoryStatus.AlreadyInitialized);
        if (!KernelPhysicalMemory.IsInitialized()) return SetFailure(KernelVirtualMemoryStatus.InvalidParameter);
        KernelPhysicalMemoryStatistics physicalStatistics = KernelPhysicalMemory.GetStatistics();
        if (physicalStatistics.LiveAllocationCount != 0) return SetFailure(KernelVirtualMemoryStatus.InvalidParameter);
        UInt64 root = Native.ReadPageTableRoot() & AddressMask4KiB;
        if (root == 0UL || (root & 0xFFFUL) != 0UL) return SetFailure(KernelVirtualMemoryStatus.ArchitectureOperationFailed);
        ResetState();
        _rootPhysicalAddress = root;
        if (!CaptureDirectMapPlan()) return false;
        _executeDisableEnabled = Native.EnableExecuteDisable();
        _page1GiBSupported = Native.Supports1GiBPages();
        if (!ProtectTableHierarchy(root, 4)) return false;
        _initialized = true;
        _lastStatus = KernelVirtualMemoryStatus.Success;
        return true;
    }

    /// <summary>Installs one physical-to-virtual leaf mapping into the active x64 address space.</summary>
    /// <returns><see langword="true"/> when the leaf was installed and its translation invalidated.</returns>
    public static Boolean TryMap(UInt64 virtualAddress, UInt64 physicalAddress, KernelVirtualPageSize pageSize, KernelVirtualMemoryProtection protection)
    {
        if (!_initialized) return SetFailure(KernelVirtualMemoryStatus.NotInitialized);
        UInt64 size = (UInt64)pageSize;
        if (!IsSupportedPageSize(size)) return SetFailure(KernelVirtualMemoryStatus.UnsupportedPageSize);
        if (pageSize == KernelVirtualPageSize.Page1GiB && !_page1GiBSupported) return SetFailure(KernelVirtualMemoryStatus.UnsupportedPageSize);
        if (!HasProtection(protection, KernelVirtualMemoryProtection.Execute) && !_executeDisableEnabled) return SetFailure(KernelVirtualMemoryStatus.UnsupportedProtection);
        if (!IsCanonical(virtualAddress)) return SetFailure(KernelVirtualMemoryStatus.NonCanonicalAddress);
        if ((virtualAddress & (size - 1UL)) != 0UL || (physicalAddress & (size - 1UL)) != 0UL)
            return SetFailure(KernelVirtualMemoryStatus.InvalidParameter);
        if ((physicalAddress & ~LeafAddressMask(pageSize)) != 0UL) return SetFailure(KernelVirtualMemoryStatus.InvalidParameter);
        if (!HasProtection(protection, KernelVirtualMemoryProtection.Read)) return SetFailure(KernelVirtualMemoryStatus.UnsupportedProtection);

        if (!IsTableWritableBeforeDirectMap(_rootPhysicalAddress)) return SetFailure(KernelVirtualMemoryStatus.ArchitectureOperationFailed);
        UInt64* pml4 = TablePointer(_rootPhysicalAddress);
        Int32 pml4Index = (Int32)((virtualAddress >> 39) & 0x1FFUL);
        Int32 pdptIndex = (Int32)((virtualAddress >> 30) & 0x1FFUL);
        Int32 pdIndex = (Int32)((virtualAddress >> 21) & 0x1FFUL);
        Int32 ptIndex = (Int32)((virtualAddress >> 12) & 0x1FFUL);

        if (!TryGetOrCreateChild(pml4, pml4Index, protection, out UInt64* pdpt)) return false;
        if (pageSize == KernelVirtualPageSize.Page1GiB)
            return TryInstallLeaf(pdpt, pdptIndex, virtualAddress, physicalAddress, pageSize, protection);
        if (IsLarge(pdpt[pdptIndex])) return SetFailure(KernelVirtualMemoryStatus.AlreadyMapped);
        if (!TryGetOrCreateChild(pdpt, pdptIndex, protection, out UInt64* pd)) return false;
        if (pageSize == KernelVirtualPageSize.Page2MiB)
            return TryInstallLeaf(pd, pdIndex, virtualAddress, physicalAddress, pageSize, protection);
        if (IsLarge(pd[pdIndex])) return SetFailure(KernelVirtualMemoryStatus.AlreadyMapped);
        if (!TryGetOrCreateChild(pd, pdIndex, protection, out UInt64* pt)) return false;
        return TryInstallLeaf(pt, ptIndex, virtualAddress, physicalAddress, pageSize, protection);
    }

    /// <summary>Removes the present leaf mapping covering one canonical virtual address.</summary>
    /// <returns><see langword="true"/> when a leaf was cleared and its translation invalidated.</returns>
    public static Boolean TryUnmap(UInt64 virtualAddress)
    {
        if (!_initialized) return SetFailure(KernelVirtualMemoryStatus.NotInitialized);
        if (!TryFindLeaf(virtualAddress, out UInt64* table, out Int32 index, out KernelVirtualPageSize pageSize)) return false;
        table[index] = 0UL;
        DecrementMapped(pageSize);
        if (!Native.InvalidatePage(virtualAddress)) return SetFailure(KernelVirtualMemoryStatus.ArchitectureOperationFailed);
        _lastStatus = KernelVirtualMemoryStatus.Success;
        return true;
    }

    /// <summary>Replaces access and cache protection on the present leaf mapping covering one virtual address.</summary>
    /// <returns><see langword="true"/> when the leaf was rewritten without changing its physical target.</returns>
    public static Boolean TryProtect(UInt64 virtualAddress, KernelVirtualMemoryProtection protection)
    {
        if (!_initialized) return SetFailure(KernelVirtualMemoryStatus.NotInitialized);
        if (!HasProtection(protection, KernelVirtualMemoryProtection.Read)) return SetFailure(KernelVirtualMemoryStatus.UnsupportedProtection);
        if (!HasProtection(protection, KernelVirtualMemoryProtection.Execute) && !_executeDisableEnabled) return SetFailure(KernelVirtualMemoryStatus.UnsupportedProtection);
        if (!TryFindLeaf(virtualAddress, out UInt64* table, out Int32 index, out KernelVirtualPageSize pageSize)) return false;
        UInt64 current = table[index];
        UInt64 physicalAddress = current & LeafAddressMask(pageSize);
        if (!TryEncodeLeaf(physicalAddress, pageSize, protection, out UInt64 replacement))
            return SetFailure(KernelVirtualMemoryStatus.InvalidParameter);
        table[index] = replacement;
        if (!Native.InvalidatePage(virtualAddress)) return SetFailure(KernelVirtualMemoryStatus.ArchitectureOperationFailed);
        _lastStatus = KernelVirtualMemoryStatus.Success;
        return true;
    }

    /// <summary>Resolves one canonical virtual byte address through the active x64 page tables.</summary>
    /// <returns><see langword="true"/> when a present leaf covers the queried address.</returns>
    public static Boolean TryTranslate(UInt64 virtualAddress, out KernelVirtualTranslation translation)
    {
        translation = default;
        if (KernelFaultInjection.ShouldInject(KernelFaultKind.PageFault,"virtual-memory",out _)) return SetFailure(KernelVirtualMemoryStatus.NotMapped);
        if (!_initialized) return SetFailure(KernelVirtualMemoryStatus.NotInitialized);
        if (!TryFindLeaf(virtualAddress, out UInt64* table, out Int32 index, out KernelVirtualPageSize pageSize)) return false;
        UInt64 entry = table[index];
        UInt64 size = (UInt64)pageSize;
        UInt64 physicalBase = entry & LeafAddressMask(pageSize);
        UInt64 physicalAddress = physicalBase + (virtualAddress & (size - 1UL));
        translation = new KernelVirtualTranslation(virtualAddress, physicalAddress, pageSize, DecodeProtection(entry));
        _lastStatus = KernelVirtualMemoryStatus.Success;
        return true;
    }

    /// <summary>Gets current inherited page-table protection and manager-created mapping accounting.</summary>
    public static KernelVirtualMemoryStatistics GetStatistics() => new((UInt64)_protectedTableCount, _createdPageTables, _mapped4KiB, _mapped2MiB, _mapped1GiB);

    private static Boolean TryGetOrCreateChild(UInt64* table, Int32 index, KernelVirtualMemoryProtection protection, out UInt64* child)
    {
        child = (UInt64*)0;
        UInt64 entry = table[index];
        if (IsPresent(entry))
        {
            if (IsLarge(entry)) return SetFailure(KernelVirtualMemoryStatus.AlreadyMapped);
            if (HasProtection(protection, KernelVirtualMemoryProtection.Write) && (entry & Writable) == 0UL)
                return SetFailure(KernelVirtualMemoryStatus.UnsupportedProtection);
            if (HasProtection(protection, KernelVirtualMemoryProtection.User) && (entry & User) == 0UL)
                return SetFailure(KernelVirtualMemoryStatus.UnsupportedProtection);
            if (HasProtection(protection, KernelVirtualMemoryProtection.Execute) && (entry & NoExecute) != 0UL)
                return SetFailure(KernelVirtualMemoryStatus.UnsupportedProtection);
            UInt64 childPhysicalAddress = entry & AddressMask4KiB;
            if (!IsTableWritableBeforeDirectMap(childPhysicalAddress))
                return SetFailure(KernelVirtualMemoryStatus.ArchitectureOperationFailed);
            child = TablePointer(childPhysicalAddress);
            return true;
        }

        if (!TryAllocatePageTable(out UInt64 createdPhysicalAddress))
            return SetFailure(KernelVirtualMemoryStatus.PhysicalAllocationFailed);
        UInt64* created = TablePointer(createdPhysicalAddress);
        for (Int32 i = 0; i < 512; i++) created[i] = 0UL;
        table[index] = (createdPhysicalAddress & AddressMask4KiB) | Present | Writable | User;
        _createdPageTables++;
        child = created;
        return true;
    }

    private static Boolean TryInstallLeaf(UInt64* table, Int32 index, UInt64 virtualAddress, UInt64 physicalAddress, KernelVirtualPageSize pageSize, KernelVirtualMemoryProtection protection)
    {
        if (IsPresent(table[index])) return SetFailure(KernelVirtualMemoryStatus.AlreadyMapped);
        if (!TryEncodeLeaf(physicalAddress, pageSize, protection, out UInt64 entry))
            return SetFailure(KernelVirtualMemoryStatus.InvalidParameter);
        table[index] = entry;
        IncrementMapped(pageSize);
        if (!Native.InvalidatePage(virtualAddress)) return SetFailure(KernelVirtualMemoryStatus.ArchitectureOperationFailed);
        _lastStatus = KernelVirtualMemoryStatus.Success;
        return true;
    }

    private static Boolean TryFindLeaf(UInt64 virtualAddress, out UInt64* table, out Int32 index, out KernelVirtualPageSize pageSize)
    {
        table = (UInt64*)0;
        index = 0;
        pageSize = KernelVirtualPageSize.Page4KiB;
        if (!IsCanonical(virtualAddress)) return SetFailure(KernelVirtualMemoryStatus.NonCanonicalAddress);
        UInt64* pml4 = TablePointer(_rootPhysicalAddress);
        UInt64 pml4Entry = pml4[(Int32)((virtualAddress >> 39) & 0x1FFUL)];
        if (!IsPresent(pml4Entry) || IsLarge(pml4Entry)) return SetFailure(KernelVirtualMemoryStatus.NotMapped);
        UInt64* pdpt = TablePointer(pml4Entry & AddressMask4KiB);
        Int32 pdptIndex = (Int32)((virtualAddress >> 30) & 0x1FFUL);
        UInt64 pdptEntry = pdpt[pdptIndex];
        if (!IsPresent(pdptEntry)) return SetFailure(KernelVirtualMemoryStatus.NotMapped);
        if (IsLarge(pdptEntry))
        {
            table = pdpt;
            index = pdptIndex;
            pageSize = KernelVirtualPageSize.Page1GiB;
            return true;
        }
        UInt64* pd = TablePointer(pdptEntry & AddressMask4KiB);
        Int32 pdIndex = (Int32)((virtualAddress >> 21) & 0x1FFUL);
        UInt64 pdEntry = pd[pdIndex];
        if (!IsPresent(pdEntry)) return SetFailure(KernelVirtualMemoryStatus.NotMapped);
        if (IsLarge(pdEntry))
        {
            table = pd;
            index = pdIndex;
            pageSize = KernelVirtualPageSize.Page2MiB;
            return true;
        }
        UInt64* pt = TablePointer(pdEntry & AddressMask4KiB);
        Int32 ptIndex = (Int32)((virtualAddress >> 12) & 0x1FFUL);
        if (!IsPresent(pt[ptIndex])) return SetFailure(KernelVirtualMemoryStatus.NotMapped);
        table = pt;
        index = ptIndex;
        pageSize = KernelVirtualPageSize.Page4KiB;
        return true;
    }

    private static Boolean ProtectTableHierarchy(UInt64 physicalAddress, Int32 level)
    {
        if (WasProtected(physicalAddress)) return true;
        if (_protectedTableCount >= MaximumProtectedTables) return SetFailure(KernelVirtualMemoryStatus.PageTableCapacityExhausted);
        if (!KernelPhysicalMemory.TryExcludePage(physicalAddress)) return SetFailure(KernelVirtualMemoryStatus.PhysicalAllocationFailed);
        fixed (UInt64* protectedTables = _state.ProtectedTables)
        {
            protectedTables[_protectedTableCount] = physicalAddress;
            _protectedTableCount++;
        }
        if (level <= 1) return true;
        UInt64* table = TablePointer(physicalAddress);
        for (Int32 index = 0; index < 512; index++)
        {
            UInt64 entry = table[index];
            if (!IsPresent(entry)) continue;
            if ((level == 3 || level == 2) && IsLarge(entry)) continue;
            UInt64 child = entry & AddressMask4KiB;
            if (child == 0UL) return SetFailure(KernelVirtualMemoryStatus.InvalidParameter);
            if (!ProtectTableHierarchy(child, level - 1)) return false;
        }
        return true;
    }

    private static Boolean WasProtected(UInt64 physicalAddress)
    {
        fixed (UInt64* protectedTables = _state.ProtectedTables)
        {
            for (Int32 index = 0; index < _protectedTableCount; index++)
                if (protectedTables[index] == physicalAddress) return true;
        }
        return false;
    }

    private static Boolean TryEncodeLeaf(UInt64 physicalAddress, KernelVirtualPageSize pageSize, KernelVirtualMemoryProtection protection, out UInt64 entry)
    {
        entry = 0UL;
        UInt64 mask = LeafAddressMask(pageSize);
        UInt64 size = (UInt64)pageSize;
        if (mask == 0UL || (physicalAddress & (size - 1UL)) != 0UL || (physicalAddress & ~mask) != 0UL) return false;
        UInt64 flags = Present;
        if (HasProtection(protection, KernelVirtualMemoryProtection.Write)) flags |= Writable;
        if (HasProtection(protection, KernelVirtualMemoryProtection.User)) flags |= User;
        if (HasProtection(protection, KernelVirtualMemoryProtection.Global)) flags |= Global;
        if (HasProtection(protection, KernelVirtualMemoryProtection.Device)) flags |= CacheDisable;
        if (HasProtection(protection, KernelVirtualMemoryProtection.WriteThrough)) flags |= WriteThrough;
        if (!HasProtection(protection, KernelVirtualMemoryProtection.Execute)) flags |= NoExecute;
        if (pageSize != KernelVirtualPageSize.Page4KiB) flags |= LargePage;
        entry = (physicalAddress & mask) | flags;
        return true;
    }

    private static KernelVirtualMemoryProtection DecodeProtection(UInt64 entry)
    {
        KernelVirtualMemoryProtection protection = KernelVirtualMemoryProtection.Read;
        if ((entry & Writable) != 0UL) protection = protection | KernelVirtualMemoryProtection.Write;
        if ((entry & User) != 0UL) protection = protection | KernelVirtualMemoryProtection.User;
        if ((entry & Global) != 0UL) protection = protection | KernelVirtualMemoryProtection.Global;
        if ((entry & CacheDisable) != 0UL) protection = protection | KernelVirtualMemoryProtection.Device;
        if ((entry & WriteThrough) != 0UL) protection = protection | KernelVirtualMemoryProtection.WriteThrough;
        if ((entry & NoExecute) == 0UL) protection = protection | KernelVirtualMemoryProtection.Execute;
        return protection;
    }

    private static UInt64 LeafAddressMask(KernelVirtualPageSize pageSize)
    {
        if (pageSize == KernelVirtualPageSize.Page4KiB) return AddressMask4KiB;
        if (pageSize == KernelVirtualPageSize.Page2MiB) return AddressMask2MiB;
        if (pageSize == KernelVirtualPageSize.Page1GiB) return AddressMask1GiB;
        return 0UL;
    }

    private static Boolean IsSupportedPageSize(UInt64 size) => size == 4096UL || size == 2097152UL || size == 1073741824UL;
    private static Boolean IsPresent(UInt64 entry) => (entry & Present) != 0UL;
    private static Boolean IsLarge(UInt64 entry) => (entry & LargePage) != 0UL;
    private static Boolean HasProtection(KernelVirtualMemoryProtection value, KernelVirtualMemoryProtection flag) => (((UInt64)value & (UInt64)flag) != 0UL);
    private static UInt64* TablePointer(UInt64 physicalAddress)
    {
        if (_directMapReady && !WasProtected(physicalAddress))
            return (UInt64*)(nuint)(_directMapBase + physicalAddress);
        return (UInt64*)(nuint)physicalAddress;
    }

    private static Boolean IsCanonical(UInt64 address)
    {
        UInt64 upper = address >> 48;
        Boolean high = ((address >> 47) & 1UL) != 0UL;
        return high ? upper == 0xFFFFUL : upper == 0UL;
    }

    private static Boolean IncrementMapped(KernelVirtualPageSize pageSize)
    {
        if (pageSize == KernelVirtualPageSize.Page4KiB) _mapped4KiB++;
        else if (pageSize == KernelVirtualPageSize.Page2MiB) _mapped2MiB++;
        else _mapped1GiB++;
        return true;
    }

    private static Boolean DecrementMapped(KernelVirtualPageSize pageSize)
    {
        if (pageSize == KernelVirtualPageSize.Page4KiB && _mapped4KiB != 0UL) _mapped4KiB--;
        else if (pageSize == KernelVirtualPageSize.Page2MiB && _mapped2MiB != 0UL) _mapped2MiB--;
        else if (pageSize == KernelVirtualPageSize.Page1GiB && _mapped1GiB != 0UL) _mapped1GiB--;
        return true;
    }

    private static Boolean ResetState()
    {
        _protectedTableCount = 0;
        _rootPhysicalAddress = 0UL;
        _createdPageTables = 0UL;
        _mapped4KiB = 0UL;
        _mapped2MiB = 0UL;
        _mapped1GiB = 0UL;
        _executeDisableEnabled = false;
        _page1GiBSupported = false;
        ResetDirectMapState();
        fixed (UInt64* protectedTables = _state.ProtectedTables)
        {
            for (Int32 index = 0; index < MaximumProtectedTables; index++) protectedTables[index] = 0UL;
        }
        return true;
    }

    private static Boolean SetFailure(KernelVirtualMemoryStatus status)
    {
        _lastStatus = status;
        return false;
    }
}
