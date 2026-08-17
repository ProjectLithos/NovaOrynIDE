using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NovaOryn.DocumentationGenerator;

internal static class ApiCompatibilityWriter
{
    internal static void Write(string root, DocumentationConfiguration configuration, IReadOnlyList<ProjectDocumentation> projects)
    {
        var assemblies = projects.Where(project => project.IsPublicAssembly)
            .OrderBy(project => project.Name, StringComparer.Ordinal)
            .Select(project => new
            {
                name = project.Name,
                project = project.ProjectPath,
                dependencies = project.Dependencies,
                items = project.Items.OrderBy(item => item.QualifiedName, StringComparer.Ordinal)
                    .ThenBy(item => item.Signature, StringComparer.Ordinal)
                    .Select(item => new
                    {
                        id = item.Id,
                        item.Namespace,
                        item.Kind,
                        item.Name,
                        item.QualifiedName,
                        item.Signature,
                        signatureHash = Hash(item.Signature),
                        item.SourcePath,
                        item.SourceLine
                    }).ToArray()
            }).ToArray();

        string directory = Path.Combine(root, "Artifacts", "Documentation");
        Directory.CreateDirectory(directory);
        string output = Path.Combine(directory, "NovaOryn.ApiCompatibility.json");
        File.WriteAllText(output, JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            product = configuration.Product,
            version = configuration.Version,
            generatedUtc = DateTimeOffset.UtcNow,
            assemblies
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
