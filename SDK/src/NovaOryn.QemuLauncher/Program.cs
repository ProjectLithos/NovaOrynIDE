using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using NovaOryn.ProjectModel;

return MainEntry(args);

static int MainEntry(string[] args)
{
    if (args.Length < 2 || !string.Equals(args[0], "run", StringComparison.OrdinalIgnoreCase))
    {
        return Fail("Usage: NovaOryn.QemuLauncher run <NovaOrynProject.json> --qemu <path> --image <path> [--ovmf-code <path>] [--ovmf-vars <path>] [--timeout-seconds <value>] [--dry-run]");
    }

    if (!NovaOrynProject.TryLoad(args[1], out NovaOrynProject? project, out string error) || project is null)
    {
        return Fail(error);
    }

    string? qemuOption = GetOption(args, "--qemu") ?? Environment.GetEnvironmentVariable("NOVAORYN_QEMU_X64");
    string? imageOption = GetOption(args, "--image");
    if (string.IsNullOrWhiteSpace(qemuOption) || string.IsNullOrWhiteSpace(imageOption))
    {
        return Fail("QEMU and image paths are required.");
    }

    string qemu = Path.GetFullPath(qemuOption);
    string imagePath = Path.GetFullPath(imageOption);
    string? ovmfCodeOption = GetOption(args, "--ovmf-code") ?? Environment.GetEnvironmentVariable("NOVAORYN_OVMF_CODE");
    string? ovmfVarsOption = GetOption(args, "--ovmf-vars") ?? Environment.GetEnvironmentVariable("NOVAORYN_OVMF_VARS");
    string? ovmfCode = ResolveFirmware(qemu, ovmfCodeOption, ["edk2-x86_64-code.fd", "OVMF_CODE.fd"]);
    string? ovmfVars = ResolveFirmware(qemu, ovmfVarsOption, ["edk2-i386-vars.fd", "edk2-x86_64-vars.fd", "OVMF_VARS.fd"]);
    int timeoutSeconds;
    try
    {
        timeoutSeconds = ParseTimeout(GetOption(args, "--timeout-seconds"));
    }
    catch (ArgumentOutOfRangeException exception)
    {
        return Fail(exception.Message);
    }

    string outputDirectory = Path.GetFullPath(project.OutputDirectory);
    string runId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
    string runDirectory = Path.Combine(outputDirectory, "Runs", runId);
    string runImage = Path.Combine(runDirectory, Path.GetFileName(imagePath));
    string variableStore = Path.Combine(runDirectory, "OVMF_VARS.fd");
    string serialLog = Path.Combine(runDirectory, "serial.log");

    if (string.IsNullOrWhiteSpace(ovmfCode) || string.IsNullOrWhiteSpace(ovmfVars))
    {
        return Fail("x64 OVMF firmware was not found. Run Install-NovaOrynToolchain.bat or pass --ovmf-code and --ovmf-vars.");
    }

    int hostLogicalProcessorCount = Environment.ProcessorCount;
    int qemuProcessorCount = CalculateQemuProcessorCount(hostLogicalProcessorCount);
    string[] qemuArguments = BuildArguments(ovmfCode, variableStore, runImage, serialLog, qemuProcessorCount);
    Console.WriteLine($"[INFO] QEMU executable: {qemu}");
    Console.WriteLine($"[INFO] OVMF code     : {ovmfCode}");
    Console.WriteLine($"[INFO] OVMF variables: {ovmfVars}");
    Console.WriteLine($"[INFO] Boot image    : {imagePath}");
    Console.WriteLine($"[INFO] Host CPUs     : {hostLogicalProcessorCount} logical processor(s)");
    Console.WriteLine($"[INFO] QEMU CPUs     : {qemuProcessorCount} logical processor(s) (50% of host, rounded up)");
    Console.WriteLine($"[INFO] {Quote(qemu)} {string.Join(" ", qemuArguments.Select(Quote))}");
    if (HasOption(args, "--dry-run"))
    {
        return 0;
    }

    if (!File.Exists(qemu)) return Fail($"QEMU executable not found: {qemu}");
    if (!File.Exists(imagePath)) return Fail($"Boot image not found: {imagePath}");
    if (!File.Exists(ovmfCode)) return Fail($"OVMF code firmware not found: {ovmfCode}");
    if (!File.Exists(ovmfVars)) return Fail($"OVMF variable-store template not found: {ovmfVars}");

    Process? process = null;
    try
    {
        Directory.CreateDirectory(runDirectory);
        File.Copy(imagePath, runImage, true);
        File.Copy(ovmfVars, variableStore, true);
        if (File.Exists(serialLog)) File.Delete(serialLog);

        process = new Process();
        process.StartInfo = new ProcessStartInfo(qemu)
        {
            UseShellExecute = false,
            WorkingDirectory = runDirectory,
            CreateNoWindow = false
        };
        foreach (string argument in qemuArguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        if (!process.Start())
        {
            process.Dispose();
            process = null;
            return Fail("QEMU failed to start.");
        }

        Console.WriteLine($"[ OK ] QEMU started without -S. Process ID: {process.Id}");
        DateTime deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        string serialText = string.Empty;
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                int exitCode = process.ExitCode;
                process.Dispose();
                return Fail($"QEMU exited before runtime acceptance completed. Exit code: {exitCode}. Serial log: {serialLog}");
            }

            serialText = ReadSharedText(serialLog);
            if (serialText.Contains("NovaOryn KMain started.", StringComparison.Ordinal) &&
                serialText.Contains("NovaOryn> ", StringComparison.Ordinal))
            {
                Thread.Sleep(1500);
                if (process.HasExited)
                {
                    int exitCode = process.ExitCode;
                    process.Dispose();
                    return Fail($"QEMU exited after the interactive command prompt appeared. Exit code: {exitCode}.");
                }

                string latestSerialLog = Path.Combine(outputDirectory, "serial.log");
                File.WriteAllText(latestSerialLog, serialText);
                string runManifest = Path.Combine(outputDirectory, "NovaOryn.Run.json");
                File.WriteAllText(runManifest, JsonSerializer.Serialize(new
                {
                    schemaVersion = 1,
                    productVersion = "0.37.4",
                    project = project.Name,
                    qemuProcessId = process.Id,
                    qemuExecutable = qemu,
                    hostLogicalProcessorCount,
                    qemuProcessorCount,
                    ovmfCode,
                    ovmfVariableTemplate = ovmfVars,
                    variableStore,
                    bootImage = runImage,
                    serialLog,
                    latestSerialLog,
                    managedKMainConfirmed = true,
                    interactiveConsoleConfirmed = true,
                    qemuRemainedOpen = true,
                    acceptedUtc = DateTimeOffset.UtcNow
                }, new JsonSerializerOptions { WriteIndented = true }));

                Console.WriteLine("[ OK ] Managed KMain execution confirmed.");
                Console.WriteLine("[ OK ] Interactive command prompt confirmed.");
                Console.WriteLine($"[ OK ] QEMU remains open indefinitely. Process ID: {process.Id}");
                Console.WriteLine($"[ OK ] Serial output captured: {latestSerialLog}");
                Console.WriteLine($"[ OK ] Live run directory: {runDirectory}");
                return 0;
            }

            Thread.Sleep(100);
        }

        serialText = ReadSharedText(serialLog);
        PersistLatestSerial(outputDirectory, serialText);
        PrintSerialTail(serialText);
        TryStop(process);
        if (string.IsNullOrEmpty(serialText))
        {
            return Fail($"Timed out before any NovaOryn serial output appeared after {timeoutSeconds} seconds. Serial log: {serialLog}");
        }
        if (!serialText.Contains("NovaOryn KMain started.", StringComparison.Ordinal))
        {
            return Fail($"Timed out before NovaOryn KMain started after {timeoutSeconds} seconds. Serial log: {serialLog}");
        }
        return Fail($"NovaOryn KMain started, but interactive command prompt was not reached within {timeoutSeconds} seconds. Serial log: {serialLog}");
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
    {
        if (process is not null) TryStop(process);
        return Fail(exception.Message);
    }
}

