using System.Text.RegularExpressions;
string root = FindRepositoryRoot(AppContext.BaseDirectory);
List<string> failures = [];
foreach (string file in Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
{
    string source = File.ReadAllText(file);
    if (source.Contains("public void ", StringComparison.Ordinal) || source.Contains("public static void ", StringComparison.Ordinal))
        failures.Add($"Public void method found: {Path.GetRelativePath(root, file)}");
}
string kernel = Read(root, "src/NovaOryn.Kernel.Sample/Kernel.cs");
Require(Regex.IsMatch(kernel, @"public\s+static\s+bool\s+KMain\s*\("), "KMain must be public static bool.");
string cpu = Read(root, "src/NovaOryn.Architecture.X64/CPU.cs");
Require(cpu.Contains("/// <summary>", StringComparison.Ordinal), "Public x64 CPU API must retain XML documentation.");
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
