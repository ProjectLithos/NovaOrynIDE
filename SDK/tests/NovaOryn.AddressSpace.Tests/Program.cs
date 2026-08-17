using NovaOryn.Memory.AddressSpace;
using NovaOryn.Memory.AddressSpace.X64;

List<string> failures = [];
Check(X64KernelAddressSpace.TryCreateStandard(out KernelAddressSpaceLayout layout), "Standard x64 kernel layout could not be created.", failures);
Check(X64KernelAddressSpace.Validate(layout), "Standard x64 kernel layout failed x64 validation.", failures);
Check(layout.User.BaseAddress == 0x10000UL && layout.User.EndExclusive == X64KernelAddressSpace.UserEndExclusive, "User/null-guard split is incorrect.", failures);
Check(layout.KernelImage.BaseAddress == X64KernelAddressSpace.KernelImageBase, "Kernel image base is incorrect.", failures);
Check(layout.KernelHeap.BaseAddress == X64KernelAddressSpace.KernelHeapBase && layout.KernelHeap.Length == X64KernelAddressSpace.KernelHeapLength, "Kernel heap reservation is incorrect.", failures);
Check(layout.KernelStacks.BaseAddress == X64KernelAddressSpace.KernelStacksBase, "Kernel stack arena is incorrect.", failures);
Check(layout.DirectPhysicalMap.Length == 0x0000400000000000UL, "Direct-map capacity is not 64 TiB.", failures);
Check(!layout.DirectPhysicalMap.Overlaps(layout.Mmio) && !layout.Mmio.Overlaps(layout.PageTableWindow), "Standard high-half regions overlap.", failures);
Check(X64KernelAddressSpace.TryPhysicalToDirectMap(0x12345000UL, out ulong direct) && direct == X64KernelAddressSpace.DirectMapBase + 0x12345000UL, "Physical-to-direct-map conversion failed.", failures);
Check(X64KernelAddressSpace.TryDirectMapToPhysical(direct, out ulong physical) && physical == 0x12345000UL, "Direct-map-to-physical conversion failed.", failures);
Check(!X64KernelAddressSpace.TryPhysicalToDirectMap(X64KernelAddressSpace.DirectMapLength, out _), "Out-of-range direct-map physical address was accepted.", failures);
Check(KernelAddressSpaceRegion.TryCreate(KernelAddressSpaceRegionKind.KernelHeap, 0xFFFF810000000000UL, 0x2000UL, out var first), "Valid custom region rejected.", failures);
Check(KernelAddressSpaceRegion.TryCreate(KernelAddressSpaceRegionKind.KernelStacks, 0xFFFF810000001000UL, 0x2000UL, out var second) && first.Overlaps(second), "Overlap detection failed.", failures);
if (failures.Count != 0) { foreach (string failure in failures) Console.Error.WriteLine($"[FAIL] {failure}"); return 1; }
Console.WriteLine("[ OK ] Kernel address-space layout, x64 canonical policy, region separation, and direct-map transforms passed.");
return 0;
static void Check(bool condition, string failure, List<string> failures) { if (!condition) failures.Add(failure); }
