using System.Security.Cryptography;
using System.Text.Json;
using NovaOryn.ProjectModel;

return MainEntry(args);

static int MainEntry(string[] args)
{
    if (args.Length < 2 || !string.Equals(args[0], "create", StringComparison.OrdinalIgnoreCase))
    {
        return Fail("Usage: NovaOryn.ImageBuilder create <NovaOrynProject.json> [--kernel <path>] [--output <path>] [--dry-run]");
    }

    if (!NovaOrynProject.TryLoad(args[1], out NovaOrynProject? project, out string error) || project is null)
    {
        return Fail(error);
    }

    string outputDirectory = Path.GetFullPath(project.OutputDirectory);
    string kernelPath = Path.GetFullPath(GetOption(args, "--kernel") ?? Path.Combine(outputDirectory, project.Name + ".efi"));
    string imagePath = Path.GetFullPath(GetOption(args, "--output") ?? Path.Combine(outputDirectory, project.Name + ".img"));
    string stagingRoot = Path.Combine(outputDirectory, "BootFiles");
    string stagedKernel = Path.Combine(stagingRoot, "EFI", "BOOT", "BOOTX64.EFI");

    Console.WriteLine($"[INFO] Kernel input : {kernelPath}");
    Console.WriteLine($"[INFO] EFI path     : EFI\\BOOT\\BOOTX64.EFI");
    Console.WriteLine($"[INFO] FAT32 image  : {imagePath}");
    if (HasOption(args, "--dry-run"))
    {
        return 0;
    }

    if (!File.Exists(kernelPath))
    {
        return Fail($"Freestanding EFI kernel not found: {kernelPath}");
    }

    try
    {
        Directory.CreateDirectory(outputDirectory);
        if (Directory.Exists(stagingRoot))
        {
            Directory.Delete(stagingRoot, true);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(stagedKernel)!);
        File.Copy(kernelPath, stagedKernel, true);

        if (!EfiDiskImage.TryCreate(stagedKernel, imagePath, out error))
        {
            return Fail(error);
        }

        string manifestPath = Path.Combine(outputDirectory, "NovaOryn.Image.json");
        using FileStream image = File.OpenRead(imagePath);
        string imageSha256 = Convert.ToHexString(SHA256.HashData(image)).ToLowerInvariant();
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            productVersion = "0.40.0",
            project = project.Name,
            architecture = project.TargetArchitecture,
            bootProtocol = project.BootProtocol,
            format = "GPT/FAT32 EFI System Partition",
            imagePath,
            efiPath = "EFI/BOOT/BOOTX64.EFI",
            stagedKernel,
            kernelLength = new FileInfo(stagedKernel).Length,
            imageLength = new FileInfo(imagePath).Length,
            imageSha256,
            producedUtc = DateTimeOffset.UtcNow
        }, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine($"[ OK ] Staged EFI application: {stagedKernel}");
        Console.WriteLine($"[ OK ] Bootable GPT/FAT32 image: {imagePath}");
        Console.WriteLine($"[ OK ] Image manifest: {manifestPath}");
        return 0;
    }
    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
    {
        return Fail(exception.Message);
    }
}

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
