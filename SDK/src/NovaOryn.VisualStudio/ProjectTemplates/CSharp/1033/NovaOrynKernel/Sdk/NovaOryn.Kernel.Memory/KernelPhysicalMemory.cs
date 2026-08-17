using System;
using NovaOryn.Kernel.Console;

namespace NovaOryn.Kernel.Memory;

/// <summary>Reports the result of a freestanding physical-memory operation.</summary>
public enum KernelPhysicalMemoryStatus
{
    /// <summary>The operation completed successfully.</summary>
    Success = 0,
    /// <summary>The supplied boot map or allocation request was invalid.</summary>
    InvalidParameter = 1,
    /// <summary>The physical-memory manager has not been initialized.</summary>
    NotInitialized = 2,
    /// <summary>The physical-memory manager was already initialized.</summary>
    AlreadyInitialized = 3,
    /// <summary>The fixed early-boot extent table cannot represent another free range.</summary>
    ExtentCapacityExhausted = 4,
    /// <summary>The bounded live-allocation table is full.</summary>
    AllocationCapacityExhausted = 5,
    /// <summary>No free physical extent can satisfy the request.</summary>
    OutOfMemory = 6,
    /// <summary>The allocation token is unknown or has already been released.</summary>
    AllocationNotFound = 7
}

/// <summary>Identifies one live contiguous physical-frame allocation.</summary>
public readonly struct KernelPhysicalAllocation
{
    /// <summary>Creates an allocation descriptor whose values are validated by release operations.</summary>
    public KernelPhysicalAllocation(UInt64 token, UInt64 startAddress, UInt64 pageCount)
    {
        Token = token;
        StartAddress = startAddress;
        PageCount = pageCount;
    }

    /// <summary>Gets the opaque allocation token.</summary>
    public UInt64 Token { get; }
    /// <summary>Gets the physical address of the first allocated 4 KiB frame.</summary>
    public UInt64 StartAddress { get; }
    /// <summary>Gets the number of contiguous allocated frames.</summary>
    public UInt64 PageCount { get; }
}

/// <summary>Provides an immutable snapshot of early physical-memory accounting.</summary>
public readonly struct KernelPhysicalMemoryStatistics
{
    internal KernelPhysicalMemoryStatistics(UInt64 managedPages, UInt64 freePages, UInt64 allocatedPages, UInt64 largestFreeExtentPages, Int32 freeExtentCount, Int32 liveAllocationCount)
    {
        ManagedPages = managedPages;
        FreePages = freePages;
        AllocatedPages = allocatedPages;
        LargestFreeExtentPages = largestFreeExtentPages;
        FreeExtentCount = freeExtentCount;
        LiveAllocationCount = liveAllocationCount;
    }

    /// <summary>Gets the number of immediately allocatable pages discovered at boot.</summary>
    public UInt64 ManagedPages { get; }
    /// <summary>Gets the number of currently free managed pages.</summary>
    public UInt64 FreePages { get; }
    /// <summary>Gets the number of pages currently owned by live allocations.</summary>
    public UInt64 AllocatedPages { get; }
    /// <summary>Gets the number of managed pages permanently excluded from ordinary allocation.</summary>
    public UInt64 ReservedPages => ManagedPages - FreePages - AllocatedPages;
    /// <summary>Gets the largest currently free contiguous extent in pages.</summary>
    public UInt64 LargestFreeExtentPages { get; }
    /// <summary>Gets the number of current free extents.</summary>
    public Int32 FreeExtentCount { get; }
    /// <summary>Gets the number of live allocations.</summary>
    public Int32 LiveAllocationCount { get; }
}

/// <summary>
/// Provides the default no-heap physical-frame manager used by the editable freestanding kernel template.
/// </summary>
/// <remarks>
/// The early manager consumes only UEFI ConventionalMemory. BootServicesCode and BootServicesData remain
/// deferred even after ExitBootServices because firmware-provided bootstrap state, including the inherited
/// stack, can still occupy those pages. Loader, runtime, ACPI, MMIO, framebuffer and defective ranges also
/// remain unavailable. Metadata lives in fixed kernel storage, so initialization never depends on a heap.
/// </remarks>
public static unsafe partial class KernelPhysicalMemory
{
    private const UInt64 PageSize = 4096UL;
    private const Int32 MaximumExtents = 512;
    private const Int32 MaximumAllocations = 256;
    private const UInt32 UefiConventionalMemory = 7U;
    private const UInt64 UefiRuntimeAttribute = 0x8000000000000000UL;

