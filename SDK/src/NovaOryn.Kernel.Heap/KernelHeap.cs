using System;
using NovaOryn.Kernel.Contracts;
using NovaOryn.Kernel.AddressSpace;
using NovaOryn.Kernel.Memory;
using NovaOryn.Kernel.VirtualMemory;

namespace NovaOryn.Kernel.Heap;

/// <summary>Reports the most recent freestanding kernel-heap operation.</summary>
public enum KernelHeapStatus
{
    /// <summary>The operation completed successfully.</summary>
    Success = 0,
    /// <summary>One or more prerequisite memory layers are not initialized.</summary>
    DependencyNotInitialized = 1,
    /// <summary>The heap was already initialized.</summary>
    AlreadyInitialized = 2,
    /// <summary>The request has an invalid size or alignment.</summary>
    InvalidParameter = 3,
    /// <summary>No physical or virtual capacity can satisfy the request.</summary>
    OutOfMemory = 4,
    /// <summary>The bounded block table has no free metadata record.</summary>
    MetadataCapacityExhausted = 5,
    /// <summary>Backing pages could not be mapped into the kernel heap reservation.</summary>
    MappingFailed = 6,
    /// <summary>The supplied allocation token/range is unknown or was already released.</summary>
    AllocationNotFound = 7,
    /// <summary>The supplied token was already released.</summary>
    DoubleFreeDetected = 8,
    /// <summary>A guarded allocation canary was modified.</summary>
    GuardCorruptionDetected = 9
}

/// <summary>Identifies one live first-fit kernel-heap allocation.</summary>
public readonly struct KernelHeapAllocation
{
    internal KernelHeapAllocation(UInt64 token, UInt64 address, UInt64 byteCount)
    {
        Token = token;
        Address = address;
        ByteCount = byteCount;
    }

    /// <summary>Gets the opaque allocation token.</summary>
    public UInt64 Token { get; }
    /// <summary>Gets the first usable virtual byte.</summary>
    public UInt64 Address { get; }
    /// <summary>Gets the usable allocation byte count.</summary>
    public UInt64 ByteCount { get; }
}

/// <summary>Provides current page-backed heap accounting.</summary>
public readonly struct KernelHeapStatistics
{
    internal KernelHeapStatistics(UInt64 committed, UInt64 allocated, UInt64 free, UInt64 peak, Int32 live, Int32 freeBlocks)
    {
        CommittedBytes = committed;
        AllocatedBytes = allocated;
        FreeBytes = free;
        PeakAllocatedBytes = peak;
        LiveAllocations = live;
        FreeBlocks = freeBlocks;
    }

    /// <summary>Gets bytes backed by physical pages.</summary>
    public UInt64 CommittedBytes { get; }
    /// <summary>Gets bytes owned by live allocations.</summary>
    public UInt64 AllocatedBytes { get; }
    /// <summary>Gets committed reusable bytes.</summary>
    public UInt64 FreeBytes { get; }
    /// <summary>Gets peak simultaneously allocated bytes.</summary>
    public UInt64 PeakAllocatedBytes { get; }
    /// <summary>Gets the live allocation count.</summary>
    public Int32 LiveAllocations { get; }
    /// <summary>Gets the reusable free-block count.</summary>
    public Int32 FreeBlocks { get; }
}

/// <summary>Provides the default first-fit, page-backed, non-executable kernel heap inside the standard heap reservation.</summary>
public static unsafe partial class KernelHeap
{
    private const UInt64 PageSize = 4096UL;
    private const UInt64 GrowthPages = 16UL;
    private const Int32 MaximumBlocks = 512;

    private unsafe struct State
    {
        internal fixed UInt64 Starts[MaximumBlocks];
        internal fixed UInt64 Lengths[MaximumBlocks];
        internal fixed UInt64 Tokens[MaximumBlocks];
        internal fixed Byte States[MaximumBlocks];
    }

