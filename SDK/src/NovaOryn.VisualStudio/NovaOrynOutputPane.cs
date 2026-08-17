using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
namespace NovaOryn.VisualStudio;
internal sealed class NovaOrynOutputPane
{
    private static readonly Guid PaneGuid = new("654f85ab-ab82-48c6-86a0-75cfe7600c06");
    private readonly IVsOutputWindowPane _pane;
    private NovaOrynOutputPane(IVsOutputWindowPane pane) => _pane = pane;
    public static async Task<NovaOrynOutputPane> CreateAsync(AsyncPackage package, CancellationToken token)
    {
        await package.JoinableTaskFactory.SwitchToMainThreadAsync(token);
        var output = await package.GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow
            ?? throw new InvalidOperationException("Visual Studio output service is unavailable.");
        Guid guid = PaneGuid;
        output.CreatePane(ref guid, "NovaOryn OS SDK", 1, 1);
        output.GetPane(ref guid, out IVsOutputWindowPane pane);
        return new NovaOrynOutputPane(pane ?? throw new InvalidOperationException("NovaOryn output pane was not created."));
    }
    public bool WriteLine(string message)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return _pane.OutputStringThreadSafe((message ?? string.Empty) + Environment.NewLine) >= 0;
    }
    public bool Activate()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return _pane.Activate() >= 0;
    }
}
