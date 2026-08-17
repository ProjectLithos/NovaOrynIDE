string root = FindRepositoryRoot(AppContext.BaseDirectory);
List<string> failures = [];
string cpu=Read(root,"native/x64/Cpu.S");
Require(cpu.Contains("cli",StringComparison.Ordinal)&&cpu.Contains("hlt",StringComparison.Ordinal)&&cpu.Contains("jmp .LNovaOrynHaltForever",StringComparison.Ordinal),"Native halt must be CLI plus a repeating HLT loop.");
string entry=Read(root,"native/x64/Entry.asm");
Require(entry.Contains("NovaOrynCaptureFinalUefiMemoryMap",StringComparison.Ordinal),"UEFI entry must capture the final memory map.");
Require(entry.Contains("NovaOrynCaptureUefiAcpiRoot",StringComparison.Ordinal)&&entry.Contains("ConfigurationTable",StringComparison.Ordinal)&&entry.Contains("0x80",StringComparison.Ordinal),"UEFI entry must capture the ACPI RSDP from firmware configuration tables before ExitBootServices.");
Require(entry.Contains("NovaOrynBootstrapStack",StringComparison.Ordinal)&&entry.Contains("lea rsp, [rel NovaOrynBootstrapStackEnd]",StringComparison.Ordinal),"UEFI entry must switch to a NovaOryn-owned bootstrap stack.");
Require(entry.IndexOf("lea rsp, [rel NovaOrynBootstrapStackEnd]",StringComparison.Ordinal)<entry.IndexOf("call NovaOrynRuntimeInitialize",StringComparison.Ordinal),"Stack switch must precede managed runtime initialization.");
Require(entry.Contains("NovaOrynPlanBootstrapPageTables",StringComparison.Ordinal)&&entry.Contains("EFI_BOOT_SERVICES.AllocatePages",StringComparison.Ordinal),"UEFI entry must reserve calculated page-table bootstrap storage before ExitBootServices.");
Require(entry.Contains("AllocateMaxAddress",StringComparison.Ordinal)&&entry.Contains("0x88",StringComparison.Ordinal)&&entry.Contains("0x000000000009F000",StringComparison.Ordinal),"UEFI entry must reserve a SIPI trampoline page below 1 MiB before final ExitBootServices capture.");
string kernel=Read(root,"src/NovaOryn.Kernel.Bootstrap/Kernel.cs");
foreach(string text in new[]{"NovaOryn KMain started.","GDT and TSS installed.","IDT with 256 vectors installed.","ACPI MADT, MCFG, HPET, FADT and platform power services online.","SMP and per-CPU state online.","Interactive console ready. Defaults: font 3, buffering 3. Userland: font get/set/list; buffering get/set/list."}) Require(kernel.Contains(text,StringComparison.Ordinal),$"Boot kernel must report: {text}");
Require(!kernel.Contains("DllImport",StringComparison.Ordinal)&&!kernel.Contains("WritePort8",StringComparison.Ordinal),"End-user kernel must remain high-level.");
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
