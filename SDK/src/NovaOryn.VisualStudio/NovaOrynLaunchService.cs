using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
namespace NovaOryn.VisualStudio;
internal sealed class NovaOrynLaunchService
{
    private readonly NovaOrynPackage _package;
    private int _launchActive;
    public NovaOrynLaunchService(NovaOrynPackage package) => _package = package ?? throw new ArgumentNullException(nameof(package));
    public bool TryGetActiveProject(out string projectFile)
    {
        ThreadHelper.ThrowIfNotOnUIThread(); projectFile = string.Empty;
        if (!(ServiceProvider.GlobalProvider.GetService(typeof(DTE)) is DTE dte) || dte.Solution == null || !dte.Solution.IsOpen) return false;
        Project project = ResolveStartupProject(dte) ?? (dte.ActiveSolutionProjects as Array)?.Cast<object>().OfType<Project>().FirstOrDefault();
        string candidate = project?.FullName;
        if (!NovaOrynProjectRecognizer.IsNovaOrynProject(candidate)) return false;
        projectFile = Path.GetFullPath(candidate); return true;
    }
    public bool QueueLaunch(bool buildOnly)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (Interlocked.CompareExchange(ref _launchActive, 1, 0) != 0)
        {
            _package.Output.WriteLine("[INFO] A NovaOryn build/run is already active; duplicate launch ignored.");
            return false;
        }
        _package.JoinableTaskFactory.RunAsync(async delegate
        {
            try { await LaunchAsync(buildOnly, CancellationToken.None); }
            finally { Interlocked.Exchange(ref _launchActive, 0); }
        }).FileAndForget("NovaOryn/Launch");
        return true;
    }
    private async Task LaunchAsync(bool buildOnly, CancellationToken token)
    {
        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(token); _package.Output.Activate();
        if (!TryGetActiveProject(out string projectFile)) { _package.Output.WriteLine("[FAIL] The startup project is not a NovaOryn kernel project."); return; }
        if (!SdkInstallationDetector.IsInstalled(out string sdkRoot)) { _package.Output.WriteLine($"[FAIL] NovaOryn SDK was not found at {sdkRoot}."); return; }
        DTE dte = await _package.GetServiceAsync(typeof(DTE)) as DTE;
        if (dte != null) dte.ExecuteCommand("File.SaveAll");
        string manifest = Path.Combine(Path.GetDirectoryName(projectFile) ?? string.Empty, "NovaOrynProject.json");
        string script = Path.Combine(sdkRoot, "Build-NovaOryn.ps1");
        string configuration = ResolveConfiguration(dte);
        string arguments = $"-NoProfile -ExecutionPolicy Bypass -File {Quote(script)} -Project {Quote(manifest)} -Configuration {configuration}" + (buildOnly ? " -NoRun" : " -Run");
        _package.Output.WriteLine($"[INFO] NovaOryn {(buildOnly ? "Build" : "Run")}: {Path.GetFileName(projectFile)} ({configuration}, fast kernel path; SDK validation skipped).");
        using var process = new System.Diagnostics.Process { StartInfo = new System.Diagnostics.ProcessStartInfo("powershell.exe", arguments) { WorkingDirectory = sdkRoot, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => WriteLine(e.Data); process.ErrorDataReceived += (_, e) => WriteLine(e.Data);
        if (!process.Start()) { _package.Output.WriteLine("[FAIL] NovaOryn build process did not start."); return; }
        process.BeginOutputReadLine(); process.BeginErrorReadLine(); await Task.Run(() => process.WaitForExit(), token);
        WriteLine(process.ExitCode == 0 ? "[ OK ] NovaOryn operation completed." : $"[FAIL] NovaOryn operation failed with exit code {process.ExitCode}.");
        if (process.ExitCode == 0)
        {
            await _package.JoinableTaskFactory.SwitchToMainThreadAsync(token);
            if (dte != null) NovaOrynSolutionSynchronizer.QueueUserlandProjects(_package, dte, _package.Output);
        }
    }
    private void WriteLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        _package.JoinableTaskFactory.RunAsync(async delegate { await _package.JoinableTaskFactory.SwitchToMainThreadAsync(); _package.Output.WriteLine(line); }).FileAndForget("NovaOryn/Output");
    }
    private static string ResolveConfiguration(DTE dte)
    {
        ThreadHelper.ThrowIfNotOnUIThread(); try { return dte?.Solution?.SolutionBuild?.ActiveConfiguration?.Name ?? "Debug"; } catch { return "Debug"; }
    }
    private static Project ResolveStartupProject(DTE dte)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        string unique = (dte.Solution.SolutionBuild.StartupProjects as Array)?.Cast<object>().Select(x => x?.ToString()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        if (string.IsNullOrWhiteSpace(unique)) return null;
        foreach (Project p in dte.Solution.Projects) if (string.Equals(p.UniqueName, unique, StringComparison.OrdinalIgnoreCase)) return p;
        return null;
    }
    private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";
}
