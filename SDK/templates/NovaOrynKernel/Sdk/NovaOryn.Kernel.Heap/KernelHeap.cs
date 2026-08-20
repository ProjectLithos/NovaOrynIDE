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
    AllocationNotFound = 7
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
public static unsafe class KernelHeap
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

#pragma warning disable CS0169 // Fixed-buffer member access is not counted as use of the containing freestanding state field.
    private static State _state;
#pragma warning restore CS0169
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
        Reset();
        if (!Grow(GrowthPages)) return false;
        _initialized = true;
        _status = KernelHeapStatus.Success;
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
        fixed (UInt64* starts = _state.Starts)
        fixed (UInt64* lengths = _state.Lengths)
        fixed (UInt64* tokens = _state.Tokens)
        fixed (Byte* states = _state.States)
        {
            for (Int32 i = 0; i < MaximumBlocks; i++)
            {
                if (states[i] != 2 || tokens[i] != allocation.Token || starts[i] != allocation.Address || lengths[i] != allocation.ByteCount) continue;
                states[i] = 1;
                tokens[i] = 0UL;
                _allocated -= lengths[i];
                _live--;
                Coalesce();
                _status = KernelHeapStatus.Success;
                return true;
            }
        }
        _status = KernelHeapStatus.AllocationNotFound;
        return false;
    }

    /// <summary>Gets current heap accounting.</summary>
    /// <returns>An immutable statistics snapshot.</returns>
    public static KernelHeapStatistics GetStatistics()
    {
        UInt64 free = 0UL;
        Int32 blocks = 0;
        fixed (UInt64* lengths = _state.Lengths)
        fixed (Byte* states = _state.States)
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
        fixed (UInt64* starts = _state.Starts)
        fixed (UInt64* lengths = _state.Lengths)
        fixed (UInt64* tokens = _state.Tokens)
        fixed (Byte* states = _state.States)
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
                _status = KernelHeapStatus.Success;
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
        if (_committed > KernelAddressSpace.KernelHeapLength || bytes > KernelAddressSpace.KernelHeapLength - _committed)
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

        fixed (UInt64* starts = _state.Starts)
        fixed (UInt64* lengths = _state.Lengths)
        fixed (Byte* states = _state.States)
        {
            states[slot] = 1;
            starts[slot] = KernelAddressSpace.KernelHeapBase + _committed;
            lengths[slot] = bytes;
        }
        _committed += bytes;
        Coalesce();
        return true;
    }

    private static Int32 FindUnused(Int32 excluded, Int32 excluded2)
    {
        fixed (Byte* states = _state.States)
        {
            for (Int32 i = 0; i < MaximumBlocks; i++)
                if (i != excluded && i != excluded2 && states[i] == 0) return i;
        }
        return -1;
    }

    private static void Coalesce()
    {
        Boolean changed = true;
        fixed (UInt64* starts = _state.Starts)
        fixed (UInt64* lengths = _state.Lengths)
        fixed (Byte* states = _state.States)
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
        fixed (UInt64* starts = _state.Starts)
        fixed (UInt64* lengths = _state.Lengths)
        fixed (UInt64* tokens = _state.Tokens)
        fixed (Byte* states = _state.States)
        {
            for (Int32 i = 0; i < MaximumBlocks; i++)
            {
                starts[i] = 0UL;
                lengths[i] = 0UL;
                tokens[i] = 0UL;
                states[i] = 0;
            }
        }
    }
}
