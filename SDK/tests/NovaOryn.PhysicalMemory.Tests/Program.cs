using System.Runtime.InteropServices;
using NovaOryn.Boot.Memory;
using NovaOryn.Memory;
using NovaOryn.Memory.Physical;
using NovaOryn.Primitives;

List<string> failures = [];
NormalisedMemoryMap map = CreateMap();

TestExtent(map, failures);
TestBitmap(map, failures);
TestBuddy(map, failures);
TestWorkspaceFailure(map, failures);
TestRecordCapacity(map, failures);
TestCustomContract(failures);

if (failures.Count != 0)
{
    foreach (string failure in failures) Console.Error.WriteLine($"[FAIL] {failure}");
    return 1;
}

Console.WriteLine("[ OK ] Physical-memory contracts, bitmap, buddy, extent, reservations, constraints, and release validation passed.");
return 0;

static void TestExtent(NormalisedMemoryMap map, List<string> failures)
{
    Check(ExtentPhysicalMemoryManager.TryGetRequiredWorkspaceBytes(map, 8, 4, out ulong bytes) && bytes > 0, "Extent workspace sizing failed.", failures);
    ExtentPhysicalMemoryManager manager = default;
    ExerciseManager("Extent", manager, map, bytes, 3, failures);
}

static void TestBitmap(NormalisedMemoryMap map, List<string> failures)
{
    Check(BitmapPhysicalMemoryManager.TryGetRequiredWorkspaceBytes(map, 8, 4, out ulong bytes) && bytes > 0, "Bitmap workspace sizing failed.", failures);
    BitmapPhysicalMemoryManager manager = default;
    ExerciseManager("Bitmap", manager, map, bytes, 3, failures);
}

static void TestBuddy(NormalisedMemoryMap map, List<string> failures)
{
    Check(BuddyPhysicalMemoryManager.TryGetRequiredWorkspaceBytes(map, 8, 4, out ulong bytes) && bytes > 0, "Buddy workspace sizing failed.", failures);
    BuddyPhysicalMemoryManager manager = default;
    ExerciseManager("Buddy", manager, map, bytes, 4, failures);
}

