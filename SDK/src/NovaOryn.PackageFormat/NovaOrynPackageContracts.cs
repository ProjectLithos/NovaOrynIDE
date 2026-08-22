using System.Text.Json.Serialization;

namespace NovaOryn.PackageFormat;

public static class NovaOrynPackageFormat
{
    public const string ContainerExtension = ".zip";
    public const string ManifestName = "NovaOryn.Package.json";
    public const string Format = "novaoryn-package-v1";
    public const int SchemaVersion = 1;
}

[JsonConverter(typeof(JsonStringEnumConverter<NovaOrynPackageKind>))]
public enum NovaOrynPackageKind
{
    Application,
    Driver,
    Library,
    Service,
    KernelExtension
}

public sealed class NovaOrynPackageManifest
{
    public string Format { get; set; } = NovaOrynPackageFormat.Format;
    public int SchemaVersion { get; set; } = NovaOrynPackageFormat.SchemaVersion;
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public NovaOrynPackageKind Type { get; set; }
    public string Publisher { get; set; } = "";
    public string[] Architectures { get; set; } = ["any"];
    public NovaOrynPackageRequirements Requires { get; set; } = new();
    public NovaOrynPackageDependency[] Dependencies { get; set; } = [];
    public string[] Capabilities { get; set; } = [];
    public NovaOrynPackageFile[] Files { get; set; } = [];
    public NovaOrynPackageInstall Install { get; set; } = new();
    public NovaOrynPackageSignature Signing { get; set; } = new();
}

public sealed class NovaOrynPackageRequirements
{
    public string MinimumNovaOrynVersion { get; set; } = "0.0.0";
    public string SdkApiVersion { get; set; } = "1.0";
    public string AbiVersion { get; set; } = "1.0";
}

public sealed class NovaOrynPackageDependency
{
    public string Id { get; set; } = "";
    public string Version { get; set; } = "*";
    public bool Optional { get; set; }
}

public sealed class NovaOrynPackageFile
{
    public string Path { get; set; } = "";
    public long Length { get; set; }
    public string Sha256 { get; set; } = "";
}

public sealed class NovaOrynPackageInstall
{
    public string Entry { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string ServiceStartup { get; set; } = "manual";
    public string LibraryKind { get; set; } = "";
}

public sealed class NovaOrynPackageSignature
{
    public string State { get; set; } = "unsigned";
    public string Algorithm { get; set; } = "";
    public string SignerId { get; set; } = "";
    public string ManifestDigest { get; set; } = "";
    public string Value { get; set; } = "";
}

public sealed record NovaOrynPackageInspection(NovaOrynPackageManifest Manifest, IReadOnlyList<string> Entries);

public sealed record NovaOrynPackageVerification(bool Success, NovaOrynPackageManifest? Manifest, IReadOnlyList<string> Errors)
{
    public static NovaOrynPackageVerification Failed(params string[] errors) => new(false, null, errors);
}
