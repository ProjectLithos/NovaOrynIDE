using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
namespace NovaOryn.VisualStudio;
/// <summary>
/// Provides the NovaOryn Visual Studio package and registers its build and run command handlers.
/// </summary>
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration("NovaOryn OS SDK", "Visual Studio integration for NovaOryn kernels", "0.41.4")]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
[Guid(PackageIds.PackageGuidString)]
public sealed class NovaOrynPackage : AsyncPackage
{
    private IVsRegisterPriorityCommandTarget _registration;
    private NovaOrynPriorityLaunchCommandTarget _target;
    private uint _cookie;
    private DTE _dte;
    private SolutionEvents _solutionEvents;
    internal NovaOrynOutputPane Output { get; private set; }
    internal DTE Dte => _dte;
    /// <summary>
    /// Initialises the NovaOryn Visual Studio services and command targets.
    /// </summary>
    /// <param name="token">A cancellation token supplied by Visual Studio.</param>
    /// <param name="progress">Visual Studio package-load progress reporting.</param>
    /// <returns>A task representing package initialisation.</returns>
    protected override async Task InitializeAsync(CancellationToken token, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(token);
        Output = await NovaOrynOutputPane.CreateAsync(this, token);
        _dte = await GetServiceAsync(typeof(SDTE)) as DTE;
        if (_dte != null)
        {
            NovaOrynSolutionSynchronizer.QueueUserlandProjects(this, _dte, Output);
            _solutionEvents = _dte.Events.SolutionEvents;
            _solutionEvents.ProjectAdded += OnProjectAdded;
        }
        await NovaOrynCommands.InitializeAsync(this, token);
        _registration = await GetServiceAsync(typeof(SVsRegisterPriorityCommandTarget)) as IVsRegisterPriorityCommandTarget
            ?? throw new InvalidOperationException("Visual Studio priority command service is unavailable.");
        _target = new NovaOrynPriorityLaunchCommandTarget(new NovaOrynLaunchService(this));
        ErrorHandler.ThrowOnFailure(_registration.RegisterPriorityCommandTarget(0, _target, out _cookie));
        Output.WriteLine("[ OK ] NovaOryn Visual Studio extension 0.41.4 loaded.");
    }
    private void OnProjectAdded(Project project)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (_dte == null) return;
        NovaOrynSolutionSynchronizer.QueueUserlandProjects(this, _dte, Output);
        JoinableTaskFactory.RunAsync(async delegate
        {
            await Task.Delay(700);
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            try { new NovaOrynConfigurationService(this, _dte).ShowForProject(project, true); }
            catch (Exception exception) { Output.WriteLine("[FAIL] NovaOryn Configuration Pages: " + exception.Message); }
        }).FileAndForget("NovaOryn/ConfigurationPages");
    }

    /// <summary>
    /// Releases the registered Visual Studio command target and package resources.
    /// </summary>
    /// <param name="disposing">True when managed resources should be released.</param>
    protected override void Dispose(bool disposing)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (disposing && _solutionEvents != null) _solutionEvents.ProjectAdded -= OnProjectAdded;
        if (disposing && _registration != null && _cookie != 0) _registration.UnregisterPriorityCommandTarget(_cookie);
        base.Dispose(disposing);
    }
}