    /// <summary>Gets the fixed virtual address of the debugger-readable kernel-heap diagnostic metadata area.</summary>
    public const UInt64 DiagnosticMetadataAddress = KernelAddressSpace.KernelHeapBase + KernelAddressSpace.KernelHeapLength - 0x4000UL;
    /// <summary>Gets the byte length reserved for the debugger-readable heap metadata area.</summary>
    public const UInt64 DiagnosticMetadataLength = 0x4000UL;
    /// <summary>Gets the current heap diagnostic ABI version.</summary>
    public const UInt32 DiagnosticMetadataVersion = 1U;

    private const UInt64 DiagnosticMagic = 0x4E4F484541503031UL;
    private const UInt64 DiagnosticStateOffset = 64UL;
    private const UInt64 AllocatableHeapLength = KernelAddressSpace.KernelHeapLength - DiagnosticMetadataLength;
    private static Boolean _diagnosticMetadataReady;
    private static UInt64 _committed;
    private static UInt64 _allocated;
    private static UInt64 _peak;
    private static UInt64 _nextToken = 1UL;
    private static Int32 _live;
    private static Boolean _initialized;
    private static KernelHeapStatus _status;

    /// <summary>Gets whether the kernel heap is initialized.</summary>
    public static Boolean IsInitialized() => _initialized;

    /// <summary>Gets the most recent heap status.</summary>
    public static KernelHeapStatus GetLastStatus() => _status;

    /// <summary>Gets a freestanding-safe symbolic status name.</summary>
    /// <returns>A stable status string.</returns>
    public static String GetLastStatusName()
    {
        if (_status == KernelHeapStatus.Success) return "Success";
        if (_status == KernelHeapStatus.DependencyNotInitialized) return "DependencyNotInitialized";
        if (_status == KernelHeapStatus.AlreadyInitialized) return "AlreadyInitialized";
        if (_status == KernelHeapStatus.InvalidParameter) return "InvalidParameter";
        if (_status == KernelHeapStatus.OutOfMemory) return "OutOfMemory";
        if (_status == KernelHeapStatus.MetadataCapacityExhausted) return "MetadataCapacityExhausted";
        if (_status == KernelHeapStatus.MappingFailed) return "MappingFailed";
        if (_status == KernelHeapStatus.AllocationNotFound) return "AllocationNotFound";
        if (_status == KernelHeapStatus.DoubleFreeDetected) return "DoubleFreeDetected";
        if (_status == KernelHeapStatus.GuardCorruptionDetected) return "GuardCorruptionDetected";
        return "Unknown";
    }

    /// <summary>Commits the first 64 KiB of the standard kernel-heap reservation.</summary>
    /// <returns><see langword="true"/> when dependencies and initial mappings are ready.</returns>
    public static Boolean Initialize()
    {
        if (_initialized)
        {
            _status = KernelHeapStatus.AlreadyInitialized;
            return false;
        }
        if (!KernelAddressSpace.IsInitialized() || !KernelVirtualMemory.IsInitialized() || !KernelPhysicalMemory.IsInitialized())
        {
            _status = KernelHeapStatus.DependencyNotInitialized;
            return false;
        }
        if (!InitializeDiagnosticMetadata()) return false;
        Reset();
        if (!Grow(GrowthPages)) return false;
        _initialized = true;
        _status = KernelHeapStatus.Success;
        SynchronizeDiagnosticHeader();
        return true;
    }

    /// <summary>Allocates raw virtual bytes using address-ordered first fit and optional zero filling.</summary>
    /// <returns><see langword="true"/> when a live allocation was created.</returns>
    public static Boolean TryAllocate(UInt64 byteCount, UInt64 alignment, Boolean zeroFill, out KernelHeapAllocation allocation)
    {
        allocation = default;
        if (KernelFaultInjection.ShouldInject(KernelFaultKind.AllocationFailure,"heap",out _))
        {
            _status = KernelHeapStatus.OutOfMemory;
            return false;
        }
        if (!_initialized)
        {
            _status = KernelHeapStatus.DependencyNotInitialized;
            return false;
        }
        if (byteCount == 0UL || alignment == 0UL || (alignment & (alignment - 1UL)) != 0UL || alignment > PageSize)
        {
            _status = KernelHeapStatus.InvalidParameter;
            return false;
        }
        if (byteCount > 0xFFFFFFFFFFFFFFFFUL - (alignment - 1UL))
        {
            _status = KernelHeapStatus.InvalidParameter;
            return false;
        }

        for (Int32 attempt = 0; attempt < 2; attempt++)
        {
            if (TryAllocateExisting(byteCount, alignment, zeroFill, out allocation)) return true;
            UInt64 required = byteCount + alignment - 1UL;
            if (required > 0xFFFFFFFFFFFFFFFFUL - (PageSize - 1UL))
            {
                _status = KernelHeapStatus.InvalidParameter;
                return false;
            }
            UInt64 pages = (required + PageSize - 1UL) / PageSize;
            if (pages < GrowthPages) pages = GrowthPages;
            if (!Grow(pages)) return false;
        }
        _status = KernelHeapStatus.OutOfMemory;
        return false;
    }

