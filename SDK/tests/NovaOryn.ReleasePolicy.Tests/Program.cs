using System.Text.Json;
string root = FindRepositoryRoot(AppContext.BaseDirectory);
List<string> failures = [];
string updater=Read(root,"Update-NovaOryn.ps1");
Require(updater.Contains("Assert-TargetSourceManifest",StringComparison.Ordinal)&&updater.Contains("Target source manifest verified",StringComparison.Ordinal),"Updater must verify the target source manifest before staging/pushing.");
Require(updater.Contains("SHA-256 mismatch",StringComparison.Ordinal),"Updater must reject source-manifest hash mismatches.");
string bootstrap=Read(root,"Update-NovaOryn.bat");
Require(bootstrap.Contains("Bootstrap-Update-NovaOryn.ps1",StringComparison.Ordinal),"Update-NovaOryn.bat must launch the archive-carried bootstrap updater.");
string manifestPath=Path.Combine(root,"NovaOryn-SourceManifest.json");
using JsonDocument manifest=JsonDocument.Parse(File.ReadAllText(manifestPath));
HashSet<string> listed=new(StringComparer.OrdinalIgnoreCase);
foreach(JsonElement e in manifest.RootElement.GetProperty("files").EnumerateArray()) listed.Add(e.GetProperty("path").GetString()!.Replace('\\','/'));
Require(listed.Contains("THIRD-PARTY-NOTICES.md"),"Source manifest must include THIRD-PARTY-NOTICES.md.");
Require(!listed.Contains("NovaOryn-SourceManifest.json"),"Source manifest must exclude itself.");
foreach (string tree in new[] { "src", "tests", "templates" })
{
    string treePath = Path.Combine(root, tree);
    if (!Directory.Exists(treePath)) continue;
    foreach (string file in Directory.EnumerateFiles(treePath, "*.cs", SearchOption.AllDirectories))
    {
        int lineNumber = 0;
        foreach (string line in File.ReadLines(file))
        {
            lineNumber++;
            int comment = line.IndexOf("///", StringComparison.Ordinal);
            int summaryEnd = line.IndexOf("</summary>", StringComparison.Ordinal);
            if (comment < 0 || summaryEnd < 0 || summaryEnd < comment) continue;
            string tail = line[(summaryEnd + "</summary>".Length)..];
            bool declaration = tail.Contains(" public ", StringComparison.Ordinal) || tail.TrimStart().StartsWith("public ", StringComparison.Ordinal) || tail.Contains(" internal ", StringComparison.Ordinal) || tail.TrimStart().StartsWith("internal ", StringComparison.Ordinal) || tail.Contains(" private ", StringComparison.Ordinal) || tail.TrimStart().StartsWith("private ", StringComparison.Ordinal) || tail.Contains(" protected ", StringComparison.Ordinal) || tail.TrimStart().StartsWith("protected ", StringComparison.Ordinal);
            Require(!declaration, $"C# declaration is hidden by an XML documentation comment: {Path.GetRelativePath(root,file)}:{lineNumber}.");
        }
    }
}
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
