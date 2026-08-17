using System.Runtime.InteropServices;
using NovaOryn.Boot.Contracts;
using NovaOryn.Boot.Memory;
using NovaOryn.Memory;
using NovaOryn.Primitives;

List<string> failures = [];
TestFinalMapRetry(failures);
TestStrictOverlap(failures);
TestSafetyPriorityAndReservations(failures);
TestConservativeOverlap(failures);
TestSortingAndMerging(failures);
TestReservationOnlyRegion(failures);
TestInvalidProviderCount(failures);
TestRuntimeAttributePreservation(failures);
TestReservationPreservesRuntimeOwnership(failures);
TestSafeOverlapMetadata(failures);
TestNativeFinalMapSource(failures);
TestRequiredReservationValidation(failures);
TestOverflowRejection(failures);

if (failures.Count != 0)
{
    foreach (string failure in failures) Console.Error.WriteLine($"[FAIL] {failure}");
    return 1;
}

Console.WriteLine("[ OK ] Final UEFI map capture retries stale keys and seals only the accepted map.");
Console.WriteLine("[ OK ] Strict, safety-priority, and conservative normalisers behave independently.");
Console.WriteLine("[ OK ] Sorting, splitting, compatible merging, reservations, native-map adaptation, and overflow rejection passed.");
return 0;

static void TestFinalMapRetry(List<string> failures)
{
    UefiMemoryDescriptor[][] maps =
    [
        [new(UefiMemoryType.ConventionalMemory, new PhysicalAddress(0x1000), 0, 3, UefiMemoryAttributes.WriteBack)],
        [new(UefiMemoryType.ConventionalMemory, new PhysicalAddress(0x1000), 0, 4, UefiMemoryAttributes.WriteBack)]
    ];
    FakeProvider provider = new(maps, [11UL, 12UL]);
    UefiMemoryMapWorkspace workspace = new(8);
    bool result = FinalUefiMemoryMapAcquirer.TryCaptureAndExit(provider, 7, workspace, 4, out FinalUefiMemoryMapSnapshot? snapshot, out UefiMemoryMapStatus mapStatus, out UefiExitBootServicesStatus exitStatus);
    Check(result, "Final map acquisition failed.", failures);
    Check(mapStatus == UefiMemoryMapStatus.Success, "Final map status was not success.", failures);
    Check(exitStatus == UefiExitBootServicesStatus.Success, "ExitBootServices did not succeed.", failures);
    Check(snapshot is not null && snapshot.IsFinal && snapshot.MapKey == 12 && snapshot.CaptureAttempts == 2, "The accepted second map was not retained.", failures);
    Check(snapshot is not null && snapshot.TryGetDescriptor(0, out MemoryDescriptor descriptor) && descriptor.PageCount == 4, "The stale first map leaked into the final snapshot.", failures);
    bool reused = FinalUefiMemoryMapAcquirer.TryCaptureAndExit(provider, 7, workspace, 1, out _, out _, out _);
    Check(!reused && snapshot is not null && snapshot.TryGetDescriptor(0, out descriptor) && descriptor.PageCount == 4, "A sealed final-map workspace was reused or mutated.", failures);
}

static void TestStrictOverlap(List<string> failures)
{
    FinalUefiMemoryMapSnapshot snapshot = CreateOverlapSnapshot();
    IMemoryMapNormaliser normaliser = MemoryMapNormaliserFactory.Create(MemoryMapNormalisationMethod.Strict);
    MemoryMapNormalisationResult result = normaliser.Normalise(snapshot, [], new MemoryMapNormalisationWorkspace(8, 16), out NormalisedMemoryMap? map);
    Check(result.Status == MemoryMapNormalisationStatus.OverlapConflict && map is null, "Strict normalisation did not reject incompatible overlap.", failures);
}

