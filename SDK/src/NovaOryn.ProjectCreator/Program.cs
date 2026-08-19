using System.Linq;
using System.Text.Json;

return MainEntry(args);

static int MainEntry(string[] args)
{
    if (args.Length < 1 || !string.Equals(args[0], "create", StringComparison.OrdinalIgnoreCase))
    {
        return Fail("Usage: NovaOryn.ProjectCreator create [--output <directory>] [--sdk-root <directory>]");
    }

    string sdkRoot = Path.GetFullPath(GetOption(args, "--sdk-root") ?? FindSdkRoot(AppContext.BaseDirectory));
    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    string output = Path.GetFullPath(GetOption(args, "--output") ?? Path.Combine(userProfile, "Source", "Repos", "NovaOrynKernel"));
    string template = Path.Combine(sdkRoot, "templates", "NovaOrynKernel");
    if (!Directory.Exists(template)) return Fail($"Kernel project template was not found: {template}");

    Directory.CreateDirectory(output);
    string? mainProjectPath = ResolveMainProjectPath(output);
    if (mainProjectPath is null) return 1;
    if (!MigrateLegacyRootKernel(output)) return 1;
    if (!MigrateKernelBootstrapContract(output)) return 1;
    if (!MigrateIdeGeneratedMinimalKernel(output, template)) return 1;
    if (!MigrateUnsupportedFreestandingFormatting(output)) return 1;
    if (!MigrateAddressSpaceDiagnostics(output)) return 1;
    if (!MigrateGeneratedFramebufferFontSize(output)) return 1;
    if (!MigrateHeapDiagnostics(output)) return 1;
    if (!MigrateGeneratedInteractiveConsole(output)) return 1;
    if (!RemoveSdkOwnedLegacyTrees(output)) return 1;

    foreach (string source in Directory.EnumerateFiles(template, "*", SearchOption.AllDirectories))
    {
        string relative = Path.GetRelativePath(template, source);
        bool isConfigurationFile = string.Equals(relative, "NovaOryn.Configuration.json", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(relative, "NovaOryn.Configuration.props", StringComparison.OrdinalIgnoreCase);
        if (isConfigurationFile && File.Exists(Path.Combine(output, relative))) continue;
        if (string.Equals(relative, "NovaOrynProject.json", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(relative, "NovaOrynKernel.csproj", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(relative, "NovaOrynKernel.sln", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        string destination = Path.Combine(output, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        // Boot and HAL are SDK-owned generated source and must refresh with the SDK. Keeping
        // them user-owned left existing OSes on obsolete startup/runtime contracts. The user's
        // high-level Kernel\Kernel.cs remains protected exactly as before.
        bool userOwnedTree = relative.StartsWith("Kernel" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            relative.StartsWith("KernelProjects" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            relative.StartsWith("Userland" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
            relative.StartsWith("Tests" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

        if (userOwnedTree && File.Exists(destination))
        {
            // Kernel\Kernel.cs is the required user-owned entry source. A completely
            // empty file can be produced by a failed/partial Visual Studio template
            // instantiation and cannot represent a buildable user kernel. Re-seed only
            // that empty file; never replace non-empty user code.
            bool isUserKernel = string.Equals(
                relative,
                Path.Combine("Kernel", "Kernel.cs"),
                StringComparison.OrdinalIgnoreCase);

            if (!isUserKernel || !IsEmptyTextFile(destination)) continue;

            File.Copy(source, destination, true);
            Console.WriteLine($"[ OK ] Re-seeded empty user kernel from SDK template: {destination}");
            continue;
        }

        File.Copy(source, destination, true);
    }

    string mainProjectFileName = Path.GetFileName(mainProjectPath);
    if (string.IsNullOrWhiteSpace(mainProjectFileName)) return Fail($"Kernel project filename is invalid: {mainProjectPath}");
    File.Copy(Path.Combine(template, "NovaOrynKernel.csproj"), mainProjectPath, true);
    string entryProjectPath = Path.Combine(output, "Sdk", "NovaOryn.Kernel.Entry.X64", "NovaOryn.Kernel.Entry.X64.csproj");
    string entryProject = File.ReadAllText(entryProjectPath);
    entryProject = entryProject.Replace(
        Path.Combine("..", "..", "NovaOrynKernel.csproj"),
        Path.Combine("..", "..", mainProjectFileName),
        StringComparison.OrdinalIgnoreCase);
    File.WriteAllText(entryProjectPath, entryProject);

    string solutionPath = ResolveSolutionPath(output);
    if (!File.Exists(solutionPath))
    {
        string solution = File.ReadAllText(Path.Combine(template, "NovaOrynKernel.sln"));
        solution = solution.Replace("NovaOrynKernel.csproj", mainProjectFileName, StringComparison.Ordinal);
        solution = solution.Replace("\"NovaOrynKernel\"", $"\"{Path.GetFileNameWithoutExtension(mainProjectPath)}\"", StringComparison.Ordinal);
        File.WriteAllText(solutionPath, solution);
    }

    string manifestPath = Path.Combine(output, "NovaOrynProject.json");
    string targetArchitecture = "x64";
    string kernelModel = "Monolithic";
    string bootProtocol = "Uefi";
    string runtimePack = "NovaOryn.RuntimePack.X64.Bootstrap";
    string projectFile = "Sdk/NovaOryn.Kernel.Entry.X64/NovaOryn.Kernel.Entry.X64.csproj";
    string[] workAreas = new[] { "HAL", "Drivers", "Storage", "Filesystems", "Networking", "USB", "Input", "Processes", "Scheduler", "System Calls", "Security", "Diagnostics" };

    if (File.Exists(manifestPath))
    {
        try
        {
            using JsonDocument existingManifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
            JsonElement root = existingManifest.RootElement;
            if (root.TryGetProperty("TargetArchitecture", out JsonElement architectureElement) && !string.IsNullOrWhiteSpace(architectureElement.GetString()))
                targetArchitecture = architectureElement.GetString()!;
            if (root.TryGetProperty("KernelModel", out JsonElement modelElement) && !string.IsNullOrWhiteSpace(modelElement.GetString()))
                kernelModel = modelElement.GetString()!;
            if (root.TryGetProperty("BootProtocol", out JsonElement bootElement) && !string.IsNullOrWhiteSpace(bootElement.GetString()))
                bootProtocol = bootElement.GetString()!;
            if (root.TryGetProperty("RuntimePack", out JsonElement runtimeElement) && !string.IsNullOrWhiteSpace(runtimeElement.GetString()))
                runtimePack = runtimeElement.GetString()!;
            if (root.TryGetProperty("ProjectFile", out JsonElement projectElement) && !string.IsNullOrWhiteSpace(projectElement.GetString()))
                projectFile = projectElement.GetString()!;
            if (root.TryGetProperty("WorkAreas", out JsonElement workElement) && workElement.ValueKind == JsonValueKind.Array)
                workAreas = workElement.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        }
        catch (JsonException)
        {
            // A malformed manifest is replaced with safe defaults below.
        }
    }

    // NovaOryn.Configuration.json is authoritative for Visual Studio-generated projects.
    // NovaOryn IDE projects additionally carry NovaOryn.json; it is the IDE-authoritative
    // configuration and is applied after compatibility metadata so stale manifests/props
    // can never silently revert Microkernel or Hybrid to Monolithic during refresh.
    string configurationPath = Path.Combine(output, "NovaOryn.Configuration.json");
    string[] developmentAreas = Array.Empty<string>();
    if (File.Exists(configurationPath))
    {
        try
        {
            using JsonDocument configurationDocument = JsonDocument.Parse(File.ReadAllText(configurationPath));
            JsonElement configurationRoot = configurationDocument.RootElement;
            if (configurationRoot.TryGetProperty("Architecture", out JsonElement architectureElement) && !string.IsNullOrWhiteSpace(architectureElement.GetString()))
                targetArchitecture = architectureElement.GetString()!;
            if (configurationRoot.TryGetProperty("KernelModel", out JsonElement modelElement) && !string.IsNullOrWhiteSpace(modelElement.GetString()))
                kernelModel = modelElement.GetString()!;
            if (configurationRoot.TryGetProperty("BootProtocol", out JsonElement bootElement) && !string.IsNullOrWhiteSpace(bootElement.GetString()))
                bootProtocol = bootElement.GetString()!;
            if (configurationRoot.TryGetProperty("WorkAreas", out JsonElement developmentElement) && developmentElement.ValueKind == JsonValueKind.Array)
                developmentAreas = developmentElement.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            string[] availableAreas = new[] { "Shell", "GUI", "Drivers", "HAL", "Audio", "Filesystems", "Storage", "Networking", "USB", "Input", "Processes", "Scheduler", "System Calls", "Security", "Diagnostics", "Tests" };
            workAreas = availableAreas.Where(area => !developmentAreas.Contains(area, StringComparer.OrdinalIgnoreCase)).ToArray();
        }
        catch (JsonException)
        {
            Console.Error.WriteLine($"[FAIL] NovaOryn configuration is malformed: {configurationPath}");
            return 1;
        }
    }


    string ideConfigurationPath = Path.Combine(output, "NovaOryn.json");
    if (File.Exists(ideConfigurationPath))
    {
        try
        {
            using JsonDocument ideConfigurationDocument = JsonDocument.Parse(File.ReadAllText(ideConfigurationPath));
            JsonElement ideRoot = ideConfigurationDocument.RootElement;
            if (ideRoot.TryGetProperty("targetArchitecture", out JsonElement architectureElement) && !string.IsNullOrWhiteSpace(architectureElement.GetString()))
            {
                string configured = architectureElement.GetString()!;
                targetArchitecture = string.Equals(configured, "x86_64", StringComparison.OrdinalIgnoreCase) ? "x64" : configured;
            }
            if (ideRoot.TryGetProperty("kernelArchitecture", out JsonElement modelElement) && !string.IsNullOrWhiteSpace(modelElement.GetString()))
            {
                string configured = modelElement.GetString()!;
                kernelModel = string.Equals(configured, "microkernel", StringComparison.OrdinalIgnoreCase) ? "Microkernel"
                    : string.Equals(configured, "hybrid", StringComparison.OrdinalIgnoreCase) ? "Hybrid" : "Monolithic";
            }
            if (ideRoot.TryGetProperty("bootArchitecture", out JsonElement bootElement) && !string.IsNullOrWhiteSpace(bootElement.GetString()))
            {
                string configured = bootElement.GetString()!;
                bootProtocol = string.Equals(configured, "uefi", StringComparison.OrdinalIgnoreCase) ? "Uefi" : configured;
            }
        }
        catch (JsonException)
        {
            Console.Error.WriteLine($"[FAIL] NovaOryn IDE configuration is malformed: {ideConfigurationPath}");
            return 1;
        }
    }

    File.WriteAllText(manifestPath, JsonSerializer.Serialize(new
    {
        Name = "MinimalKernel",
        ProjectFile = projectFile,
        TargetArchitecture = targetArchitecture,
        BootProtocol = bootProtocol,
        KernelEntry = "KMain",
        RuntimePack = runtimePack,
        OutputDirectory = Path.Combine(sdkRoot, "Artifacts", "MinimalKernel"),
        KernelModel = kernelModel,
        ConfigurationFile = "NovaOryn.Configuration.json",
        WorkAreas = workAreas,
        DevelopmentAreas = developmentAreas
    }, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);

    if (!EnsureRequiredUserKernel(output, template)) return 1;

    Console.WriteLine($"[ OK ] C# kernel project: {output}");
    Console.WriteLine($"[ OK ] User kernel     : {Path.Combine(output, "Kernel", "Kernel.cs")}");
    Console.WriteLine($"[ OK ] Kernel project  : {mainProjectPath}");
    Console.WriteLine($"[ OK ] Kernel solution : {solutionPath}");
    Console.WriteLine($"[ OK ] Project manifest: {manifestPath}");
    return 0;
}

static bool IsEmptyTextFile(string path)
{
    FileInfo info = new(path);
    if (info.Length == 0) return true;

    string source = File.ReadAllText(path);
    return string.IsNullOrWhiteSpace(source);
}

static bool EnsureRequiredUserKernel(string output, string template)
{
    string kernelPath = Path.Combine(output, "Kernel", "Kernel.cs");
    string templateKernelPath = Path.Combine(template, "Kernel", "Kernel.cs");

    if (!File.Exists(templateKernelPath))
    {
        Console.Error.WriteLine($"[FAIL] SDK high-level user kernel template is missing: {templateKernelPath}");
        return false;
    }

    string canonicalSource = File.ReadAllText(templateKernelPath);
    if (string.IsNullOrWhiteSpace(canonicalSource))
    {
        Console.Error.WriteLine($"[FAIL] SDK high-level user kernel template is empty: {templateKernelPath}");
        return false;
    }

    bool needsRepair = !File.Exists(kernelPath);
    if (!needsRepair)
    {
        string currentSource = File.ReadAllText(kernelPath);
        needsRepair = string.IsNullOrWhiteSpace(currentSource);
    }

    if (needsRepair)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(kernelPath)!);
        File.WriteAllText(kernelPath, canonicalSource);
        Console.WriteLine($"[ OK ] Repaired empty required user kernel from SDK template: {kernelPath}");
    }

    string verifiedSource = File.ReadAllText(kernelPath);
    if (string.IsNullOrWhiteSpace(verifiedSource))
    {
        Console.Error.WriteLine($"[FAIL] Required user kernel source is still empty after repair: {kernelPath}");
        return false;
    }

    Console.WriteLine($"[ OK ] User kernel source verified: {kernelPath} ({new FileInfo(kernelPath).Length} bytes)");
    return true;
}

static string? ResolveMainProjectPath(string output)
{
    string[] candidates = Directory.EnumerateFiles(output, "*.csproj", SearchOption.TopDirectoryOnly).ToArray();
    if (candidates.Length == 0) return Path.Combine(output, "NovaOrynKernel.csproj");
    if (candidates.Length == 1) return candidates[0];
    Console.Error.WriteLine($"[FAIL] More than one root kernel project exists in {output}: {string.Join(", ", candidates.Select(candidate => Path.GetFileName(candidate)))}");
    return null;
}

static string ResolveSolutionPath(string output)
{
    string? existing = Directory.EnumerateFiles(output, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
    return existing ?? Path.Combine(output, "NovaOrynKernel.sln");
}

static bool MigrateLegacyRootKernel(string output)
{
    string legacyRootKernel = Path.Combine(output, "Kernel.cs");
    if (!File.Exists(legacyRootKernel)) return true;

    if (!IsSdkGeneratedLowLevelKernel(legacyRootKernel))
    {
        Console.Error.WriteLine($"[FAIL] A user-owned legacy root Kernel.cs prevents migration: {legacyRootKernel}");
        Console.Error.WriteLine("[FAIL] Move that file to Kernel\\Kernel.cs or remove it before refreshing the SDK project.");
        return false;
    }

    File.Delete(legacyRootKernel);
    Console.WriteLine($"[ OK ] Removed generated legacy root kernel: {legacyRootKernel}");
    return true;
}

static bool MigrateKernelBootstrapContract(string output)
{
    string kernelPath = Path.Combine(output, "Kernel", "Kernel.cs");
    if (!File.Exists(kernelPath)) return true;

    string source = File.ReadAllText(kernelPath);
    const string canonicalNamespace = "NovaOryn.Kernel.Bootstrap";

    // Kernel\\Kernel.cs is user-owned. Refresh may repair only the stable public
    // entry contract required by KernelEntry: NovaOryn.Kernel.Bootstrap.Kernel
    // with KMain(BootContext). Never replace the user's method body.
    bool hasKMain = source.Contains("KMain", StringComparison.Ordinal);
    bool hasBootContext = source.Contains("BootContext", StringComparison.Ordinal);
    if (!hasKMain || !hasBootContext)
    {
        Console.Error.WriteLine($"[FAIL] User kernel does not expose the required Kernel.KMain(BootContext) contract: {kernelPath}");
        return false;
    }

    bool changed = false;
    string previousNamespace = "<global>";
    int namespaceStart = source.IndexOf("namespace ", StringComparison.Ordinal);
    if (namespaceStart >= 0)
    {
        int nameStart = namespaceStart + "namespace ".Length;
        while (nameStart < source.Length && Char.IsWhiteSpace(source[nameStart])) nameStart++;
        int nameEnd = nameStart;
        while (nameEnd < source.Length)
        {
            char current = source[nameEnd];
            if (current == ';' || current == '{' || Char.IsWhiteSpace(current)) break;
            nameEnd++;
        }
        if (nameEnd <= nameStart)
        {
            Console.Error.WriteLine($"[FAIL] User kernel namespace declaration is malformed: {kernelPath}");
            return false;
        }

        previousNamespace = source.Substring(nameStart, nameEnd - nameStart);
        if (!string.Equals(previousNamespace, canonicalNamespace, StringComparison.Ordinal))
        {
            source = source.Substring(0, nameStart) + canonicalNamespace + source.Substring(nameEnd);
            changed = true;
        }
    }
    else
    {
        // File-scoped namespace can safely follow top-level using directives. Insert
        // it immediately before the first type declaration so existing usings remain valid.
        int typeInsertion = FindKernelTypeDeclarationStart(source);
        if (typeInsertion < 0)
        {
            Console.Error.WriteLine($"[FAIL] User kernel contains no class declaration that can host KMain: {kernelPath}");
            return false;
        }
        source = source.Insert(typeInsertion, $"namespace {canonicalNamespace};{Environment.NewLine}{Environment.NewLine}");
        changed = true;
    }

    int classKeyword = FindClassKeywordForKMain(source);
    if (classKeyword < 0)
    {
        Console.Error.WriteLine($"[FAIL] User kernel contains KMain but no class declaration could be resolved safely: {kernelPath}");
        return false;
    }

    int classNameStart = classKeyword + "class".Length;
    while (classNameStart < source.Length && Char.IsWhiteSpace(source[classNameStart])) classNameStart++;
    int classNameEnd = classNameStart;
    while (classNameEnd < source.Length && (Char.IsLetterOrDigit(source[classNameEnd]) || source[classNameEnd] == '_')) classNameEnd++;
    if (classNameEnd <= classNameStart)
    {
        Console.Error.WriteLine($"[FAIL] User kernel class declaration is malformed: {kernelPath}");
        return false;
    }

    string previousClassName = source.Substring(classNameStart, classNameEnd - classNameStart);
    if (!string.Equals(previousClassName, "Kernel", StringComparison.Ordinal))
    {
        source = source.Substring(0, classNameStart) + "Kernel" + source.Substring(classNameEnd);
        changed = true;
    }

    // KernelEntry performs a static call, so reject a non-static entry class here with
    // a useful error rather than allowing a later CS0120/CS0234 from the entry project.
    int declarationLineStart = source.LastIndexOf('\n', classKeyword);
    declarationLineStart = declarationLineStart < 0 ? 0 : declarationLineStart + 1;
    string declarationPrefix = source.Substring(declarationLineStart, classKeyword - declarationLineStart);
    if (!declarationPrefix.Contains("static", StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"[FAIL] User kernel entry class must be static: {kernelPath}");
        Console.Error.WriteLine("[FAIL] Declare the entry type as public static class Kernel.");
        return false;
    }

    if (changed)
    {
        File.WriteAllText(kernelPath, source);
        Console.WriteLine($"[ OK ] Migrated user kernel bootstrap contract: namespace {previousNamespace} -> {canonicalNamespace}; class {previousClassName} -> Kernel");
    }

    if (!source.Contains("namespace " + canonicalNamespace, StringComparison.Ordinal) ||
        !source.Contains("class Kernel", StringComparison.Ordinal) ||
        !source.Contains("KMain", StringComparison.Ordinal) ||
        !source.Contains("BootContext", StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"[FAIL] User kernel bootstrap contract verification failed after migration: {kernelPath}");
        return false;
    }

    Console.WriteLine($"[ OK ] User kernel bootstrap contract verified: {canonicalNamespace}.Kernel.KMain(BootContext)");
    return true;
}

static int FindKernelTypeDeclarationStart(string source)
{
    int classIndex = source.IndexOf("class ", StringComparison.Ordinal);
    if (classIndex < 0) return -1;
    int lineStart = source.LastIndexOf('\n', classIndex);
    return lineStart < 0 ? 0 : lineStart + 1;
}

static int FindClassKeywordForKMain(string source)
{
    int kMainIndex = source.IndexOf("KMain", StringComparison.Ordinal);
    if (kMainIndex < 0) return -1;

    int search = kMainIndex;
    while (search >= 0)
    {
        int candidate = source.LastIndexOf("class", search, StringComparison.Ordinal);
        if (candidate < 0) return -1;

        bool leftBoundary = candidate == 0 || !(Char.IsLetterOrDigit(source[candidate - 1]) || source[candidate - 1] == '_');
        int after = candidate + "class".Length;
        bool rightBoundary = after >= source.Length || Char.IsWhiteSpace(source[after]);
        if (leftBoundary && rightBoundary) return candidate;
        search = candidate - 1;
    }

    return -1;
}

static bool MigrateIdeGeneratedMinimalKernel(string output, string template)
{
    string kernelPath = Path.Combine(output, "Kernel", "Kernel.cs");
    if (!File.Exists(kernelPath)) return true;

    string source = File.ReadAllText(kernelPath);
    if (!IsIdeGeneratedMinimalKernel(source)) return true;

    string templateKernelPath = Path.Combine(template, "Kernel", "Kernel.cs");
    if (!File.Exists(templateKernelPath))
    {
        Console.Error.WriteLine($"[FAIL] Full NovaOryn kernel bootstrap template is missing: {templateKernelPath}");
        return false;
    }

    string replacement = File.ReadAllText(templateKernelPath);
    if (string.IsNullOrWhiteSpace(replacement) ||
        !replacement.Contains("BootStartup.Initialize(boot)", StringComparison.Ordinal) ||
        !replacement.Contains("HardwareAbstractionLayer.Initialize()", StringComparison.Ordinal) ||
        !replacement.Contains("KernelCommandLine.Initialize()", StringComparison.Ordinal))
    {
        Console.Error.WriteLine($"[FAIL] Full NovaOryn kernel bootstrap template is incomplete: {templateKernelPath}");
        return false;
    }

    File.WriteAllText(kernelPath, replacement);
    Console.WriteLine($"[ OK ] Migrated IDE-generated minimal Kernel.cs to the full NovaOryn runtime bootstrap: {kernelPath}");
    return true;
}

static bool IsIdeGeneratedMinimalKernel(string source)
{
    // This recognises only the small Kernel.cs emitted by NovaOryn IDE 0.4.2 and
    // earlier.  User-written kernels are deliberately left untouched.
    if (source.Length > 2200) return false;
    string[] required = new[]
    {
        "public static Boolean KMain(BootContext boot)",
        "private const UInt32 ConsoleFontSize = 32U;",
        "KernelConsole.Initialize(boot, ConsoleFontSize)",
        "boot.HasFinalMemoryMap()",
        "KernelPlatform.InitializeDescriptors()",
        "KernelPlatform.InitializeInterrupts()",
        "KernelPlatform.DisableLegacyPic()",
        "return KernelPlatform.Halt();"
    };
    foreach (string item in required)
        if (!source.Contains(item, StringComparison.Ordinal)) return false;

    string[] fullRuntimeMarkers = new[]
    {
        "BootStartup.Initialize(boot)",
        "HardwareAbstractionLayer.Initialize()",
        "KernelSystemCalls.Initialize()",
        "KernelScheduler.Initialize()",
        "KernelProcesses.Initialize()",
        "KernelConsole.RunInteractive()"
    };
    foreach (string item in fullRuntimeMarkers)
        if (source.Contains(item, StringComparison.Ordinal)) return false;

    return true;
}

static bool MigrateUnsupportedFreestandingFormatting(string output)
{
    string kernelPath = Path.Combine(output, "Kernel", "Kernel.cs");
    if (!File.Exists(kernelPath)) return true;

    string source = File.ReadAllText(kernelPath);
    const string generatedStatistics =
        "        var stats = KernelVirtualMemory.GetStatistics();\r\n" +
        "        KernelConsole.WriteLine($\"Virtual memory statistics: {stats}\");\r\n";
    const string generatedStatisticsLf =
        "        var stats = KernelVirtualMemory.GetStatistics();\n" +
        "        KernelConsole.WriteLine($\"Virtual memory statistics: {stats}\");\n";
    const string safeReplacement =
        "        if (!KernelConsole.WriteLine(\"Virtual memory manager statistics are available through KernelVirtualMemory.GetStatistics().\")) return false;";

    string migrated = source.Replace(generatedStatistics, safeReplacement + Environment.NewLine, StringComparison.Ordinal)
        .Replace(generatedStatisticsLf, safeReplacement + "\n", StringComparison.Ordinal);
    if (string.Equals(source, migrated, StringComparison.Ordinal)) return true;

    File.WriteAllText(kernelPath, migrated);
    Console.WriteLine($"[ OK ] Migrated generated virtual-memory statistics output to freestanding-safe text: {kernelPath}");
    return true;
}

static bool MigrateAddressSpaceDiagnostics(string output)
{
    string kernelPath = Path.Combine(output, "Kernel", "Kernel.cs");
    if (!File.Exists(kernelPath)) return true;

    string source = File.ReadAllText(kernelPath);
    const string oldCrLf =
        "        if (!KernelAddressSpace.Initialize()) return false;\r\n" +
        "        if (!KernelConsole.WriteLine(\"Kernel address-space policy initialized.\")) return false;\r\n";
    const string oldLf =
        "        if (!KernelAddressSpace.Initialize()) return false;\n" +
        "        if (!KernelConsole.WriteLine(\"Kernel address-space policy initialized.\")) return false;\n";
    const string replacement =
        "        Boolean addressSpaceReady = KernelAddressSpace.Initialize();\n" +
        "        if (!KernelConsole.Write(\"Kernel address-space status: \")) return false;\n" +
        "        if (!KernelConsole.WriteLine(KernelAddressSpace.GetLastStatusName())) return false;\n" +
        "        if (!addressSpaceReady) return false;\n" +
        "        if (!KernelConsole.Write(\"Kernel image base: \")) return false;\n" +
        "        if (!KernelConsole.WriteHex(KernelAddressSpace.KernelImageBase)) return false;\n" +
        "        if (!KernelConsole.WriteLine(\"\")) return false;\n" +
        "        if (!KernelConsole.Write(\"Kernel heap base: \")) return false;\n" +
        "        if (!KernelConsole.WriteHex(KernelAddressSpace.KernelHeapBase)) return false;\n" +
        "        if (!KernelConsole.WriteLine(\"\")) return false;\n" +
        "        if (!KernelConsole.Write(\"Kernel stacks base: \")) return false;\n" +
        "        if (!KernelConsole.WriteHex(KernelAddressSpace.KernelStacksBase)) return false;\n" +
        "        if (!KernelConsole.WriteLine(\"\")) return false;\n" +
        "        if (!KernelConsole.Write(\"Direct map base: \")) return false;\n" +
        "        if (!KernelConsole.WriteHex(KernelAddressSpace.DirectMapBase)) return false;\n" +
        "        if (!KernelConsole.WriteLine(\"\")) return false;\n" +
        "        if (!KernelConsole.Write(\"MMIO base: \")) return false;\n" +
        "        if (!KernelConsole.WriteHex(KernelAddressSpace.MmioBase)) return false;\n" +
        "        if (!KernelConsole.WriteLine(\"\")) return false;\n" +
        "        if (!KernelConsole.Write(\"Page-table window: \")) return false;\n" +
        "        if (!KernelConsole.WriteHex(KernelAddressSpace.PageTableWindowBase)) return false;\n" +
        "        if (!KernelConsole.WriteLine(\"\")) return false;\n";

    string migrated = source.Replace(oldCrLf, replacement.Replace("\n", "\r\n", StringComparison.Ordinal), StringComparison.Ordinal)
        .Replace(oldLf, replacement, StringComparison.Ordinal);
    if (string.Equals(source, migrated, StringComparison.Ordinal)) return true;

    File.WriteAllText(kernelPath, migrated);
    Console.WriteLine($"[ OK ] Migrated generated kernel address-space diagnostics: {kernelPath}");
    return true;
}

static bool RemoveSdkOwnedLegacyTrees(string output)
{
    foreach (string relative in new[] { "Console", "Runtime" })
    {
        string path = Path.Combine(output, relative);
        if (!Directory.Exists(path)) continue;
        Directory.Delete(path, true);
        Console.WriteLine($"[ OK ] Refreshed SDK-owned project tree: {path}");
    }
    return true;
}

static bool MigrateGeneratedInteractiveConsole(string output)
{
    string kernelPath = Path.Combine(output, "Kernel", "Kernel.cs");
    if (!File.Exists(kernelPath)) return true;
    string source = File.ReadAllText(kernelPath);
    const string oldCrLf = "        if (!KernelConsole.WriteLine(\"CPU halted.\")) return false;\r\n        return KernelPlatform.Halt();";
    const string oldLf = "        if (!KernelConsole.WriteLine(\"CPU halted.\")) return false;\n        return KernelPlatform.Halt();";
    const string replacementLf = "        if (!KernelConsole.WriteLine(\"Interactive console ready. Defaults: font 3, buffering auto (double for text). Userland: font get/set/list; buffering get/set/list.\")) return false;\n        return KernelConsole.RunInteractive();";
    string migrated = source;
    if (source.Contains(oldCrLf, StringComparison.Ordinal)) migrated = source.Replace(oldCrLf, replacementLf.Replace("\n", "\r\n", StringComparison.Ordinal), StringComparison.Ordinal);
    else if (source.Contains(oldLf, StringComparison.Ordinal)) migrated = source.Replace(oldLf, replacementLf, StringComparison.Ordinal);
    if (string.Equals(source, migrated, StringComparison.Ordinal)) return true;
    File.WriteAllText(kernelPath, migrated);
    Console.WriteLine($"[ OK ] Migrated generated terminal CPU halt to interactive console input loop: {kernelPath}");
    return true;
}

static bool MigrateGeneratedFramebufferFontSize(string output)
{
    string kernelPath = Path.Combine(output, "Kernel", "Kernel.cs");
    if (!File.Exists(kernelPath)) return true;
    string source = File.ReadAllText(kernelPath);
    if (!source.Contains("Editable kernel: change this line and rebuild.", StringComparison.Ordinal) ||
        !source.Contains("Kernel address-space status: ", StringComparison.Ordinal) ||
        !source.Contains("private const UInt32 ConsoleFontSize = 32U;", StringComparison.Ordinal)) return true;
    string migrated = source.Replace("private const UInt32 ConsoleFontSize = 32U;", "private const UInt32 ConsoleFontSize = 16U;", StringComparison.Ordinal);
    File.WriteAllText(kernelPath, migrated);
    Console.WriteLine($"[ OK ] Migrated generated framebuffer font default from 32 pixels to 16 pixels: {kernelPath}");
    return true;
}

static bool MigrateHeapDiagnostics(string output)
{
    string kernelPath = Path.Combine(output, "Kernel", "Kernel.cs");
    if (!File.Exists(kernelPath)) return true;
    string source = File.ReadAllText(kernelPath);
    if (source.Contains("KernelHeap.Initialize()", StringComparison.Ordinal)) return true;
    const string markerCrLf = "        if (!KernelConsole.WriteLine(\"CPU halted.\")) return false;\r\n";
    const string markerLf = "        if (!KernelConsole.WriteLine(\"CPU halted.\")) return false;\n";
    if (!source.Contains("Kernel address-space status: ", StringComparison.Ordinal) ||
        !source.Contains("KernelConsole.WriteHex(KernelAddressSpace.PageTableWindowBase)", StringComparison.Ordinal)) return true;
    const string block =
        "        if (!KernelEarlyAllocator.Initialize()) return false;\n" +
        "        if (!KernelEarlyAllocator.TryAllocate(256UL, 16UL, out UInt64 earlyAddress)) return false;\n" +
        "        if (!KernelConsole.Write(\"Early allocator sample: \")) return false;\n" +
        "        if (!KernelConsole.WriteHex(earlyAddress)) return false;\n" +
        "        if (!KernelConsole.WriteLine(\"\")) return false;\n" +
        "        Boolean heapReady = KernelHeap.Initialize();\n" +
        "        if (!KernelConsole.Write(\"Kernel heap status: \")) return false;\n" +
        "        if (!KernelConsole.WriteLine(KernelHeap.GetLastStatusName())) return false;\n" +
        "        if (!heapReady) return false;\n" +
        "        if (!KernelHeap.TryAllocate(256UL, 16UL, true, out KernelHeapAllocation heapSample)) return false;\n" +
        "        if (!KernelConsole.Write(\"Kernel heap sample: \")) return false;\n" +
        "        if (!KernelConsole.WriteHex(heapSample.Address)) return false;\n" +
        "        if (!KernelConsole.WriteLine(\"\")) return false;\n" +
        "        if (!KernelHeap.TryRelease(heapSample)) return false;\n";
    string migrated = source;
    if (source.Contains(markerCrLf, StringComparison.Ordinal)) migrated = source.Replace(markerCrLf, block.Replace("\n", "\r\n", StringComparison.Ordinal) + markerCrLf, StringComparison.Ordinal);
    else if (source.Contains(markerLf, StringComparison.Ordinal)) migrated = source.Replace(markerLf, block + markerLf, StringComparison.Ordinal);
    if (string.Equals(source, migrated, StringComparison.Ordinal)) return true;
    if (!migrated.Contains("using NovaOryn.Kernel.Heap;", StringComparison.Ordinal))
        migrated = migrated.Replace("using NovaOryn.Kernel.AddressSpace;", "using NovaOryn.Kernel.AddressSpace;" + Environment.NewLine + "using NovaOryn.Kernel.Heap;", StringComparison.Ordinal);
    File.WriteAllText(kernelPath, migrated);
    Console.WriteLine($"[ OK ] Migrated generated kernel early-allocator and heap diagnostics: {kernelPath}");
    return true;
}

static bool IsSdkGeneratedLowLevelKernel(string path)
{
    string source = File.ReadAllText(path);
    bool exposesNativeInterop = source.Contains("DllImport", StringComparison.Ordinal) &&
        source.Contains("WritePort8", StringComparison.Ordinal) &&
        source.Contains("NovaOrynX64", StringComparison.Ordinal);
    bool monolithicConsole = source.Contains("FramebufferConsole", StringComparison.Ordinal) &&
        source.Contains("InitializeSerial", StringComparison.Ordinal) &&
        source.Contains("WriteLineDescriptors", StringComparison.Ordinal);
    bool exportedBootstrap = source.Contains("RuntimeExport", StringComparison.Ordinal) &&
        source.Contains("NovaOrynManagedEntry", StringComparison.Ordinal) &&
        source.Contains("KMain", StringComparison.Ordinal);
    return exposesNativeInterop && monolithicConsole && exportedBootstrap;
}

static string FindSdkRoot(string start)
{
    DirectoryInfo? directory = new(start);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "NovaOryn.sln"))) return directory.FullName;
        directory = directory.Parent;
    }
    throw new DirectoryNotFoundException("NovaOryn SDK root was not found.");
}

static string? GetOption(string[] args, string name)
{
    for (int index = 0; index + 1 < args.Length; index++)
    {
        if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase)) return args[index + 1];
    }
    return null;
}

static int Fail(string message)
{
    Console.Error.WriteLine($"[FAIL] {message}");
    return 1;
}