    /// <summary>Releases one exact live allocation and coalesces adjacent free blocks.</summary>
    /// <returns><see langword="true"/> when the allocation was live.</returns>
    public static Boolean TryRelease(KernelHeapAllocation allocation)
    {
        if (!_initialized)
        {
            _status = KernelHeapStatus.DependencyNotInitialized;
            return false;
        }
        State* state = GetState();
        UInt64* starts = state->Starts;
        UInt64* lengths = state->Lengths;
        UInt64* tokens = state->Tokens;
        Byte* states = state->States;
        {
            for (Int32 i = 0; i < MaximumBlocks; i++)
            {
                if (states[i] != 2 || tokens[i] != allocation.Token || starts[i] != allocation.Address || lengths[i] != allocation.ByteCount) continue;
                UInt64 releasedToken = tokens[i];
                states[i] = 1;
                tokens[i] = 0UL;
                _allocated -= lengths[i];
                _live--;
                OnAllocationReleased(releasedToken);
                Coalesce();
                _status = KernelHeapStatus.Success;
                SynchronizeDiagnosticHeader();
                return true;
            }
        }
        if (WasReleasedToken(allocation.Token))
        {
            _doubleFreeFailures++;
            _status = KernelHeapStatus.DoubleFreeDetected;
            return false;
        }
        _status = KernelHeapStatus.AllocationNotFound;
        return false;
    }

    /// <summary>Gets current heap accounting.</summary>
    /// <returns>An immutable statistics snapshot.</returns>
    public static KernelHeapStatistics GetStatistics()
    {
        if (!_diagnosticMetadataReady) return new KernelHeapStatistics(0UL, 0UL, 0UL, 0UL, 0, 0);
        UInt64 free = 0UL;
        Int32 blocks = 0;
        State* state = GetState();
        UInt64* lengths = state->Lengths;
        Byte* states = state->States;
        {
            for (Int32 i = 0; i < MaximumBlocks; i++)
            {
                if (states[i] != 1) continue;
                free += lengths[i];
                blocks++;
            }
        }
        return new KernelHeapStatistics(_committed, _allocated, free, _peak, _live, blocks);
    }