static void TestSafetyPriorityAndReservations(List<string> failures)
{
    FinalUefiMemoryMapSnapshot snapshot = CreateOverlapSnapshot();
    MemoryReservationPlan plan = new(4);
    Check(plan.TryAddPageTables(new PhysicalAddress(0x2000), 1), "Page-table reservation was rejected.", failures);
    IMemoryMapNormaliser normaliser = MemoryMapNormaliserFactory.Create(MemoryMapNormalisationMethod.SafetyPriority);
    MemoryMapNormalisationResult result = normaliser.Normalise(snapshot, plan.ToArray(), new MemoryMapNormalisationWorkspace(8, 16), out NormalisedMemoryMap? map);
    Check(result.Succeeded() && map is not null, "Safety-priority normalisation failed.", failures);
    Check(map is not null && map.Count == 3, "Safety-priority split count was incorrect.", failures);
    Check(map is not null && map.TryGetDescriptor(0, out MemoryDescriptor first) && first.MemoryType == MemoryType.UsableConventional && first.PhysicalStart.Value == 0x1000 && first.EndExclusive == 0x2000, "Initial usable split is incorrect.", failures);
    Check(map is not null && map.TryGetDescriptor(1, out MemoryDescriptor second) && second.MemoryType == MemoryType.PageTables, "Page-table overlay did not override firmware availability.", failures);
    Check(map is not null && map.TryGetDescriptor(2, out MemoryDescriptor third) && third.MemoryType == MemoryType.RuntimeServices && third.PhysicalStart.Value == 0x3000, "Runtime overlap did not win by safety priority.", failures);
}

static void TestConservativeOverlap(List<string> failures)
{
    FinalUefiMemoryMapSnapshot snapshot = CreateOverlapSnapshot();
    IMemoryMapNormaliser normaliser = MemoryMapNormaliserFactory.Create(MemoryMapNormalisationMethod.Conservative);
    MemoryMapNormalisationResult result = normaliser.Normalise(snapshot, [], new MemoryMapNormalisationWorkspace(8, 16), out NormalisedMemoryMap? map);
    Check(result.Succeeded() && map is not null, "Conservative normalisation failed.", failures);
    Check(map is not null && map.TryGetDescriptor(1, out MemoryDescriptor overlap) && overlap.MemoryType == MemoryType.FirmwareReserved && overlap.Availability == MemoryAvailability.RuntimeOwned && overlap.RuntimeStatus == MemoryRuntimeStatus.RuntimeData, "Conservative overlap did not preserve runtime ownership while reserving the conflict.", failures);
}

static void TestSortingAndMerging(List<string> failures)
{
    UefiMemoryDescriptor[][] maps =
    [[
        new(UefiMemoryType.ConventionalMemory, new PhysicalAddress(0x3000), 0, 2, UefiMemoryAttributes.WriteBack),
        new(UefiMemoryType.ConventionalMemory, new PhysicalAddress(0x1000), 0, 2, UefiMemoryAttributes.WriteBack)
    ]];
    FakeProvider provider = new(maps, [99UL], false);
    UefiMemoryMapWorkspace capture = new(8);
    Check(FinalUefiMemoryMapAcquirer.TryCaptureAndExit(provider, 1, capture, 1, out FinalUefiMemoryMapSnapshot? snapshot, out _, out _), "Sorting test map was not captured.", failures);
    IMemoryMapNormaliser normaliser = MemoryMapNormaliserFactory.Create(MemoryMapNormalisationMethod.Strict);
    MemoryMapNormalisationResult result = normaliser.Normalise(snapshot!, [], new MemoryMapNormalisationWorkspace(8, 16), out NormalisedMemoryMap? map);
    Check(result.Succeeded() && map is not null && map.Count == 1, "Adjacent compatible ranges were not merged.", failures);
    Check(map is not null && map.TryGetDescriptor(0, out MemoryDescriptor descriptor) && descriptor.PhysicalStart.Value == 0x1000 && descriptor.PageCount == 4, "Merged descriptor is not sorted or complete.", failures);
    MemoryMapDiagnosticCursor cursor = map!.CreateDiagnosticCursor();
    Check(cursor.MoveNext() && cursor.Current.PageCount == 4 && !cursor.MoveNext(), "Immutable diagnostic cursor returned an invalid sequence.", failures);
}


