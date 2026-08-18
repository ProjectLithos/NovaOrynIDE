using System;
using NovaOryn.Kernel.Console;
using NovaOryn.Kernel.Contracts;
using NovaOryn.Kernel.Smp;
using NovaOryn.Kernel.Scheduler;
using NovaOryn.Kernel.Time;

namespace NovaOryn.Kernel.Bootstrap;

/// <summary>Formats structured kernel log records for the serial/framebuffer console.</summary>
public sealed class KernelConsoleLogSink : IKernelLogSink
{
    public Boolean TryWrite(KernelLogRecord record)
    {
        if (!KernelConsole.Write("[")) return false;
        if (!KernelConsole.Write(LevelName(record.Level))) return false;
        if (!KernelConsole.Write("][t=")) return false;
        if (!KernelConsole.WriteUInt64(record.TimestampNanoseconds)) return false;
        if (!KernelConsole.Write("][cpu=")) return false;
        if (!KernelConsole.WriteUInt64(record.Cpu)) return false;
        if (!KernelConsole.Write("][thread=")) return false;
        if (!KernelConsole.WriteUInt64(record.ThreadId)) return false;
        if (!KernelConsole.Write("][process=")) return false;
        if (!KernelConsole.WriteUInt64(record.ProcessId)) return false;
        if (!KernelConsole.Write("][")) return false;
        if (!KernelConsole.Write(record.Subsystem)) return false;
        if (!KernelConsole.Write("][")) return false;
        if (!KernelConsole.Write(record.Source)) return false;
        if (!KernelConsole.Write("] ")) return false;
        return KernelConsole.WriteLine(record.Message);
    }

    private static String LevelName(KernelLogLevel level)
    {
        switch (level)
        {
            case KernelLogLevel.Trace: return "TRACE";
            case KernelLogLevel.Debug: return "DEBUG";
            case KernelLogLevel.Info: return "INFO";
            case KernelLogLevel.Warning: return "WARN";
            case KernelLogLevel.Error: return "ERROR";
            case KernelLogLevel.Critical: return "CRITICAL";
            default: return "UNKNOWN";
        }
    }
}

/// <summary>Supplies live CPU/thread/time context without making logging depend on scheduler availability during early boot.</summary>
public sealed class KernelLogContextProvider : IKernelLogContextProvider
{
    public Boolean TryGetContext(out UInt32 cpu, out UInt64 threadId, out UInt64 processId, out UInt64 timestampNanoseconds)
    {
        cpu = 0U;
        threadId = 0UL;
        processId = 0UL;
        timestampNanoseconds = KernelTime.IsInitialized ? KernelTime.GetMonotonicNanoseconds() : 0UL;

        if (KernelSmp.IsInitialized() && KernelSmp.TryGetCurrentProcessor(out KernelProcessorState processor))
            cpu = processor.Index;

        if (KernelScheduler.IsInitialized())
            KernelScheduler.TryGetCurrentThreadId(out threadId);

        // Process ownership is intentionally zero for kernel-only threads until a scheduler/process binding exists.
        return true;
    }
}