    private static Boolean TryAllocateExisting(UInt64 bytes, UInt64 alignment, Boolean zeroFill, out KernelHeapAllocation allocation)
    {
        allocation = default;
        State* state = GetState();
        UInt64* starts = state->Starts;
        UInt64* lengths = state->Lengths;
        UInt64* tokens = state->Tokens;
        Byte* states = state->States;
        {
            for (Int32 i = 0; i < MaximumBlocks; i++)
            {
                if (states[i] != 1) continue;
                UInt64 start = starts[i];
                UInt64 length = lengths[i];
                UInt64 mask = alignment - 1UL;
                if (start > 0xFFFFFFFFFFFFFFFFUL - mask) continue;
                UInt64 aligned = (start + mask) & ~mask;
                UInt64 prefix = aligned - start;
                if (prefix > length || bytes > length - prefix) continue;
                UInt64 suffix = length - prefix - bytes;
                Int32 firstSlot = prefix > 0UL ? FindUnused(i, -1) : -1;
                Int32 secondSlot = suffix > 0UL ? FindUnused(i, firstSlot) : -1;
                if ((prefix > 0UL && firstSlot < 0) || (suffix > 0UL && secondSlot < 0))
                {
                    _status = KernelHeapStatus.MetadataCapacityExhausted;
                    return false;
                }

                UInt64 token = NextToken();
                states[i] = 2;
                starts[i] = aligned;
                lengths[i] = bytes;
                tokens[i] = token;
                if (prefix > 0UL)
                {
                    states[firstSlot] = 1;
                    starts[firstSlot] = start;
                    lengths[firstSlot] = prefix;
                }
                if (suffix > 0UL)
                {
                    states[secondSlot] = 1;
                    starts[secondSlot] = aligned + bytes;
                    lengths[secondSlot] = suffix;
                }
                _allocated += bytes;
                if (_allocated > _peak) _peak = _allocated;
                _live++;
                if (zeroFill)
                {
                    Byte* pointer = (Byte*)(nuint)aligned;
                    for (UInt64 n = 0UL; n < bytes; n++) pointer[n] = 0;
                }
                allocation = new KernelHeapAllocation(token, aligned, bytes);
                OnAllocationCreated(token, aligned, bytes);
                _status = KernelHeapStatus.Success;
                SynchronizeDiagnosticHeader();
                return true;
            }
        }
        return false;
    }

    private static Boolean Grow(UInt64 pages)
    {
        if (pages == 0UL || pages > 0xFFFFFFFFFFFFFFFFUL / PageSize)
        {
            _status = KernelHeapStatus.InvalidParameter;
            return false;
        }
        UInt64 bytes = pages * PageSize;
        if (_committed > AllocatableHeapLength || bytes > AllocatableHeapLength - _committed)
        {
            _status = KernelHeapStatus.OutOfMemory;
            return false;
        }
        Int32 slot = FindUnused(-1, -1);
        if (slot < 0)
        {
            _status = KernelHeapStatus.MetadataCapacityExhausted;
            return false;
        }
        if (!KernelPhysicalMemory.TryAllocate(pages, 1UL, out KernelPhysicalAllocation physical))
        {
            _status = KernelHeapStatus.OutOfMemory;
            return false;
        }

        UInt64 mapped = 0UL;
        KernelVirtualMemoryProtection protection = KernelVirtualMemoryProtection.Read | KernelVirtualMemoryProtection.Write | KernelVirtualMemoryProtection.Global;
        for (UInt64 page = 0UL; page < pages; page++)
        {
            UInt64 virtualAddress = KernelAddressSpace.KernelHeapBase + _committed + page * PageSize;
            UInt64 physicalAddress = physical.StartAddress + page * PageSize;
            if (!KernelVirtualMemory.TryMap(virtualAddress, physicalAddress, KernelVirtualPageSize.Page4KiB, protection))
            {
                for (UInt64 undo = 0UL; undo < mapped; undo++)
                    KernelVirtualMemory.TryUnmap(KernelAddressSpace.KernelHeapBase + _committed + undo * PageSize);
                KernelPhysicalMemory.TryRelease(physical);
                _status = KernelHeapStatus.MappingFailed;
                return false;
            }
            mapped++;
        }

        State* state = GetState();
        UInt64* starts = state->Starts;
        UInt64* lengths = state->Lengths;
        Byte* states = state->States;
        {
            states[slot] = 1;
            starts[slot] = KernelAddressSpace.KernelHeapBase + _committed;
            lengths[slot] = bytes;
        }
        _committed += bytes;
        Coalesce();
        SynchronizeDiagnosticHeader();
        return true;
    }

    private static Int32 FindUnused(Int32 excluded, Int32 excluded2)
    {
        State* state = GetState();
        Byte* states = state->States;
        {
            for (Int32 i = 0; i < MaximumBlocks; i++)
                if (i != excluded && i != excluded2 && states[i] == 0) return i;
        }
        return -1;
    }

