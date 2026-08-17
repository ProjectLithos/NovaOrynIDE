string root = FindRepositoryRoot(AppContext.BaseDirectory);
List<string> failures = [];
string collector=Read(root,"src/NovaOryn.DocumentationGenerator/PublicApiCollector.cs");
string writer=Read(root,"src/NovaOryn.DocumentationGenerator/HtmlSiteWriter.cs");
Require(collector.Contains("SearchOption.TopDirectoryOnly",StringComparison.Ordinal),"Documentation collector must enumerate authoritative top-level src projects.");
Require(collector.Contains("ProjectTemplates",StringComparison.Ordinal),"Documentation collector must exclude embedded VSIX template duplicates.");
Require(collector.Contains("PublicDeclarationRegex().Match(signature)",StringComparison.Ordinal),"Documentation collector must classify complete multiline public declarations.");
Require(writer.Contains("All public items",StringComparison.Ordinal),"index.html must contain the exhaustive public-item list.");
Require(writer.Contains("Public SDK source",StringComparison.Ordinal)&&writer.Contains("source/index.html",StringComparison.Ordinal),"Documentation must expose public source navigation.");
Require(writer.Contains("search-index.js",StringComparison.Ordinal)&&!writer.Contains("fetch(root+'search-index.json')",StringComparison.Ordinal),"Offline documentation search must use relative script mapping, not fetch().");
Require(!writer.Contains("C:\\\\NovaOryn",StringComparison.Ordinal),"Generated documentation must not hard-code the repository path.");
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
