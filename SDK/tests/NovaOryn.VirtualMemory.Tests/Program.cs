using NovaOryn.Memory.Virtual;
using NovaOryn.Memory.Virtual.X64;
using NovaOryn.Primitives;

List<string> failures = [];

Check(X64VirtualAddress.IsCanonical(0x00007FFFFFFFFFFFUL), "Highest low-half canonical address was rejected.", failures);
Check(X64VirtualAddress.IsCanonical(0xFFFF800000000000UL), "Lowest high-half canonical address was rejected.", failures);
Check(!X64VirtualAddress.IsCanonical(0x0000800000000000UL), "Non-canonical low transition address was accepted.", failures);
Check(!X64VirtualAddress.IsCanonical(0xFFFF7FFFFFFFFFFFUL), "Non-canonical high transition address was accepted.", failures);

ulong address = 0xFFFF812345678000UL;
Check(X64VirtualAddress.TryGetIndices(address, out ushort pml4, out ushort pdpt, out ushort pd, out ushort pt), "Canonical x64 indices were not produced.", failures);
Check(pml4 == ((address >> 39) & 0x1FFUL) && pdpt == ((address >> 30) & 0x1FFUL) && pd == ((address >> 21) & 0x1FFUL) && pt == ((address >> 12) & 0x1FFUL), "x64 page-table indices are incorrect.", failures);

VirtualMemoryProtection rwx = VirtualMemoryProtection.Read | VirtualMemoryProtection.Write | VirtualMemoryProtection.Execute | VirtualMemoryProtection.Global;
Check(VirtualMappingRequest.TryCreate(0xFFFF800000200000UL, new PhysicalAddress(0x200000UL), VirtualPageSize.Page2MiB, rwx, out VirtualMappingRequest request), "Valid 2 MiB mapping request was rejected.", failures);
Check(request.PageSize == VirtualPageSize.Page2MiB && request.PhysicalAddress.Value == 0x200000UL, "Mapping request retained incorrect values.", failures);
Check(!VirtualMappingRequest.TryCreate(0xFFFF800000201000UL, new PhysicalAddress(0x200000UL), VirtualPageSize.Page2MiB, rwx, out _), "Misaligned large-page virtual address was accepted.", failures);
Check(!VirtualMappingRequest.TryCreate(0xFFFF800000200000UL, new PhysicalAddress(0x201000UL), VirtualPageSize.Page2MiB, rwx, out _), "Misaligned large-page physical address was accepted.", failures);
Check(!VirtualMappingRequest.TryCreate(0xFFFF800000200000UL, new PhysicalAddress(0x200000UL), VirtualPageSize.Page2MiB, VirtualMemoryProtection.Write, out _), "Write-only mapping without read permission was accepted.", failures);

Check(VirtualAddressRange.TryCreate(0xFFFF800000000000UL, 8, VirtualPageSize.Page4KiB, out VirtualAddressRange range) && range.Length == 32768UL, "Validated virtual range accounting is incorrect.", failures);
Check(!VirtualAddressRange.TryCreate(0xFFFF800000000001UL, 1, VirtualPageSize.Page4KiB, out _), "Misaligned virtual range was accepted.", failures);

VirtualMemoryProtection nx = VirtualMemoryProtection.Read | VirtualMemoryProtection.Write | VirtualMemoryProtection.User | VirtualMemoryProtection.Device;
Check(X64PageTableCodec.TryEncodeLeaf(new PhysicalAddress(0x12345000UL), VirtualPageSize.Page4KiB, nx, out ulong leaf4K), "4 KiB x64 leaf encoding failed.", failures);
Check(X64PageTableCodec.IsPresent(leaf4K) && !X64PageTableCodec.IsLargePage(leaf4K), "4 KiB x64 leaf flags are incorrect.", failures);
Check(X64PageTableCodec.TryDecodeLeaf(leaf4K, 0xFFFF800000001234UL, VirtualPageSize.Page4KiB, out VirtualTranslation translated4K), "4 KiB leaf decode failed.", failures);
Check(translated4K.PhysicalAddress.Value == 0x12345234UL && (translated4K.Protection & VirtualMemoryProtection.Execute) == 0 && (translated4K.Protection & VirtualMemoryProtection.Device) != 0, "4 KiB translation or protection decode is incorrect.", failures);

Check(X64PageTableCodec.TryEncodeLeaf(new PhysicalAddress(0x40000000UL), VirtualPageSize.Page1GiB, VirtualMemoryProtection.Read | VirtualMemoryProtection.Execute, out ulong leaf1G), "1 GiB x64 leaf encoding failed.", failures);
Check(X64PageTableCodec.IsLargePage(leaf1G), "1 GiB x64 leaf did not set the large-page bit.", failures);
Check(X64PageTableCodec.TryDecodeLeaf(leaf1G, 0xFFFF800012345678UL, VirtualPageSize.Page1GiB, out VirtualTranslation translated1G), "1 GiB leaf decode failed.", failures);
Check(translated1G.PhysicalAddress.Value == 0x52345678UL, "1 GiB translation offset is incorrect.", failures);

Check(X64PageTableCodec.TryEncodeTablePointer(new PhysicalAddress(0x9000UL), true, true, out ulong pointer), "x64 table-pointer encoding failed.", failures);
Check(X64PageTableCodec.GetTableAddress(pointer).Value == 0x9000UL, "x64 table-pointer decode is incorrect.", failures);
Check(!X64PageTableCodec.TryEncodeTablePointer(new PhysicalAddress(0x9001UL), true, true, out _), "Misaligned x64 table pointer was accepted.", failures);

if (failures.Count != 0)
{
    foreach (string failure in failures) Console.Error.WriteLine($"[FAIL] {failure}");
    return 1;
}

Console.WriteLine("[ OK ] Virtual-memory contracts, canonical-address validation, x64 indices, leaf encoding, translation, and page-size rules passed.");
return 0;

static void Check(bool condition, string failure, List<string> failures)
{
    if (!condition) failures.Add(failure);
}
