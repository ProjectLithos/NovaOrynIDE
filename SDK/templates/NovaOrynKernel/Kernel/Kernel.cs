using System;
using NovaOryn.Kernel.Console;
using NovaOryn.Kernel.Contracts;
using NovaOryn.Kernel.CommandLine;
using NovaOryn.Kernel.InterruptDispatch;
using NovaOryn.Kernel.Bootstrap.Boot;
using NovaOryn.Kernel.Bootstrap.HAL;

namespace NovaOryn.Kernel.Bootstrap;

/// <summary>Defines the high-level NovaOryn kernel entry and delegates startup to Boot and HAL.</summary>
public static class Kernel
{
    /// <summary>Boots the configured NovaOryn runtime, initializes hardware/services, then enters the interactive console.</summary>
    public static Boolean KMain(BootContext boot)
    {
        if (!BootStartup.Initialize(boot)) return false;
        if (!HardwareAbstractionLayer.Initialize()) return false;
        if (!KernelLog.Info("console","Kernel.KMain","Interactive console ready. Defaults: font 3, buffering auto (double for text).")) return false;
        if (!KernelCommandLine.Initialize()) return false;
        if (!KernelInterruptDispatch.Enable()) return false;
        return KernelConsole.RunInteractive();
    }
}