static string[] BuildArguments(string ovmfCode, string variableStore, string runImage, string serialLog, int qemuProcessorCount)
{
    return
    [
        "-machine", "q35",
        "-accel", "tcg,thread=multi",
        "-cpu", "max",
        "-smp", qemuProcessorCount.ToString(CultureInfo.InvariantCulture),
        "-m", "512M",
        "-display", "sdl",
        "-drive", $"if=pflash,format=raw,unit=0,readonly=on,file={EscapeDriveValue(ovmfCode)}",
        "-drive", $"if=pflash,format=raw,unit=1,file={EscapeDriveValue(variableStore)}",
        "-drive", $"if=none,format=raw,readonly=on,file={EscapeDriveValue(runImage)},id=boot",
        "-device", "virtio-blk-pci,drive=boot,bootindex=0",
        "-device", "virtio-gpu-pci",
        "-boot", "menu=off,strict=on",
        "-serial", $"file:{serialLog}",
        "-monitor", "none",
        "-no-reboot",
        "-no-shutdown"
    ];
}


static int CalculateQemuProcessorCount(int hostLogicalProcessorCount)
{
    if (hostLogicalProcessorCount <= 1) return 1;
    return (hostLogicalProcessorCount + 1) / 2;
}

static string? ResolveFirmware(string qemu, string? explicitPath, string[] names)
{
    if (!string.IsNullOrWhiteSpace(explicitPath))
    {
        return Path.GetFullPath(explicitPath);
    }

    string qemuDirectory = Path.GetDirectoryName(qemu) ?? Environment.CurrentDirectory;
    string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
    string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    List<string> roots =
    [
        qemuDirectory,
        Path.Combine(qemuDirectory, "share"),
        Path.Combine(qemuDirectory, "share", "qemu"),
        Path.GetFullPath(Path.Combine(qemuDirectory, "..", "share")),
        Path.GetFullPath(Path.Combine(qemuDirectory, "..", "share", "qemu")),
        Path.Combine(programFiles, "qemu"),
        Path.Combine(programFilesX86, "qemu"),
        Path.Combine(localAppData, "Programs", "qemu")
    ];

    foreach (string root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        if (!Directory.Exists(root)) continue;
        foreach (string name in names)
        {
            string direct = Path.Combine(root, name);
            if (File.Exists(direct)) return Path.GetFullPath(direct);
            try
            {
                string? recursive = Directory.EnumerateFiles(root, name, SearchOption.AllDirectories).FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(recursive)) return Path.GetFullPath(recursive);
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
    return null;
}

static int ParseTimeout(string? value)
{
    if (string.IsNullOrWhiteSpace(value)) return 90;
    if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int seconds) || seconds < 5 || seconds > 300)
    {
        throw new ArgumentOutOfRangeException(nameof(value), "Boot timeout must be between 5 and 300 seconds.");
    }
    return seconds;
}