static void TestReservationOnlyRegion(List<string> failures)
{
    UefiMemoryDescriptor[][] maps =
    [[new(UefiMemoryType.ConventionalMemory, new PhysicalAddress(0x1000), 0, 1, UefiMemoryAttributes.WriteBack)]];
    FakeProvider provider = new(maps, [70UL], false);
    UefiMemoryMapWorkspace capture = new(4);
    Check(FinalUefiMemoryMapAcquirer.TryCaptureAndExit(provider, 1, capture, 1, out FinalUefiMemoryMapSnapshot? snapshot, out _, out _), "Reservation-only test map was not captured.", failures);
    MemoryReservationPlan plan = new(2);
    Check(plan.TryAddMemoryMappedIo(new PhysicalAddress(0xFEC00000), 1), "Reservation-only MMIO range was rejected.", failures);
    IMemoryMapNormaliser normaliser = MemoryMapNormaliserFactory.Create(MemoryMapNormalisationMethod.Strict);
    MemoryMapNormalisationResult result = normaliser.Normalise(snapshot!, plan.ToArray(), new MemoryMapNormalisationWorkspace(4, 8), out NormalisedMemoryMap? map);
    Check(result.Succeeded() && map is not null && map.Count == 2, "A reservation outside firmware descriptors was omitted.", failures);
    Check(map is not null && map.TryGetDescriptor(1, out MemoryDescriptor descriptor) && descriptor.MemoryType == MemoryType.MemoryMappedIo, "Reservation-only MMIO metadata was not retained.", failures);
}

static void TestInvalidProviderCount(List<string> failures)
{
    InvalidCountProvider provider = new();
    UefiMemoryMapWorkspace workspace = new(2);
    bool result = FinalUefiMemoryMapAcquirer.TryCaptureAndExit(provider, 1, workspace, 1, out FinalUefiMemoryMapSnapshot? snapshot, out UefiMemoryMapStatus mapStatus, out _);
    Check(!result && snapshot is null && mapStatus == UefiMemoryMapStatus.InvalidParameter, "An invalid provider count was accepted.", failures);
    Check(provider.ExitCalls == 0, "ExitBootServices was called before the provider count was validated.", failures);
}

static void TestRuntimeAttributePreservation(List<string> failures)
{
    UefiMemoryDescriptor source = new(UefiMemoryType.MemoryMappedIo, new PhysicalAddress(0xFEE00000), 0, 1, UefiMemoryAttributes.Uncacheable | UefiMemoryAttributes.Runtime);
    Check(UefiMemoryDescriptorMapper.TryMap(source, out MemoryDescriptor descriptor), "Runtime MMIO descriptor did not map.", failures);
    Check(descriptor.MemoryType == MemoryType.MemoryMappedIo && descriptor.RuntimeStatus == MemoryRuntimeStatus.RuntimeData && descriptor.Availability == MemoryAvailability.RuntimeOwned, "UEFI runtime attribute was not retained for MMIO.", failures);
}



static void TestReservationPreservesRuntimeOwnership(List<string> failures)
{
    UefiMemoryDescriptor[][] maps =
    [[new(UefiMemoryType.RuntimeServicesData, new PhysicalAddress(0xFEE00000), 0, 1, UefiMemoryAttributes.WriteBack | UefiMemoryAttributes.ExecuteProtected | UefiMemoryAttributes.Runtime)]];
    FakeProvider provider = new(maps, [79UL], false);
    UefiMemoryMapWorkspace capture = new(2);
    Check(FinalUefiMemoryMapAcquirer.TryCaptureAndExit(provider, 1, capture, 1, out FinalUefiMemoryMapSnapshot? snapshot, out _, out _), "Runtime reservation map was not captured.", failures);
    MemoryReservationPlan plan = new(1);
    Check(plan.TryAddMemoryMappedIo(new PhysicalAddress(0xFEE00000), 1), "Runtime MMIO reservation was rejected.", failures);
    IMemoryMapNormaliser normaliser = MemoryMapNormaliserFactory.Create(MemoryMapNormalisationMethod.Strict);
    MemoryMapNormalisationResult result = normaliser.Normalise(snapshot!, plan.ToArray(), new MemoryMapNormalisationWorkspace(4, 4), out NormalisedMemoryMap? map);
    Check(result.Succeeded() && map is not null && map.TryGetDescriptor(0, out MemoryDescriptor descriptor), "Runtime MMIO reservation did not normalise.", failures);
    Check(map is not null && map.TryGetDescriptor(0, out descriptor) && descriptor.MemoryType == MemoryType.MemoryMappedIo && descriptor.RuntimeStatus == MemoryRuntimeStatus.RuntimeData && descriptor.Availability == MemoryAvailability.RuntimeOwned, "An explicit reservation discarded firmware runtime ownership.", failures);
    Check(map is not null && map.TryGetDescriptor(0, out descriptor) && descriptor.CacheAttributes == (MemoryCacheAttributes.Uncacheable | MemoryCacheAttributes.ExecuteProtected), "A runtime reservation did not preserve the safest cache and protection attributes.", failures);
}


