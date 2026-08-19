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
        if (!BootStartup.Initialize(boot))
            return KernelPanic.IsConfigured()
                ? KernelPanic.Raise(KernelPanicCode.Unknown,"boot-startup-failure","BootStartup.Initialize failed.",true,true,KernelPanicPolicy.DebuggerThenHalt)
                : false;
        if (!HardwareAbstractionLayer.Initialize())
            return KernelPanic.Raise(KernelPanicCode.DriverFailure,"hardware-initialization-failure","HardwareAbstractionLayer.Initialize failed.",true,true,KernelPanicPolicy.DebuggerThenHalt);
        if (!KernelStructuredLogging.InfoLine("console","Kernel.KMain","Interactive console ready. Defaults: font 3, buffering auto (double for text).")) return false;
        if (!KernelCommandLine.Initialize())
            return KernelPanic.Raise(KernelPanicCode.Unknown,"console-initialization-failure","KernelCommandLine.Initialize failed.",true,true,KernelPanicPolicy.DebuggerThenHalt);
        if (!KernelInterruptDispatch.Enable())
            return KernelPanic.Raise(KernelPanicCode.Unknown,"interrupt-enable-failure","KernelInterruptDispatch.Enable failed.",true,true,KernelPanicPolicy.DebuggerThenHalt);
        if (!KernelConsole.RunInteractive())
            return KernelPanic.Raise(KernelPanicCode.Unknown,"interactive-console-failure","KernelConsole.RunInteractive returned unexpectedly.",true,true,KernelPanicPolicy.DebuggerThenHalt);
        return true;
    }
}
