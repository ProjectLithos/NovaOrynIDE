using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using NovaOryn.ProjectModel;

return MainEntry(args);

static int MainEntry(string[] args)
{
    if (args.Length < 2 || !string.Equals(args[0], "compile", StringComparison.OrdinalIgnoreCase))
    {
        return Fail("Usage: NovaOryn.ManagedCompiler compile <NovaOrynProject.json> [--dotnet <path>] [--ilc <path>] [--configuration Debug|Release] [--sdk-root <path>] [--dry-run]");
    }

    if (!NovaOrynProject.TryLoad(args[1], out NovaOrynProject? project, out string error) || project is null)
    {
        return Fail(error);
    }

    string dotnet = GetOption(args, "--dotnet") ?? "dotnet";
    string configuration = GetOption(args, "--configuration") ?? "Release";
    bool debugBuild = string.Equals(configuration, "Debug", StringComparison.OrdinalIgnoreCase);
    bool dryRun = HasOption(args, "--dry-run");
    string repositoryRoot = Path.GetFullPath(GetOption(args, "--sdk-root") ?? FindRepositoryRoot(Path.GetDirectoryName(project.ProjectFile)!));
    string ilc = GetOption(args, "--ilc") ?? FindIlc(repositoryRoot);

    string managedOutput = Path.Combine(project.OutputDirectory, "ManagedIL");
    string nativeOutput = Path.Combine(project.OutputDirectory, "NativeAot");
    RecreateDirectory(managedOutput);
    RecreateDirectory(nativeOutput);

    List<string> buildArguments =
    [
        "build", project.ProjectFile,
        "--configuration", configuration,
        "--output", managedOutput,
        "--nologo",
        "-p:PublishAot=false",
        "-p:SelfContained=false"
    ];
    if (debugBuild)
    {
        buildArguments.Add("-p:DebugSymbols=true");
        buildArguments.Add("-p:DebugType=portable");
        buildArguments.Add("-p:Optimize=false");
    }

    Console.WriteLine($"[INFO] Compiling {project.Name} C# source to managed IL.");
    Console.WriteLine("[INFO] The bootstrap project has no standard library and no runtime identifier.");
    if (dryRun)
    {
        PrintCommand(dotnet, buildArguments);
        return 0;
    }

    int exitCode = Run(dotnet, buildArguments, Path.GetDirectoryName(project.ProjectFile)!);
    if (exitCode != 0)
    {
        return Fail($"NovaOryn bootstrap C# compilation failed with exit code {exitCode}.");
    }

    string assemblyName = GetAssemblyName(project.ProjectFile);
    string systemModule = GetProjectProperty(project.ProjectFile, "NovaOrynSystemModule") ?? assemblyName;
    string managedAssembly = Path.Combine(managedOutput, assemblyName + ".dll");
    if (!File.Exists(managedAssembly))
    {
        return Fail($"Roslyn did not produce the bootstrap IL assembly: {managedAssembly}");
    }

    string systemModuleAssembly = Path.Combine(managedOutput, systemModule + ".dll");
    if (!File.Exists(systemModuleAssembly))
    {
        return Fail($"Roslyn did not produce the configured freestanding system module: {systemModuleAssembly}");
    }

    string[] managedInputs = Directory
        .GetFiles(managedOutput, "*.dll", SearchOption.TopDirectoryOnly)
        .Select(Path.GetFullPath)
        .OrderBy(path => string.Equals(path, managedAssembly, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
        .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    string nativeObject = Path.Combine(nativeOutput, project.Name + ".obj");
    string ilcMap = Path.Combine(nativeOutput, project.Name + ".ilc.map");
    List<string> ilcArguments = [.. managedInputs];
    ilcArguments.AddRange(
    [
        $"-o:{nativeObject}",
        "--systemmodule", systemModule,
        "--targetos:win",
        "--targetarch:x64",
        "--nativelib",
        "--directpinvoke:*",
        $"--map:{ilcMap}",
        "--noscan",
        "--reflectiondata:none",
        "--nopreinitstatics"
    ]);
    if (debugBuild)
    {
        // Matches NativeAOT's official build integration: -g asks ILC to carry
        // managed sequence points into native CodeView debug records.
        ilcArguments.Add("-g");
    }

    Console.WriteLine($"[INFO] Compiling managed IL with the repository-pinned ILC host: {ilc}");
    Console.WriteLine("[INFO] targetos:win selects the PE/COFF ABI used by x64 UEFI; no Windows CoreLib or Windows runtime library is referenced.");
    Console.WriteLine("[INFO] The IL scanner is disabled for the no-GC bootstrap so ILC resolves only helpers reachable from KMain.");
    exitCode = Run(ilc, ilcArguments, repositoryRoot);
    if (exitCode != 0)
    {
        return Fail($"Direct ILC compilation failed with exit code {exitCode}.");
    }
    if (!File.Exists(nativeObject))
    {
        return Fail($"ILC did not produce the expected x64 COFF object: {nativeObject}");
    }

    string[] managedPdbs = Directory
        .GetFiles(managedOutput, "*.pdb", SearchOption.TopDirectoryOnly)
        .Select(Path.GetFullPath)
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();
    if (debugBuild && managedPdbs.Length == 0)
    {
        return Fail("Debug compilation did not produce managed PDBs. Source-level native debugging cannot be enabled.");
    }

    string compileManifest = Path.Combine(project.OutputDirectory, "NovaOryn.Compile.json");
    File.WriteAllText(compileManifest, JsonSerializer.Serialize(new
    {
        schemaVersion = 6,
        productVersion = "0.41.0",
        project = project.Name,
        kernelEntry = project.KernelEntry,
        architecture = project.TargetArchitecture,
        runtimePack = "NovaOryn.RuntimePack.X64.Bootstrap",
        runtimeMode = "NoGcBootstrap",
        ilScanner = "Disabled",
        optimization = "BootstrapCorrectness",
        configuration,
        nativeDebugSymbols = debugBuild,
        managedAssembly,
        managedPdbs,
        systemModule,
        managedInputs,
        nativeObject,
        ilcMap,
        ilcExecutable = ilc,
        compilerHostRid = "win-x64",
        windowsRuntimeLibraries = 0,
        producedUtc = DateTimeOffset.UtcNow
    }, new JsonSerializerOptions { WriteIndented = true }));

    Console.WriteLine($"[ OK ] Roslyn produced managed IL: {managedAssembly}");
    Console.WriteLine($"[ OK ] ILC produced freestanding x64 object: {nativeObject}");
    if (debugBuild)
    {
        Console.WriteLine($"[ OK ] Managed debug PDBs: {managedPdbs.Length}");
        Console.WriteLine("[ OK ] NativeAOT CodeView debug generation: enabled (-g)");
    }
    Console.WriteLine("[ OK ] Windows CoreLib/runtime libraries linked: 0");
    Console.WriteLine($"[ OK ] Compilation manifest: {compileManifest}");
    return 0;
}


static string? GetProjectProperty(string projectFile, string propertyName)
{
    XDocument project = XDocument.Load(projectFile, LoadOptions.None);
    string? value = project.Descendants().FirstOrDefault(element =>
        string.Equals(element.Name.LocalName, propertyName, StringComparison.Ordinal))?.Value.Trim();
    return string.IsNullOrWhiteSpace(value) ? null : value;
}

static string GetAssemblyName(string projectFile)
{
    XDocument project = XDocument.Load(projectFile, LoadOptions.None);
    string? configuredName = project
        .Descendants()
        .FirstOrDefault(element => string.Equals(element.Name.LocalName, "AssemblyName", StringComparison.Ordinal))
        ?.Value
        .Trim();

    return string.IsNullOrWhiteSpace(configuredName)
        ? Path.GetFileNameWithoutExtension(projectFile)
        : configuredName;
}

static string FindIlc(string repositoryRoot)
{
    string manifestPath = Path.Combine(repositoryRoot, "toolchain", "NovaOryn.Toolchain.json");
    if (!File.Exists(manifestPath))
    {
        throw new FileNotFoundException("NovaOryn toolchain manifest was not found.", manifestPath);
    }

    using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
    string version = manifest.RootElement.GetProperty("nativeAot").GetProperty("packageVersion").GetString()
        ?? throw new InvalidOperationException("NativeAOT packageVersion is missing.");
    string packageDirectory = manifest.RootElement.GetProperty("nativeAot").GetProperty("packageDirectory").GetString()
        ?? throw new InvalidOperationException("NativeAOT packageDirectory is missing.");

    List<string> candidates =
    [
        Path.Combine(repositoryRoot, packageDirectory, "runtime.win-x64.microsoft.dotnet.ilcompiler", version, "tools", "ilc.exe")
    ];

    string? userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    if (!string.IsNullOrWhiteSpace(userProfile))
    {
        candidates.Add(Path.Combine(userProfile, ".nuget", "packages", "runtime.win-x64.microsoft.dotnet.ilcompiler", version, "tools", "ilc.exe"));
    }

    foreach (string candidate in candidates)
    {
        if (File.Exists(candidate))
        {
            return Path.GetFullPath(candidate);
        }
    }

    throw new FileNotFoundException($"Pinned ILC {version} was not found. Checked: {string.Join(", ", candidates)}");
}

static string FindRepositoryRoot(string start)
{
    DirectoryInfo? directory = new(start);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "toolchain", "NovaOryn.Toolchain.json")))
        {
            return directory.FullName;
        }
        directory = directory.Parent;
    }
    throw new DirectoryNotFoundException("NovaOryn repository root was not found.");
}

