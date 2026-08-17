using System;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace NovaOryn.VisualStudio;

internal static class NovaOrynProjectRecognizer
{
    public static bool IsNovaOrynProject(string projectFile)
    {
        if (string.IsNullOrWhiteSpace(projectFile) || !File.Exists(projectFile)) return false;
        string directory = Path.GetDirectoryName(projectFile) ?? string.Empty;
        if (File.Exists(Path.Combine(directory, "NovaOrynProject.json"))) return true;
        string text = File.ReadAllText(projectFile);
        return text.IndexOf("<NovaOrynProject>true</NovaOrynProject>", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string TryGetProjectDirectory(Project project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (project == null) return string.Empty;
        try
        {
            string projectFile = project.FullName;
            if (string.IsNullOrWhiteSpace(projectFile)) return string.Empty;
            return Path.GetDirectoryName(Path.GetFullPath(projectFile)) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
