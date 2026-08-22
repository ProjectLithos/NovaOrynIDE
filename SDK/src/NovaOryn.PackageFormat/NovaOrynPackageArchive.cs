using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NovaOryn.PackageFormat;

public static class NovaOrynPackageArchive
{
    private static readonly Regex IdPattern = new("^[A-Za-z0-9]+(?:[._-][A-Za-z0-9]+)+$", RegexOptions.CultureInvariant);
    private static readonly Regex VersionPattern = new("^[0-9]+\\.[0-9]+\\.[0-9]+(?:[-+][0-9A-Za-z.-]+)?$", RegexOptions.CultureInvariant);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    public static NovaOrynPackageInspection Inspect(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        var manifestEntry = archive.GetEntry(NovaOrynPackageFormat.ManifestName)
            ?? throw new InvalidDataException($"{NovaOrynPackageFormat.ManifestName} must exist at the ZIP root.");
        using var stream = manifestEntry.Open();
        var manifest = JsonSerializer.Deserialize<NovaOrynPackageManifest>(stream, JsonOptions)
            ?? throw new InvalidDataException("Package manifest is invalid JSON.");
        ValidateManifest(manifest);
        return new(manifest, archive.Entries.Select(e => NormalizeEntryPath(e.FullName)).ToArray());
    }

    public static NovaOrynPackageVerification Verify(string packagePath)
    {
        var errors = new List<string>();
        NovaOrynPackageManifest? manifest = null;
        try
        {
            using var archive = ZipFile.OpenRead(packagePath);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in archive.Entries)
            {
                var normalized = NormalizeEntryPath(entry.FullName);
                if (!IsSafeRelativePath(normalized)) errors.Add($"Unsafe ZIP entry path: {entry.FullName}");
                if (!names.Add(normalized)) errors.Add($"Duplicate ZIP entry path: {normalized}");
            }

            var manifestEntry = archive.GetEntry(NovaOrynPackageFormat.ManifestName);
            if (manifestEntry is null) return new(false, null, [.. errors, $"Missing root {NovaOrynPackageFormat.ManifestName}."]);
            using (var stream = manifestEntry.Open())
                manifest = JsonSerializer.Deserialize<NovaOrynPackageManifest>(stream, JsonOptions);
            if (manifest is null) return new(false, null, [.. errors, "Package manifest is invalid JSON."]);
            try { ValidateManifest(manifest); } catch (Exception ex) { errors.Add(ex.Message); }

            var declared = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in manifest.Files)
            {
                var normalized = NormalizeEntryPath(file.Path);
                if (!IsSafeRelativePath(normalized) || normalized.Equals(NovaOrynPackageFormat.ManifestName, StringComparison.OrdinalIgnoreCase))
                { errors.Add($"Invalid payload path in manifest: {file.Path}"); continue; }
                if (!declared.Add(normalized)) { errors.Add($"Duplicate manifest file: {normalized}"); continue; }
                var entry = archive.GetEntry(normalized);
                if (entry is null) { errors.Add($"Missing payload file: {normalized}"); continue; }
                if (entry.Length != file.Length) errors.Add($"Length mismatch for {normalized}.");
                using var data = entry.Open();
                var hash = Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
                if (!hash.Equals(file.Sha256, StringComparison.OrdinalIgnoreCase)) errors.Add($"SHA-256 mismatch for {normalized}.");
            }

            foreach (var entry in archive.Entries)
            {
                var normalized = NormalizeEntryPath(entry.FullName);
                if (normalized.EndsWith('/')) continue;
                if (normalized.Equals(NovaOrynPackageFormat.ManifestName, StringComparison.OrdinalIgnoreCase)) continue;
                if (!declared.Contains(normalized)) errors.Add($"Undeclared package payload: {normalized}");
            }

