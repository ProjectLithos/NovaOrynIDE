using System;
using NovaOryn.Kernel.Console;
using NovaOryn.Kernel.Contracts;
using NovaOryn.Kernel.Smp;
using NovaOryn.Kernel.Scheduler;
using NovaOryn.Kernel.Processes;
using NovaOryn.Kernel.Time;

namespace NovaOryn.Kernel.Bootstrap;

/// <summary>Formats structured kernel records for serial and framebuffer diagnostics.</summary>
public sealed class KernelConsoleLogSink : IKernelLogSink
{
    public Boolean TryWrite(KernelLogRecord record)
    {
        if (!WritePrefix(record.Level, record.Subsystem, record.Source, record.Cpu, record.ThreadId, record.ProcessId, record.TimestampNanoseconds)) return false;
        return KernelConsole.WriteLine(record.Message);
    }

    internal static Boolean WritePrefix(KernelLogLevel level,String subsystem,String source,UInt32 cpu,UInt64 thread,UInt64 process,UInt64 time)
    {
        if (!KernelConsole.Write("[")) return false;
        if (!KernelConsole.Write(LevelName(level))) return false;
        if (!KernelConsole.Write("][t=")) return false;
        if (!KernelConsole.WriteUInt64(time)) return false;
        if (!KernelConsole.Write("][cpu=")) return false;
        if (!KernelConsole.WriteUInt64(cpu)) return false;
        if (!KernelConsole.Write("][thread=")) return false;
        if (!KernelConsole.WriteUInt64(thread)) return false;
        if (!KernelConsole.Write("][process=")) return false;
        if (!KernelConsole.WriteUInt64(process)) return false;
        if (!KernelConsole.Write("][")) return false;
        if (!KernelConsole.Write(subsystem ?? String.Empty)) return false;
        if (!KernelConsole.Write("][")) return false;
        if (!KernelConsole.Write(source ?? String.Empty)) return false;
        return KernelConsole.Write("] ");
    }

    private static String LevelName(KernelLogLevel level)
    {
        switch(level){case KernelLogLevel.Trace:return "TRACE";case KernelLogLevel.Debug:return "DEBUG";case KernelLogLevel.Info:return "INFO";case KernelLogLevel.Warning:return "WARN";case KernelLogLevel.Error:return "ERROR";case KernelLogLevel.Critical:return "CRITICAL";default:return "UNKNOWN";}
    }
}

/// <summary>Provides live structured-log context as platform services become available.</summary>
public sealed class KernelLogContextProvider : IKernelLogContextProvider
{
    public Boolean TryGetContext(out UInt32 cpu,out UInt64 threadId,out UInt64 processId,out UInt64 timestampNanoseconds)
    {
        cpu=0U;threadId=0UL;processId=0UL;timestampNanoseconds=KernelTime.IsInitialized?KernelTime.GetMonotonicNanoseconds():0UL;
        if(KernelSmp.IsInitialized()&&KernelSmp.TryGetCurrentProcessor(out KernelProcessorState processor))cpu=processor.Index;
        if(KernelScheduler.IsInitialized())KernelScheduler.TryGetCurrentThreadId(out threadId);
        if(KernelProcesses.IsInitialized())KernelProcesses.TryGetCurrentProcessId(out processId);
        return true;
    }
}

/// <summary>Initializes the generated-kernel logging route and prefixes dynamic diagnostic lines.</summary>
public static class KernelStructuredLogging
{
    private static readonly KernelLogContextProvider Context=new KernelLogContextProvider();
    public static Boolean Initialize()=>KernelLog.Configure(new KernelConsoleLogSink(),Context,KernelLogLevel.Trace);
    public static Boolean Begin(KernelLogLevel level,String subsystem,String source)
    {
        Context.TryGetContext(out UInt32 cpu,out UInt64 thread,out UInt64 process,out UInt64 time);
        return KernelConsoleLogSink.WritePrefix(level,subsystem,source,cpu,thread,process,time);
    }
    public static Boolean InfoLine(String subsystem,String source,String message)=>KernelLog.Info(subsystem,source,message);
    public static Boolean DebugLine(String subsystem,String source,String message)=>KernelLog.Debug(subsystem,source,message);
    public static Boolean WarningLine(String subsystem,String source,String message)=>KernelLog.Warning(subsystem,source,message);
    public static Boolean ErrorLine(String subsystem,String source,String message)=>KernelLog.Error(subsystem,source,message);
    public static Boolean CriticalLine(String subsystem,String source,String message)=>KernelLog.Critical(subsystem,source,message);
}