    private unsafe struct State
    {
        internal fixed UInt64 ExtentStarts[MaximumExtents];
        internal fixed UInt64 ExtentPages[MaximumExtents];
        internal fixed UInt64 AllocationTokens[MaximumAllocations];
        internal fixed UInt64 AllocationStarts[MaximumAllocations];
        internal fixed UInt64 AllocationPages[MaximumAllocations];
        internal fixed Byte AllocationActive[MaximumAllocations];
    }

    #pragma warning disable CS0169 // Roslyn does not count fixed-buffer member access as use of the containing freestanding state field.
    private static State _state;
    #pragma warning restore CS0169
    private static Int32 _extentCount;
    private static Int32 _liveAllocationCount;
    private static UInt64 _managedPages;
    private static UInt64 _freePages;
    private static UInt64 _allocatedPages;
    private static UInt64 _nextToken;
    private static UInt64 _bootstrapWorkspaceAddress;
    private static UInt64 _bootstrapWorkspacePages;
    private static UInt64 _bootstrapWorkspaceUsedPages;
    private static Boolean _initialized;
    private static KernelPhysicalMemoryStatus _lastStatus;

    /// <summary>Gets whether the default early physical-memory manager is initialized.</summary>
    public static Boolean IsInitialized() => _initialized;

    /// <summary>Gets the status produced by the most recent initialization/allocation/release operation.</summary>
    public static KernelPhysicalMemoryStatus GetLastStatus() => _lastStatus;

    /// <summary>Initializes the default extent manager directly from the retained final UEFI memory map.</summary>
    /// <returns><see langword="true"/> when every immediately allocatable firmware range was accepted.</returns>
    public static Boolean Initialize(BootContext boot)
    {
        if (_initialized) return SetFailure(KernelPhysicalMemoryStatus.AlreadyInitialized);
        if (!boot.HasFinalMemoryMap()) return SetFailure(KernelPhysicalMemoryStatus.InvalidParameter);

        UInt64 mapAddress = boot.GetFinalMemoryMapAddress();
        UInt64 mapLength = boot.GetFinalMemoryMapLength();
        UInt64 descriptorSize = boot.GetFinalMemoryDescriptorSize();
        if (mapAddress == 0UL || mapLength == 0UL || descriptorSize < 40UL || mapLength % descriptorSize != 0UL)
            return SetFailure(KernelPhysicalMemoryStatus.InvalidParameter);

        UInt64 descriptorCount = mapLength / descriptorSize;
        if (descriptorCount == 0UL || descriptorCount > 0x7FFFFFFFUL)
            return SetFailure(KernelPhysicalMemoryStatus.InvalidParameter);

        UInt64 bootstrapAddress = boot.GetBootstrapPageTableWorkspaceAddress();
        UInt64 bootstrapPages = boot.GetBootstrapPageTableWorkspacePages();
        if (bootstrapAddress == 0UL || bootstrapPages == 0UL || (bootstrapAddress & 0xFFFUL) != 0UL ||
            bootstrapPages > 0xFFFFFFFFFFFFFFFFUL / PageSize || bootstrapAddress > 0xFFFFFFFFFFFFFFFFUL - bootstrapPages * PageSize)
            return SetFailure(KernelPhysicalMemoryStatus.InvalidParameter);

        ResetState();
        _bootstrapWorkspaceAddress = bootstrapAddress;
        _bootstrapWorkspacePages = bootstrapPages;
        for (UInt64 index = 0; index < descriptorCount; index++)
        {
            UInt64 offset = index * descriptorSize;
            if (mapAddress > 0xFFFFFFFFFFFFFFFFUL - offset) return SetFailure(KernelPhysicalMemoryStatus.InvalidParameter);
            Byte* descriptor = (Byte*)(nuint)(mapAddress + offset);
            UInt32 type = *(UInt32*)(descriptor + 0);
            UInt64 start = *(UInt64*)(descriptor + 8);
            UInt64 pages = *(UInt64*)(descriptor + 24);
            UInt64 attributes = *(UInt64*)(descriptor + 32);
            if (pages == 0UL || (start & 0xFFFUL) != 0UL || pages > 0xFFFFFFFFFFFFFFFFUL / PageSize)
                return SetFailure(KernelPhysicalMemoryStatus.InvalidParameter);
            if ((attributes & UefiRuntimeAttribute) != 0UL) continue;
            if (type != UefiConventionalMemory) continue;
            if (!InsertFreeExtent(start / PageSize, pages)) return SetFailure(KernelPhysicalMemoryStatus.ExtentCapacityExhausted);
            if (_managedPages > 0xFFFFFFFFFFFFFFFFUL - pages) return SetFailure(KernelPhysicalMemoryStatus.InvalidParameter);
            _managedPages += pages;
            _freePages += pages;
        }

        if (_managedPages == 0UL) return SetFailure(KernelPhysicalMemoryStatus.InvalidParameter);
        _initialized = true;
        _lastStatus = KernelPhysicalMemoryStatus.Success;
        return true;
    }

