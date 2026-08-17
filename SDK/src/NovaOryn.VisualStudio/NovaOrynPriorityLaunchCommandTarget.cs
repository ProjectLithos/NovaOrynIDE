using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
namespace NovaOryn.VisualStudio;
internal sealed class NovaOrynPriorityLaunchCommandTarget : IOleCommandTarget
{
    private readonly NovaOrynLaunchService _launch;
    public NovaOrynPriorityLaunchCommandTarget(NovaOrynLaunchService launch) => _launch = launch ?? throw new ArgumentNullException(nameof(launch));
    public int QueryStatus(ref Guid group, uint count, OLECMD[] commands, IntPtr text)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (group != VSConstants.GUID_VSStandardCommandSet97 || commands == null || count == 0 || !_launch.TryGetActiveProject(out _))
            return (int)Constants.OLECMDERR_E_NOTSUPPORTED;
        bool handled = false;
        for (uint i = 0; i < Math.Min(count, (uint)commands.Length); i++)
            if (commands[i].cmdID == (uint)VSConstants.VSStd97CmdID.Start || commands[i].cmdID == (uint)VSConstants.VSStd97CmdID.StartNoDebug)
            { commands[i].cmdf = (uint)(OLECMDF.OLECMDF_SUPPORTED | OLECMDF.OLECMDF_ENABLED); handled = true; }
        return handled ? VSConstants.S_OK : (int)Constants.OLECMDERR_E_NOTSUPPORTED;
    }
    public int Exec(ref Guid group, uint id, uint options, IntPtr input, IntPtr output)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (group != VSConstants.GUID_VSStandardCommandSet97 || !_launch.TryGetActiveProject(out _)) return (int)Constants.OLECMDERR_E_NOTSUPPORTED;
        if (id == (uint)VSConstants.VSStd97CmdID.Start) { _launch.QueueLaunch(false); return VSConstants.S_OK; }
        if (id == (uint)VSConstants.VSStd97CmdID.StartNoDebug) { _launch.QueueLaunch(false); return VSConstants.S_OK; }
        return (int)Constants.OLECMDERR_E_NOTSUPPORTED;
    }
}