    private static void Coalesce()
    {
        Boolean changed = true;
        State* state = GetState();
        UInt64* starts = state->Starts;
        UInt64* lengths = state->Lengths;
        Byte* states = state->States;
        {
            while (changed)
            {
                changed = false;
                for (Int32 i = 0; i < MaximumBlocks && !changed; i++)
                {
                    if (states[i] != 1) continue;
                    for (Int32 j = 0; j < MaximumBlocks; j++)
                    {
                        if (i == j || states[j] != 1) continue;
                        if (starts[i] > 0xFFFFFFFFFFFFFFFFUL - lengths[i]) continue;
                        if (starts[i] + lengths[i] != starts[j]) continue;
                        lengths[i] += lengths[j];
                        states[j] = 0;
                        starts[j] = 0UL;
                        lengths[j] = 0UL;
                        changed = true;
                        break;
                    }
                }
            }
        }
    }

    private static UInt64 NextToken()
    {
        UInt64 token = _nextToken++;
        if (token == 0UL) token = _nextToken++;
        return token;
    }

    private static void Reset()
    {
        _committed = 0UL;
        _allocated = 0UL;
        _peak = 0UL;
        _live = 0;
        _nextToken = 1UL;
        ResetExtendedDiagnostics();
        State* state = GetState();
        UInt64* starts = state->Starts;
        UInt64* lengths = state->Lengths;
        UInt64* tokens = state->Tokens;
        Byte* states = state->States;
        for (Int32 i = 0; i < MaximumBlocks; i++)
        {
            starts[i] = 0UL;
            lengths[i] = 0UL;
            tokens[i] = 0UL;
            states[i] = 0;
        }
        SynchronizeDiagnosticHeader();
    }

    private static State* GetState() => (State*)(nuint)(DiagnosticMetadataAddress + DiagnosticStateOffset);

    private static Boolean InitializeDiagnosticMetadata()
    {
        if (_diagnosticMetadataReady) return true;
        UInt64 pages = DiagnosticMetadataLength / PageSize;
        if (!KernelPhysicalMemory.TryAllocate(pages, 1UL, out KernelPhysicalAllocation physical))
        {
            _status = KernelHeapStatus.OutOfMemory;
            return false;
        }
        UInt64 mapped = 0UL;
        KernelVirtualMemoryProtection protection = KernelVirtualMemoryProtection.Read | KernelVirtualMemoryProtection.Write | KernelVirtualMemoryProtection.Global;
        for (UInt64 page = 0UL; page < pages; page++)
        {
            UInt64 virtualAddress = DiagnosticMetadataAddress + page * PageSize;
            UInt64 physicalAddress = physical.StartAddress + page * PageSize;
            if (!KernelVirtualMemory.TryMap(virtualAddress, physicalAddress, KernelVirtualPageSize.Page4KiB, protection))
            {
                for (UInt64 undo = 0UL; undo < mapped; undo++) KernelVirtualMemory.TryUnmap(DiagnosticMetadataAddress + undo * PageSize);
                KernelPhysicalMemory.TryRelease(physical);
                _status = KernelHeapStatus.MappingFailed;
                return false;
            }
            mapped++;
        }
        Byte* bytes = (Byte*)(nuint)DiagnosticMetadataAddress;
        for (UInt64 i = 0UL; i < DiagnosticMetadataLength; i++) bytes[i] = 0;
        _diagnosticMetadataReady = true;
        SynchronizeDiagnosticHeader();
        return true;
    }

    private static void SynchronizeDiagnosticHeader()
    {
        if (!_diagnosticMetadataReady) return;
        UInt64* qwords = (UInt64*)(nuint)DiagnosticMetadataAddress;
        UInt32* dwords = (UInt32*)(nuint)DiagnosticMetadataAddress;
        Byte* bytes = (Byte*)(nuint)DiagnosticMetadataAddress;
        qwords[0] = DiagnosticMagic;
        dwords[2] = DiagnosticMetadataVersion;
        dwords[3] = MaximumBlocks;
        qwords[2] = _committed;
        qwords[3] = _allocated;
        qwords[4] = _peak;
        qwords[5] = _nextToken;
        dwords[12] = (UInt32)_live;
        dwords[13] = (UInt32)_status;
        bytes[56] = _initialized ? (Byte)1 : (Byte)0;
    }
}