static void RecreateDirectory(string path)
{
    if (Directory.Exists(path))
    {
        Directory.Delete(path, true);
    }
    Directory.CreateDirectory(path);
}

static int Run(string executable, IEnumerable<string> arguments, string workingDirectory)
{
    string[] argumentArray = arguments.ToArray();
    PrintCommand(executable, argumentArray);
    using Process process = new();
    process.StartInfo = new ProcessStartInfo(executable)
    {
        UseShellExecute = false,
        WorkingDirectory = workingDirectory
    };
    foreach (string argument in argumentArray)
    {
        process.StartInfo.ArgumentList.Add(argument);
    }
    if (!process.Start())
    {
        return -1;
    }
    process.WaitForExit();
    return process.ExitCode;
}

static void PrintCommand(string executable, IEnumerable<string> arguments)
{
    Console.WriteLine($"[INFO] {Quote(executable)} {string.Join(" ", arguments.Select(Quote))}");
}

static string Quote(string value) => value.Any(char.IsWhiteSpace) ? $"\"{value}\"" : value;
static bool HasOption(string[] args, string option) => args.Any(value => string.Equals(value, option, StringComparison.OrdinalIgnoreCase));
static string? GetOption(string[] args, string name)
{
    for (int index = 0; index + 1 < args.Length; index++)
    {
        if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
        {
            return args[index + 1];
        }
    }
    return null;
}
static int Fail(string message)
{
    Console.Error.WriteLine($"[FAIL] {message}");
    return 1;
}
