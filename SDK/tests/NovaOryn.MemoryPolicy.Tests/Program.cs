string root = FindRepositoryRoot(AppContext.BaseDirectory);
List<string> failures = [];
string memoryEnums=Read(root,"src/NovaOryn.Memory.Contracts/MemoryEnums.cs");
foreach(string name in new[]{"UsableConventional","LoaderKernelImage","BootServices","RuntimeServices","AcpiReclaimable","AcpiNvs","Framebuffer","MemoryMappedIo","FirmwareReserved","BadMemory","PersistentMemory","BootStructures","PageTables","EarlyAllocatorAllocations"}) Require(memoryEnums.Contains(name,StringComparison.Ordinal),$"Memory contracts missing {name}.");
string physical=Read(root,"src/NovaOryn.Kernel.Memory/KernelPhysicalMemory.cs");
Require(physical.Contains("if (type != UefiConventionalMemory) continue;",StringComparison.Ordinal),"Early PMM must allocate only ConventionalMemory.");
string bootstrap=Read(root,"src/NovaOryn.Kernel.Memory/KernelPhysicalMemory.Bootstrap.cs");
Require(bootstrap.Contains("TryTakeBootstrapPageTable",StringComparison.Ordinal),"PMM must expose reserved bootstrap page-table consumption.");
string vmm=Read(root,"src/NovaOryn.Kernel.VirtualMemory/KernelVirtualMemory.cs")+Read(root,"src/NovaOryn.Kernel.VirtualMemory/KernelVirtualMemory.DirectMap.cs");
Require(vmm.Contains("KernelPhysicalMemory.TryAllocate",StringComparison.Ordinal),"VMM must allocate ordinary page tables through PMM after direct-map activation.");
Require(vmm.Contains("KernelPhysicalMemory.TryTakeBootstrapPageTable",StringComparison.Ordinal),"VMM must consume UEFI-reserved bootstrap page tables before direct-map activation.");
Require(vmm.Contains("AdoptPrivateRoot",StringComparison.Ordinal)&&vmm.Contains("Native.WritePageTableRoot",StringComparison.Ordinal),"VMM must adopt a NovaOryn-owned PML4.");
string address=Read(root,"src/NovaOryn.Kernel.AddressSpace/KernelAddressSpace.cs");
Require(address.Contains("InitializeDirectMap",StringComparison.Ordinal)&&address.Contains("DirectMapInitializationFailed",StringComparison.Ordinal),"Address-space initialization must establish the direct map before heap initialization.");
string heap=Read(root,"src/NovaOryn.Kernel.Heap/KernelHeap.cs");
Require(heap.Contains("KernelVirtualMemory",StringComparison.Ordinal)&&heap.Contains("KernelPhysicalMemory",StringComparison.Ordinal),"Kernel heap must be page-backed through VMM and PMM.");
Finish();


string Read(string root, string relative) => File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
void Require(bool condition, string message) { if (!condition) failures.Add(message); }
void Finish()
{
    if (failures.Count != 0) { foreach (string failure in failures) Console.Error.WriteLine($"[FAIL] {failure}"); Environment.Exit(1); }
    Console.WriteLine("[ OK ] " + Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? "policy") + " passed.");
}
static string FindRepositoryRoot(string start)
{
    DirectoryInfo? current = new(start);
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "NovaOryn.sln"))) return current.FullName;
        current = current.Parent;
    }
    throw new InvalidOperationException("NovaOryn repository root was not found.");
}