static void TestSafeOverlapMetadata(List<string> failures)
{
    UefiMemoryDescriptor[][] maps =
    [[
        new(UefiMemoryType.RuntimeServicesCode, new PhysicalAddress(0x8000), 0, 2, UefiMemoryAttributes.WriteBack | UefiMemoryAttributes.Runtime),
        new(UefiMemoryType.RuntimeServicesData, new PhysicalAddress(0x9000), 0, 1, UefiMemoryAttributes.Uncacheable | UefiMemoryAttributes.ExecuteProtected | UefiMemoryAttributes.Runtime)
    ]];
    FakeProvider provider = new(maps, [81UL], false);
    UefiMemoryMapWorkspace capture = new(4);
    Check(FinalUefiMemoryMapAcquirer.TryCaptureAndExit(provider, 1, capture, 1, out FinalUefiMemoryMapSnapshot? snapshot, out _, out _), "Runtime overlap map was not captured.", failures);
    IMemoryMapNormaliser normaliser = MemoryMapNormaliserFactory.Create(MemoryMapNormalisationMethod.SafetyPriority);
    MemoryMapNormalisationResult result = normaliser.Normalise(snapshot!, [], new MemoryMapNormalisationWorkspace(4, 8), out NormalisedMemoryMap? map);
    Check(result.Succeeded() && map is not null, "Runtime overlap normalisation failed.", failures);
    Check(map is not null && map.TryGetDescriptor(1, out MemoryDescriptor overlap) && overlap.RuntimeStatus == MemoryRuntimeStatus.RuntimeCodeAndData, "Mixed runtime code/data ownership was not retained.", failures);
    Check(map is not null && map.TryGetDescriptor(1, out overlap) && overlap.CacheAttributes == (MemoryCacheAttributes.Uncacheable | MemoryCacheAttributes.ExecuteProtected), "Conflicting cache modes were not reduced to one safe cache mode.", failures);
}

static void TestNativeFinalMapSource(List<string> failures)
{
    IntPtr buffer = Marshal.AllocHGlobal(48);
    try
    {
        for (int offset = 0; offset < 48; offset += 4) Marshal.WriteInt32(buffer, offset, 0);
        Marshal.WriteInt32(buffer, 0, (int)UefiMemoryType.ConventionalMemory);
        Marshal.WriteInt64(buffer, 8, 0x1000);
        Marshal.WriteInt64(buffer, 16, 0);
        Marshal.WriteInt64(buffer, 24, 3);
        Marshal.WriteInt64(buffer, 32, (long)UefiMemoryAttributes.WriteBack);
        ulong address = unchecked((ulong)buffer.ToInt64());
        BootContext boot = new(BootProtocol.Uefi, default, new PhysicalAddress(address), 48, 88, 48, 1, true);
        Check(NativeUefiMemoryMapSource.TryCreate(boot, out NativeUefiMemoryMapSource? source), "The native final-map adapter rejected valid metadata.", failures);
        Check(source is not null && source.TryGetDescriptor(0, out MemoryDescriptor descriptor) && descriptor.MemoryType == MemoryType.UsableConventional && descriptor.PageCount == 3, "The native final-map adapter did not parse the retained UEFI descriptor.", failures);
    }
    finally
    {
        Marshal.FreeHGlobal(buffer);
    }
}

