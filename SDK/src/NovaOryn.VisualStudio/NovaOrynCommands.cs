using System;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
namespace NovaOryn.VisualStudio;
internal static class NovaOrynCommands
{
    public static async Task InitializeAsync(NovaOrynPackage package, CancellationToken token)
    {
        await package.JoinableTaskFactory.SwitchToMainThreadAsync(token);
        var service = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService
            ?? throw new InvalidOperationException("Visual Studio command service is unavailable.");
        var launch = new NovaOrynLaunchService(package);
        service.AddCommand(new MenuCommand((_, _) => launch.QueueLaunch(true), new CommandID(PackageIds.CommandSet, PackageIds.BuildCommand)));
        service.AddCommand(new MenuCommand((_, _) => launch.QueueLaunch(false), new CommandID(PackageIds.CommandSet, PackageIds.RunCommand)));
        var configuration = new NovaOrynConfigurationService(package, package.Dte);
        service.AddCommand(new MenuCommand((_, _) => configuration.ShowForSelectedProject(), new CommandID(PackageIds.CommandSet, PackageIds.ConfigureCommand)));
    }
}