static void PersistLatestSerial(string outputDirectory, string serialText)
{
    if (string.IsNullOrEmpty(serialText)) return;
    try
    {
        File.WriteAllText(Path.Combine(outputDirectory, "serial.log"), serialText);
    }
    catch (IOException)
    {
    }
    catch (UnauthorizedAccessException)
    {
    }
}

static void PrintSerialTail(string serialText)
{
    if (string.IsNullOrEmpty(serialText))
    {
        Console.Error.WriteLine("[INFO] QEMU serial log is empty.");
        return;
    }
    const int maximumTailCharacters = 4096;
    string tail = serialText.Length <= maximumTailCharacters ? serialText : serialText[^maximumTailCharacters..];
    Console.Error.WriteLine("[INFO] QEMU serial tail follows:");
    Console.Error.WriteLine(tail);
    Console.Error.WriteLine("[INFO] End QEMU serial tail.");
}

static string ReadSharedText(string path)
{
    if (!File.Exists(path)) return string.Empty;
    try
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
    catch (IOException)
    {
        return string.Empty;
    }
}

static bool TryStop(Process process)
{
    try
    {
        if (!process.HasExited) process.Kill(true);
        process.Dispose();
        return true;
    }
    catch
    {
        process.Dispose();
        return false;
    }
}

static string EscapeDriveValue(string value) => value.Replace(",", ",,", StringComparison.Ordinal);
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