    /// <summary>Allocates contiguous 4 KiB physical frames using first-fit extent selection.</summary>
    /// <param name="pageCount">Number of contiguous pages required.</param>
    /// <param name="alignmentPages">Power-of-two page alignment; use one for ordinary page alignment.</param>
    /// <param name="allocation">Receives the live allocation token and physical range.</param>
    /// <returns><see langword="true"/> when a matching free extent was split successfully.</returns>
    public static Boolean TryAllocate(UInt64 pageCount, UInt64 alignmentPages, out KernelPhysicalAllocation allocation)
    {
        allocation = default;
        if (!_initialized) return SetFailure(KernelPhysicalMemoryStatus.NotInitialized);
        if (pageCount == 0UL || alignmentPages == 0UL || (alignmentPages & (alignmentPages - 1UL)) != 0UL)
            return SetFailure(KernelPhysicalMemoryStatus.InvalidParameter);
        Int32 record = FindFreeAllocationRecord();
        if (record < 0) return SetFailure(KernelPhysicalMemoryStatus.AllocationCapacityExhausted);

        fixed (UInt64* starts = _state.ExtentStarts)
        fixed (UInt64* pages = _state.ExtentPages)
        fixed (UInt64* tokens = _state.AllocationTokens)
        fixed (UInt64* allocationStarts = _state.AllocationStarts)
        fixed (UInt64* allocationPages = _state.AllocationPages)
        fixed (Byte* active = _state.AllocationActive)
        {
            for (Int32 index = 0; index < _extentCount; index++)
            {
                UInt64 extentStart = starts[index];
                UInt64 extentPages = pages[index];
                if (!TryAlignFrame(extentStart, alignmentPages, out UInt64 candidate)) continue;
                if (candidate < extentStart) continue;
                UInt64 prefix = candidate - extentStart;
                if (prefix > extentPages || pageCount > extentPages - prefix) continue;

                UInt64 suffix = extentPages - prefix - pageCount;
                if (prefix != 0UL && suffix != 0UL && _extentCount >= MaximumExtents)
                    return SetFailure(KernelPhysicalMemoryStatus.ExtentCapacityExhausted);
                ReplaceAllocatedExtent(index, extentStart, prefix, candidate + pageCount, suffix);

                UInt64 token = NextToken();
                tokens[record] = token;
                allocationStarts[record] = candidate;
                allocationPages[record] = pageCount;
                active[record] = 1;
                _liveAllocationCount++;
                _freePages -= pageCount;
                _allocatedPages += pageCount;
                allocation = new KernelPhysicalAllocation(token, candidate * PageSize, pageCount);
                _lastStatus = KernelPhysicalMemoryStatus.Success;
                return true;
            }
        }
        return SetFailure(KernelPhysicalMemoryStatus.OutOfMemory);
    }