            ValidatePackageTypePayload(manifest, declared, errors);
            ValidateSignaturePolicy(manifest, errors);
        }
        catch (Exception ex) { errors.Add(ex.Message); }
        return new(errors.Count == 0, manifest, errors);
    }

    public static void ValidateManifest(NovaOrynPackageManifest manifest)
    {
        if (!string.Equals(manifest.Format, NovaOrynPackageFormat.Format, StringComparison.Ordinal)) throw new InvalidDataException($"format must be {NovaOrynPackageFormat.Format}.");
        if (manifest.SchemaVersion != NovaOrynPackageFormat.SchemaVersion) throw new InvalidDataException($"schemaVersion must be {NovaOrynPackageFormat.SchemaVersion}.");
        if (!IdPattern.IsMatch(manifest.Id ?? "")) throw new InvalidDataException("Package id must be a stable reverse-DNS-like identifier.");
        if (string.IsNullOrWhiteSpace(manifest.Name)) throw new InvalidDataException("Package name is required.");
        if (!VersionPattern.IsMatch(manifest.Version ?? "")) throw new InvalidDataException("Package version must use semantic x.y.z form.");
        if (manifest.Architectures is null || manifest.Architectures.Length == 0) throw new InvalidDataException("At least one architecture is required.");
        var allowedArch = new HashSet<string>(["any", "x64", "x86_64", "arm64", "riscv64"], StringComparer.OrdinalIgnoreCase);
        if (manifest.Architectures.Any(a => !allowedArch.Contains(a))) throw new InvalidDataException("Unsupported package architecture.");
        if (manifest.Dependencies.Any(d => string.IsNullOrWhiteSpace(d.Id))) throw new InvalidDataException("Every dependency requires an id.");
        if (manifest.Capabilities.Any(string.IsNullOrWhiteSpace)) throw new InvalidDataException("Capability names must be non-empty.");
    }

    public static string NormalizeEntryPath(string path) => path.Replace('\\', '/');

    public static bool IsSafeRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path) || path.StartsWith('/') || Regex.IsMatch(path, "^[A-Za-z]:/") || path.Contains('\0')) return false;
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.All(p => p != "." && p != "..");
    }

    private static void ValidatePackageTypePayload(NovaOrynPackageManifest m, HashSet<string> files, List<string> errors)
    {
        bool HasExt(string ext) => files.Any(f => f.StartsWith("payload/", StringComparison.OrdinalIgnoreCase) && f.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        switch (m.Type)
        {
            case NovaOrynPackageKind.Application:
                if (!HasExt(".exe")) errors.Add("Application package must contain a payload .exe.");
                break;
            case NovaOrynPackageKind.Driver:
                if (!HasExt(".nodrv")) errors.Add("Driver package must contain a payload .nodrv driver artifact.");
                break;
            case NovaOrynPackageKind.Library:
                if (!HasExt(".dll") && !HasExt(".lib")) errors.Add("Library package must contain a payload .dll or .lib.");
                break;
            case NovaOrynPackageKind.Service:
                if (!HasExt(".exe")) errors.Add("Service package must contain a payload .exe.");
                if (string.IsNullOrWhiteSpace(m.Install.ServiceName)) errors.Add("Service package must define install.serviceName.");
                break;
            case NovaOrynPackageKind.KernelExtension:
                if (!files.Any(f => f.StartsWith("payload/", StringComparison.OrdinalIgnoreCase))) errors.Add("Kernel-extension package must contain payload files.");
                break;
        }
    }

    private static void ValidateSignaturePolicy(NovaOrynPackageManifest m, List<string> errors)
    {
        var state = m.Signing?.State?.ToLowerInvariant() ?? "unsigned";
        if (state is not ("unsigned" or "development" or "signed" or "trusted" or "revoked")) errors.Add("Invalid signing.state.");
        if (state == "revoked") errors.Add("Package signer is revoked.");
        if (m.Type == NovaOrynPackageKind.KernelExtension && state is not ("signed" or "trusted")) errors.Add("Kernel extensions must be signed or trusted.");
    }
}

public static class NovaOrynVersionConstraint
{
    public static bool Matches(string installedVersion, string constraint)
    {
        if (string.IsNullOrWhiteSpace(constraint) || constraint == "*") return true;
        if (!Version.TryParse(Normalize(installedVersion), out var installed)) return false;
        foreach (var token in constraint.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string op; string value;
            if (token.StartsWith(">=")) { op = ">="; value = token[2..]; }
            else if (token.StartsWith("<=")) { op = "<="; value = token[2..]; }
            else if (token.StartsWith('>')) { op = ">"; value = token[1..]; }
            else if (token.StartsWith('<')) { op = "<"; value = token[1..]; }
            else if (token.StartsWith('=')) { op = "="; value = token[1..]; }
            else { op = "="; value = token; }
            if (!Version.TryParse(Normalize(value), out var wanted)) return false;
            var cmp = installed.CompareTo(wanted);
            if (op == ">=" && cmp < 0 || op == "<=" && cmp > 0 || op == ">" && cmp <= 0 || op == "<" && cmp >= 0 || op == "=" && cmp != 0) return false;
        }
        return true;
    }

    private static string Normalize(string version)
    {
        var core = version.Split('-', '+')[0];
        var parts = core.Split('.');
        return parts.Length switch { 1 => core + ".0.0", 2 => core + ".0", _ => core };
    }
}
