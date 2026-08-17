using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace NovaOryn.VisualStudio;

/// <summary>
/// Keeps NovaOryn workspace projects visible without loading the copied SDK dependency tree into the solution.
/// Userland projects are grouped under Userland; separately compiled kernel components are grouped under Kernel Projects.
/// Work is deliberately deferred so Visual Studio project creation is never blocked by NovaOryn synchronization.
/// </summary>
internal static class NovaOrynSolutionSynchronizer
{
    private static bool _queued;
    private static bool _running;

    internal static bool QueueUserlandProjects(AsyncPackage package, DTE dte, NovaOrynOutputPane output)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (package == null || dte == null || dte.Solution == null || !dte.Solution.IsOpen || _queued) return false;
        _queued = true;
        package.JoinableTaskFactory.RunAsync(async delegate
        {
            try
            {
                // Single-project NovaOryn templates copy nested workspace projects as ordinary files.
                // Retry briefly because the New Project transaction can finish after ProjectAdded fires.
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    await Task.Delay(500).ConfigureAwait(false);
                    await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                    if (EnsureUserlandProjectsLoaded(dte, output)) break;
                }
            }
            finally
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
                _queued = false;
            }
        }).FileAndForget("NovaOryn/WorkspaceProjects");
        return true;
    }

    internal static bool EnsureUserlandProjectsLoaded(DTE dte, NovaOrynOutputPane output)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (_running || dte == null || dte.Solution == null || !dte.Solution.IsOpen) return false;
        _running = true;
        try
        {
            List<string> workspaceRoots = new();
            HashSet<string> loaded = new(StringComparer.OrdinalIgnoreCase);
            foreach (Project project in dte.Solution.Projects) CollectProject(project, loaded, workspaceRoots);

            bool changed = false;
            foreach (string projectFile in workspaceRoots.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string root = Path.GetDirectoryName(projectFile) ?? string.Empty;
                changed |= RemoveInactiveGeneratedProjects(dte, output, root);
                changed |= EnsureConfiguredProjectsLoaded(dte, output, loaded, root);
            }
            return changed;
        }
        finally { _running = false; }
    }

    private static bool RemoveInactiveGeneratedProjects(DTE dte, NovaOrynOutputPane output, string root)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        string graph = Path.Combine(root, "Configuration", "WorkspaceProjects.txt");
        HashSet<string> active = new(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(graph))
            foreach (string relative in File.ReadAllLines(graph).Select(line => line.Trim()).Where(line => line.Length != 0))
                active.Add(Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar))));

        List<Project> projects = new();
        foreach (Project project in dte.Solution.Projects) CollectLoadedProjectObjects(project, projects);
        bool changed = false;
        foreach (Project project in projects)
        {
            string full;
            try { full = string.IsNullOrWhiteSpace(project.FullName) ? string.Empty : Path.GetFullPath(project.FullName); }
            catch { continue; }
            if (full.Length == 0 || !full.EndsWith(".Generated.csproj", StringComparison.OrdinalIgnoreCase) || active.Contains(full)) continue;
            if (!full.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) continue;
            dte.Solution.Remove(project);
            changed = true;
            output.WriteLine($"[ OK ] Removed inactive generated project from Solution Explorer: {Path.GetFileNameWithoutExtension(full)} (files preserved)");
        }
        return changed;
    }

    private static void CollectLoadedProjectObjects(Project project, List<Project> projects)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (project == null) return;
        try
        {
            if (!string.IsNullOrWhiteSpace(project.FullName) && project.FullName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) projects.Add(project);
        }
        catch { }
        try { foreach (ProjectItem item in project.ProjectItems) if (item.SubProject != null) CollectLoadedProjectObjects(item.SubProject, projects); }
        catch { }
    }

    private static bool EnsureConfiguredProjectsLoaded(DTE dte, NovaOrynOutputPane output, HashSet<string> loaded, string root)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        string graph = Path.Combine(root, "Configuration", "WorkspaceProjects.txt");
        if (!File.Exists(graph)) return false;
        bool changed = false;
        foreach (string relative in File.ReadAllLines(graph).Select(line => line.Trim()).Where(line => line.Length != 0))
        {
            string full = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!File.Exists(full) || loaded.Contains(full)) continue;
            string folderName = relative.StartsWith("Tests/", StringComparison.OrdinalIgnoreCase) ? "Tests" : "Userland";
            SolutionFolder folder = GetOrCreateSolutionFolder(dte, output, folderName);
            Project added = folder?.AddFromFile(full);
            if (added == null) continue;
            loaded.Add(full); changed = true;
            output.WriteLine($"[ OK ] Loaded configured project: {Path.GetFileNameWithoutExtension(full)}");
        }
        return changed;
    }

    private static bool EnsureProjectsLoadedFromDirectory(
        DTE dte,
        NovaOrynOutputPane output,
        HashSet<string> loaded,
        string directory,
        string solutionFolderName,
        string logKind)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (!Directory.Exists(directory)) return false;

        string[] children = Directory.EnumerateFiles(directory, "*.csproj", SearchOption.AllDirectories)
            .Where(path => path.IndexOf($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) < 0)
            .Where(path => path.IndexOf($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) < 0)
            .OrderBy(path => path.Count(ch => ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar))
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (children.Length == 0) return false;
        SolutionFolder folder = GetOrCreateSolutionFolder(dte, output, solutionFolderName);
        if (folder == null) return false;

        bool changed = false;
        foreach (string child in children)
        {
            string full = Path.GetFullPath(child);
            if (loaded.Contains(full)) continue;
            Project added = folder.AddFromFile(full);
            if (added == null) continue;
            loaded.Add(full);
            changed = true;
            output.WriteLine($"[ OK ] Loaded {logKind}: {Path.GetFileNameWithoutExtension(full)}");
        }
        return changed;
    }

    private static SolutionFolder GetOrCreateSolutionFolder(DTE dte, NovaOrynOutputPane output, string name)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        foreach (Project project in dte.Solution.Projects)
        {
            if (!string.Equals(project.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
            if (project.Object is SolutionFolder existing) return existing;
        }
        if (!(dte.Solution is Solution2 solution2)) return null;
        Project folderProject = solution2.AddSolutionFolder(name);
        if (!(folderProject?.Object is SolutionFolder folder)) return null;
        output.WriteLine($"[ OK ] Added {name} solution folder.");
        return folder;
    }

    private static void CollectProject(Project project, HashSet<string> loaded, List<string> workspaceRoots)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (project == null) return;
        try
        {
            if (!string.IsNullOrWhiteSpace(project.FullName) && project.FullName.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                string full = Path.GetFullPath(project.FullName);
                loaded.Add(full);
                if (IsWorkspaceRootProject(full)) workspaceRoots.Add(full);
            }
        }
        catch { }

        try
        {
            foreach (ProjectItem item in project.ProjectItems)
                if (item.SubProject != null) CollectProject(item.SubProject, loaded, workspaceRoots);
        }
        catch { }
    }

    private static bool IsWorkspaceRootProject(string projectFile)
    {
        if (!NovaOrynProjectRecognizer.IsNovaOrynProject(projectFile)) return false;
        string directory = Path.GetDirectoryName(projectFile) ?? string.Empty;
        return File.Exists(Path.Combine(directory, "NovaOrynProject.json"));
    }
}