    /// <summary>Permanently excludes one 4 KiB physical page from ordinary early allocation when it is currently free.</summary>
    /// <param name="physicalAddress">The 4 KiB-aligned physical page address to protect.</param>
    /// <returns><see langword="true"/> when the page is excluded or was already unavailable to this manager.</returns>
    public static Boolean TryExcludePage(UInt64 physicalAddress)
    {
        if (!_initialized) return SetFailure(KernelPhysicalMemoryStatus.NotInitialized);
        if ((physicalAddress & 0xFFFUL) != 0UL) return SetFailure(KernelPhysicalMemoryStatus.InvalidParameter);
        UInt64 frame = physicalAddress / PageSize;
        fixed (UInt64* starts = _state.ExtentStarts)
        fixed (UInt64* pages = _state.ExtentPages)
        {
            for (Int32 index = 0; index < _extentCount; index++)
            {
                UInt64 start = starts[index];
                UInt64 count = pages[index];
                if (frame < start || frame - start >= count) continue;
                UInt64 prefix = frame - start;
                UInt64 suffix = count - prefix - 1UL;
                if (prefix != 0UL && suffix != 0UL && _extentCount >= MaximumExtents)
                    return SetFailure(KernelPhysicalMemoryStatus.ExtentCapacityExhausted);
                if (!ReplaceAllocatedExtent(index, start, prefix, frame + 1UL, suffix))
                    return SetFailure(KernelPhysicalMemoryStatus.ExtentCapacityExhausted);
                _freePages--;
                _lastStatus = KernelPhysicalMemoryStatus.Success;
                return true;
            }
        }
        _lastStatus = KernelPhysicalMemoryStatus.Success;
        return true;
    }


    /// <summary>Releases a live physical allocation exactly once and coalesces adjacent free extents.</summary>
    /// <returns><see langword="true"/> when the token identifies an active allocation.</returns>
    public static Boolean TryRelease(KernelPhysicalAllocation allocation)
    {
        if (!_initialized) return SetFailure(KernelPhysicalMemoryStatus.NotInitialized);
        fixed (UInt64* tokens = _state.AllocationTokens)
        fixed (UInt64* starts = _state.AllocationStarts)
        fixed (UInt64* pages = _state.AllocationPages)
        fixed (Byte* active = _state.AllocationActive)
        {
            for (Int32 index = 0; index < MaximumAllocations; index++)
            {
                if (active[index] == 0 || tokens[index] != allocation.Token || starts[index] * PageSize != allocation.StartAddress || pages[index] != allocation.PageCount) continue;
                if (!InsertFreeExtent(starts[index], pages[index])) return SetFailure(KernelPhysicalMemoryStatus.ExtentCapacityExhausted);
                active[index] = 0;
                _liveAllocationCount--;
                _freePages += pages[index];
                _allocatedPages -= pages[index];
                _lastStatus = KernelPhysicalMemoryStatus.Success;
                return true;
            }
        }
        return SetFailure(KernelPhysicalMemoryStatus.AllocationNotFound);
    }

    /// <summary>Gets current physical-memory page accounting and free-extent diagnostics.</summary>
    public static KernelPhysicalMemoryStatistics GetStatistics()
    {
        if (!_initialized) return default;
        UInt64 largest = 0UL;
        fixed (UInt64* pages = _state.ExtentPages)
        {
            for (Int32 index = 0; index < _extentCount; index++) if (pages[index] > largest) largest = pages[index];
        }
        return new KernelPhysicalMemoryStatistics(_managedPages, _freePages, _allocatedPages, largest, _extentCount, _liveAllocationCount);
    }

    private static Boolean InsertFreeExtent(UInt64 startFrame, UInt64 pageCount)
    {
        if (pageCount == 0UL || startFrame > 0xFFFFFFFFFFFFFFFFUL - pageCount) return false;
        fixed (UInt64* starts = _state.ExtentStarts)
        fixed (UInt64* pages = _state.ExtentPages)
        {
            Int32 insert = 0;
            while (insert < _extentCount && starts[insert] < startFrame) insert++;
            if (_extentCount >= MaximumExtents) return false;
            for (Int32 move = _extentCount; move > insert; move--)
            {
                starts[move] = starts[move - 1];
                pages[move] = pages[move - 1];
            }
            starts[insert] = startFrame;
            pages[insert] = pageCount;
            _extentCount++;
            return CoalesceAround(insert);
        }
    }