static void TestRequiredReservationValidation(List<string> failures)
{
    MemoryReservationPlan plan = new(4);
    Check(!plan.TryValidateRequiredReservations(true, true, out MemoryType firstMissing) && firstMissing == MemoryType.LoaderKernelImage, "A plan without the kernel image was accepted.", failures);
    Check(plan.TryAddKernelImage(new PhysicalAddress(0x100000), 16), "Required kernel reservation was rejected.", failures);
    Check(plan.TryAddBootStructures(new PhysicalAddress(0x200000), 2), "Required boot-structure reservation was rejected.", failures);
    Check(plan.TryAddFramebuffer(new PhysicalAddress(0xE0000000), 4, MemoryCacheAttributes.WriteCombining), "Required framebuffer reservation was rejected.", failures);
    Check(plan.TryAddMemoryMappedIo(new PhysicalAddress(0xFEC00000), 1), "Required MMIO reservation was rejected.", failures);
    Check(plan.TryValidateRequiredReservations(true, true, out MemoryType missing) && missing == MemoryType.Unknown, "A complete required reservation plan was rejected.", failures);
}

static void TestOverflowRejection(List<string> failures)
{
    bool valid = MemoryDescriptor.TryCreate(new PhysicalAddress(ulong.MaxValue - 0xFFFUL), 1, MemoryType.UsableConventional, MemoryCacheAttributes.WriteBack, MemoryRuntimeStatus.NotRuntime, MemoryAvailability.AvailableAfterExitBootServices, false, 0, out _);
    Check(!valid, "Descriptor overflow was accepted.", failures);
}

static FinalUefiMemoryMapSnapshot CreateOverlapSnapshot()
{
    UefiMemoryDescriptor[][] maps =
    [[
        new(UefiMemoryType.ConventionalMemory, new PhysicalAddress(0x1000), 0, 4, UefiMemoryAttributes.WriteBack),
        new(UefiMemoryType.RuntimeServicesData, new PhysicalAddress(0x3000), 0, 2, UefiMemoryAttributes.WriteBack | UefiMemoryAttributes.Runtime)
    ]];
    FakeProvider provider = new(maps, [42UL], false);
    UefiMemoryMapWorkspace workspace = new(8);
    if (!FinalUefiMemoryMapAcquirer.TryCaptureAndExit(provider, 1, workspace, 1, out FinalUefiMemoryMapSnapshot? snapshot, out _, out _) || snapshot is null)
        throw new InvalidOperationException("Test snapshot could not be created.");
    return snapshot;
}

static void Check(bool condition, string message, List<string> failures)
{
    if (!condition) failures.Add(message);
}


sealed class InvalidCountProvider : IUefiMemoryMapProvider
{
    internal int ExitCalls { get; private set; }

    public UefiMemoryMapStatus GetMemoryMap(UefiMemoryDescriptor[] destination, out int count, out ulong mapKey, out uint descriptorVersion)
    {
        count = destination.Length + 1;
        mapKey = 5;
        descriptorVersion = 1;
        return UefiMemoryMapStatus.Success;
    }

    public UefiExitBootServicesStatus ExitBootServices(ulong imageHandle, ulong mapKey)
    {
        ExitCalls++;
        return UefiExitBootServicesStatus.Success;
    }
}

sealed class FakeProvider : IUefiMemoryMapProvider
{
    private readonly UefiMemoryDescriptor[][] _maps;
    private readonly ulong[] _keys;
    private readonly bool _failFirstExit;
    private int _mapIndex;
    private int _exitCalls;

    internal FakeProvider(UefiMemoryDescriptor[][] maps, ulong[] keys, bool failFirstExit = true)
    {
        _maps = maps;
        _keys = keys;
        _failFirstExit = failFirstExit;
    }

    public UefiMemoryMapStatus GetMemoryMap(UefiMemoryDescriptor[] destination, out int count, out ulong mapKey, out uint descriptorVersion)
    {
        int index = Math.Min(_mapIndex, _maps.Length - 1);
        UefiMemoryDescriptor[] current = _maps[index];
        count = current.Length;
        mapKey = _keys[Math.Min(index, _keys.Length - 1)];
        descriptorVersion = 1;
        if (destination.Length < current.Length) return UefiMemoryMapStatus.BufferTooSmall;
        Array.Copy(current, destination, current.Length);
        _mapIndex++;
        return UefiMemoryMapStatus.Success;
    }

    public UefiExitBootServicesStatus ExitBootServices(ulong imageHandle, ulong mapKey)
    {
        _exitCalls++;
        if (_failFirstExit && _exitCalls == 1) return UefiExitBootServicesStatus.InvalidMapKey;
        return UefiExitBootServicesStatus.Success;
    }
}