static void ExerciseManager(string name, IPhysicalMemoryManager manager, NormalisedMemoryMap map, ulong workspaceBytes, ulong expectedFirstAllocationPages, List<string> failures)
{
    IntPtr storage = Marshal.AllocHGlobal(checked((nint)workspaceBytes));
    try
    {
        Check(PhysicalAllocatorWorkspace.TryCreate(storage, workspaceBytes, out PhysicalAllocatorWorkspace workspace), $"{name} workspace creation failed.", failures);
        Check(manager.TryInitialize(map, workspace, 8, 4, out PhysicalMemoryStatus initStatus) && initStatus == PhysicalMemoryStatus.Success, $"{name} initialisation failed: {initStatus}.", failures);
        PhysicalMemoryStatistics initial = manager.GetStatistics();
        Check(initial.TotalManagedPages == 384 && initial.FreePages == 384 && initial.AllocatedPages == 0 && initial.ReservedPages == 0, $"{name} initial accounting is incorrect.", failures);

        Check(PhysicalAllocationRequest.TryCreate(3, 4, new PhysicalAddress(0x100000), new PhysicalAddress(0x200000), PhysicalMemoryPurpose.PageTables, out PhysicalAllocationRequest request), $"{name} request creation failed.", failures);
        Check(manager.TryAllocate(request, out PhysicalAllocation allocation, out PhysicalMemoryStatus allocationStatus) && allocationStatus == PhysicalMemoryStatus.Success, $"{name} constrained allocation failed: {allocationStatus}.", failures);
        Check((allocation.Range.Start.Value & 0x3FFFUL) == 0, $"{name} did not honour four-page alignment.", failures);
        Check(allocation.RequestedPageCount == 3 && allocation.Range.PageCount == expectedFirstAllocationPages, $"{name} allocation rounding is incorrect.", failures);

        Check(PhysicalFrameRange.TryCreate(new PhysicalAddress(0x220000), 5, out PhysicalFrameRange reserveRange), $"{name} reservation range creation failed.", failures);
        Check(manager.TryReserve(reserveRange, PhysicalMemoryPurpose.Metadata, out PhysicalReservation reservation, out PhysicalMemoryStatus reserveStatus) && reserveStatus == PhysicalMemoryStatus.Success, $"{name} exact reservation failed: {reserveStatus}.", failures);
        PhysicalMemoryStatistics occupied = manager.GetStatistics();
        Check(occupied.AllocatedPages == expectedFirstAllocationPages && occupied.ReservedPages == 5 && occupied.ActiveAllocations == 1 && occupied.ActiveReservations == 1, $"{name} occupied accounting is incorrect.", failures);

        Check(PhysicalAllocationRequest.TryCreate(1, 1, default, new PhysicalAddress(0x80000), PhysicalMemoryPurpose.General, out PhysicalAllocationRequest impossible), $"{name} impossible request creation failed.", failures);
        Check(!manager.TryAllocate(impossible, out _, out PhysicalMemoryStatus impossibleStatus) && impossibleStatus == PhysicalMemoryStatus.AddressConstraintUnsatisfied, $"{name} did not report an address-constraint failure.", failures);

        Check(manager.TryRelease(allocation, out PhysicalMemoryStatus releaseStatus) && releaseStatus == PhysicalMemoryStatus.Success, $"{name} allocation release failed.", failures);
        Check(!manager.TryRelease(allocation, out PhysicalMemoryStatus secondReleaseStatus) && secondReleaseStatus == PhysicalMemoryStatus.AllocationNotFound, $"{name} did not reject a double release.", failures);
        Check(manager.TryReleaseReservation(reservation, out PhysicalMemoryStatus reservationReleaseStatus) && reservationReleaseStatus == PhysicalMemoryStatus.Success, $"{name} reservation release failed.", failures);
        Check(!manager.TryReleaseReservation(reservation, out PhysicalMemoryStatus secondReservationReleaseStatus) && secondReservationReleaseStatus == PhysicalMemoryStatus.ReservationNotFound, $"{name} did not reject a second reservation release.", failures);

        PhysicalMemoryStatistics restored = manager.GetStatistics();
        Check(restored.FreePages == restored.TotalManagedPages && restored.AllocatedPages == 0 && restored.ReservedPages == 0, $"{name} did not restore all managed frames.", failures);
    }
    finally
    {
        Marshal.FreeHGlobal(storage);
    }
}

static void TestWorkspaceFailure(NormalisedMemoryMap map, List<string> failures)
{
    Check(ExtentPhysicalMemoryManager.TryGetRequiredWorkspaceBytes(map, 2, 1, out ulong required), "Workspace failure sizing failed.", failures);
    IntPtr storage = Marshal.AllocHGlobal(8);
    try
    {
        Check(PhysicalAllocatorWorkspace.TryCreate(storage, 8, out PhysicalAllocatorWorkspace workspace), "Small workspace descriptor creation failed.", failures);
        ExtentPhysicalMemoryManager manager = default;
        Check(!manager.TryInitialize(map, workspace, 2, 1, out PhysicalMemoryStatus status) && status == PhysicalMemoryStatus.WorkspaceTooSmall && required > 8, "Too-small physical metadata workspace was accepted.", failures);
    }
    finally
    {
        Marshal.FreeHGlobal(storage);
    }
}

static void TestRecordCapacity(NormalisedMemoryMap map, List<string> failures)
{
    Check(BitmapPhysicalMemoryManager.TryGetRequiredWorkspaceBytes(map, 1, 1, out ulong bytes), "Record-capacity sizing failed.", failures);
    IntPtr storage = Marshal.AllocHGlobal(checked((nint)bytes));
    try
    {
        Check(PhysicalAllocatorWorkspace.TryCreate(storage, bytes, out PhysicalAllocatorWorkspace workspace), "Record-capacity workspace creation failed.", failures);
        BitmapPhysicalMemoryManager manager = default;
        Check(manager.TryInitialize(map, workspace, 1, 1, out _), "Record-capacity bitmap initialisation failed.", failures);
        Check(PhysicalAllocationRequest.TryCreate(1, 1, default, default, PhysicalMemoryPurpose.General, out PhysicalAllocationRequest request), "Record-capacity request creation failed.", failures);
        Check(manager.TryAllocate(request, out _, out _), "First bounded allocation failed.", failures);
        Check(!manager.TryAllocate(request, out _, out PhysicalMemoryStatus status) && status == PhysicalMemoryStatus.RecordCapacityExhausted, "Allocation record capacity exhaustion was not reported.", failures);
    }
    finally
    {
        Marshal.FreeHGlobal(storage);
    }
}