    private static Boolean CoalesceAround(Int32 index)
    {
        fixed (UInt64* starts = _state.ExtentStarts)
        fixed (UInt64* pages = _state.ExtentPages)
        {
            if (index > 0)
            {
                UInt64 previousEnd = starts[index - 1] + pages[index - 1];
                if (previousEnd > starts[index]) return false;
                if (previousEnd == starts[index])
                {
                    pages[index - 1] += pages[index];
                    RemoveExtent(index);
                    index--;
                }
            }
            if (index + 1 < _extentCount)
            {
                UInt64 end = starts[index] + pages[index];
                if (end > starts[index + 1]) return false;
                if (end == starts[index + 1])
                {
                    pages[index] += pages[index + 1];
                    RemoveExtent(index + 1);
                }
            }
        }
        return true;
    }

    private static Boolean ReplaceAllocatedExtent(Int32 index, UInt64 originalStart, UInt64 prefixPages, UInt64 suffixStart, UInt64 suffixPages)
    {
        fixed (UInt64* starts = _state.ExtentStarts)
        fixed (UInt64* pages = _state.ExtentPages)
        {
            if (prefixPages == 0UL && suffixPages == 0UL) return RemoveExtent(index);
            if (prefixPages == 0UL)
            {
                starts[index] = suffixStart;
                pages[index] = suffixPages;
                return true;
            }
            starts[index] = originalStart;
            pages[index] = prefixPages;
            if (suffixPages == 0UL) return true;
            for (Int32 move = _extentCount; move > index + 1; move--)
            {
                starts[move] = starts[move - 1];
                pages[move] = pages[move - 1];
            }
            starts[index + 1] = suffixStart;
            pages[index + 1] = suffixPages;
            _extentCount++;
            return true;
        }
    }

    private static Boolean RemoveExtent(Int32 index)
    {
        fixed (UInt64* starts = _state.ExtentStarts)
        fixed (UInt64* pages = _state.ExtentPages)
        {
            for (Int32 move = index; move + 1 < _extentCount; move++)
            {
                starts[move] = starts[move + 1];
                pages[move] = pages[move + 1];
            }
            _extentCount--;
            if (_extentCount >= 0)
            {
                starts[_extentCount] = 0UL;
                pages[_extentCount] = 0UL;
            }
        }
        return true;
    }

    private static Int32 FindFreeAllocationRecord()
    {
        fixed (Byte* active = _state.AllocationActive)
        {
            for (Int32 index = 0; index < MaximumAllocations; index++) if (active[index] == 0) return index;
        }
        return -1;
    }

    private static Boolean TryAlignFrame(UInt64 frame, UInt64 alignmentPages, out UInt64 aligned)
    {
        aligned = 0UL;
        UInt64 mask = alignmentPages - 1UL;
        if (frame > 0xFFFFFFFFFFFFFFFFUL - mask) return false;
        aligned = (frame + mask) & ~mask;
        return true;
    }

    private static UInt64 NextToken()
    {
        _nextToken++;
        if (_nextToken == 0UL) _nextToken++;
        return _nextToken;
    }

    private static Boolean SetFailure(KernelPhysicalMemoryStatus status)
    {
        _lastStatus = status;
        return false;
    }

    private static Boolean ResetState()
    {
        _extentCount = 0;
        _liveAllocationCount = 0;
        _managedPages = 0UL;
        _freePages = 0UL;
        _allocatedPages = 0UL;
        _nextToken = 0UL;
        _bootstrapWorkspaceAddress = 0UL;
        _bootstrapWorkspacePages = 0UL;
        _bootstrapWorkspaceUsedPages = 0UL;
        fixed (UInt64* starts = _state.ExtentStarts)
        fixed (UInt64* pages = _state.ExtentPages)
        {
            for (Int32 index = 0; index < MaximumExtents; index++)
            {
                starts[index] = 0UL;
                pages[index] = 0UL;
            }
        }
        fixed (UInt64* tokens = _state.AllocationTokens)
        fixed (UInt64* starts = _state.AllocationStarts)
        fixed (UInt64* pages = _state.AllocationPages)
        fixed (Byte* active = _state.AllocationActive)
        {
            for (Int32 index = 0; index < MaximumAllocations; index++)
            {
                tokens[index] = 0UL;
                starts[index] = 0UL;
                pages[index] = 0UL;
                active[index] = 0;
            }
        }
        return true;
    }
}
