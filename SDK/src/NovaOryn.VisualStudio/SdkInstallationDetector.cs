using System;
using System.IO;
namespace NovaOryn.VisualStudio;
internal static class SdkInstallationDetector
{
    public static string ResolveSdkRoot()
    {
        string configured = Environment.GetEnvironmentVariable("NOVAORYN_SDK_ROOT");
        return string.IsNullOrWhiteSpace(configured) ? @"C:\NovaOryn" : Path.GetFullPath(configured);
    }
    public static bool IsInstalled(out string root)
    {
        root = ResolveSdkRoot();
        return File.Exists(Path.Combine(root, "Build-NovaOryn.ps1"));
    }
}