static void TestCustomContract(List<string> failures)
{
    IPhysicalMemoryManager custom = new CustomPhysicalMemoryManager();
    Check(custom.Method == PhysicalAllocatorMethod.Custom && custom.PageSize == 4096, "Custom physical-memory contract cannot report its methodology.", failures);
}

static NormalisedMemoryMap CreateMap()
{
    MemoryDescriptor[] descriptors = new MemoryDescriptor[3];
    if (!MemoryDescriptor.TryCreate(new PhysicalAddress(0x100000), 256, MemoryType.UsableConventional, MemoryCacheAttributes.WriteBack, MemoryRuntimeStatus.NotRuntime, MemoryAvailability.AvailableAfterExitBootServices, false, 0, out descriptors[0]))
        throw new InvalidOperationException("Could not create first test range.");
    if (!MemoryDescriptor.TryCreate(new PhysicalAddress(0x200000), 16, MemoryType.FirmwareReserved, MemoryCacheAttributes.Uncacheable, MemoryRuntimeStatus.NotRuntime, MemoryAvailability.PermanentlyReserved, false, 0, out descriptors[1]))
        throw new InvalidOperationException("Could not create reserved test hole.");
    if (!MemoryDescriptor.TryCreate(new PhysicalAddress(0x210000), 128, MemoryType.UsableConventional, MemoryCacheAttributes.WriteBack, MemoryRuntimeStatus.NotRuntime, MemoryAvailability.AvailableAfterExitBootServices, false, 0, out descriptors[2]))
        throw new InvalidOperationException("Could not create second test range.");

    TestMemoryMapSource source = new(descriptors);
    StrictMemoryMapNormaliser normaliser = new();
    MemoryMapNormalisationWorkspace workspace = new(8, 8);
    MemoryMapNormalisationResult result = normaliser.Normalise(source, [], workspace, out NormalisedMemoryMap? map);
    if (!result.Succeeded() || map is null) throw new InvalidOperationException($"Could not normalise physical-memory test map: {result.Status}.");
    return map;
}

static void Check(bool condition, string message, List<string> failures)
{
    if (!condition) failures.Add(message);
}

sealed class TestMemoryMapSource : IMemoryMapSource
{
    private readonly MemoryDescriptor[] _descriptors;

    internal TestMemoryMapSource(MemoryDescriptor[] descriptors) => _descriptors = descriptors;

    public bool IsFinal => true;
    public int Count => _descriptors.Length;

    public bool TryGetDescriptor(int index, out MemoryDescriptor descriptor)
    {
        descriptor = default;
        if ((uint)index >= (uint)_descriptors.Length) return false;
        descriptor = _descriptors[index];
        return true;
    }
}

sealed class CustomPhysicalMemoryManager : IPhysicalMemoryManager
{
    public PhysicalAllocatorMethod Method => PhysicalAllocatorMethod.Custom;
    public bool IsInitialized => false;
    public ulong PageSize => 4096;
    public bool TryInitialize(NormalisedMemoryMap map, PhysicalAllocatorWorkspace workspace, int allocationCapacity, int reservationCapacity, out PhysicalMemoryStatus status) { status = PhysicalMemoryStatus.NotInitialized; return false; }
    public bool TryAllocate(PhysicalAllocationRequest request, out PhysicalAllocation allocation, out PhysicalMemoryStatus status) { allocation = default; status = PhysicalMemoryStatus.NotInitialized; return false; }
    public bool TryRelease(PhysicalAllocation allocation, out PhysicalMemoryStatus status) { status = PhysicalMemoryStatus.NotInitialized; return false; }
    public bool TryReserve(PhysicalFrameRange range, PhysicalMemoryPurpose purpose, out PhysicalReservation reservation, out PhysicalMemoryStatus status) { reservation = default; status = PhysicalMemoryStatus.NotInitialized; return false; }
    public bool TryReleaseReservation(PhysicalReservation reservation, out PhysicalMemoryStatus status) { status = PhysicalMemoryStatus.NotInitialized; return false; }
    public PhysicalMemoryStatistics GetStatistics() => default;
}
