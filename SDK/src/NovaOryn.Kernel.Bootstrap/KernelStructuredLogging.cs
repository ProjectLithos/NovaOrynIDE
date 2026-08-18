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



/// <summary>Streams versioned structured telemetry in the wire format consumed directly by NovaOryn IDE.</summary>
public sealed class KernelConsoleTelemetrySink : IKernelTelemetrySink
{
    public Boolean TryEmit(KernelTelemetryEvent telemetryEvent)
    {
        switch (telemetryEvent.Kind)
        {
            case KernelTelemetryKind.BootEvent: return WriteBoot(telemetryEvent);
            case KernelTelemetryKind.Profile: return WriteProfile(telemetryEvent);
            case KernelTelemetryKind.Counter: return WriteCounter(telemetryEvent);
            case KernelTelemetryKind.DiagnosticEvent: return WriteTrace(telemetryEvent, "diagnostic");
            case KernelTelemetryKind.Trace: return WriteTrace(telemetryEvent, telemetryEvent.Subsystem);
            default: return false;
        }
    }

    private static Boolean WriteCommon(KernelTelemetryEvent telemetryEvent)
    {
        if (!KernelConsole.Write(" cpu=")) return false;
        if (!KernelConsole.WriteUInt64(telemetryEvent.Cpu)) return false;
        if (!KernelConsole.Write(" thread=")) return false;
        if (!KernelConsole.WriteUInt64(telemetryEvent.ThreadId)) return false;
        if (!KernelConsole.Write(" process=")) return false;
        if (!KernelConsole.WriteUInt64(telemetryEvent.ProcessId)) return false;
        if (!KernelConsole.Write(" seq=")) return false;
        return KernelConsole.WriteUInt64(telemetryEvent.CorrelationId);
    }

    private static Boolean WriteTimestamp(KernelTelemetryEvent telemetryEvent)
    {
        if (!KernelConsole.Write(" timestamp_ms=")) return false;
        UInt64 milliseconds = telemetryEvent.TimestampNanoseconds / 1000000UL;
        return KernelConsole.WriteUInt64(milliseconds);
    }

    private static Boolean WriteDetails(String detail)
    {
        if (detail==null||detail.Length==0) return true;
        if (!KernelConsole.Write(" details=\"")) return false;
        if (!KernelConsole.Write(detail)) return false;
        return KernelConsole.Write("\"");
    }

    private static String PhaseName(KernelTelemetryPhase phase)
    {
        switch (phase)
        {
            case KernelTelemetryPhase.Begin: return "begin";
            case KernelTelemetryPhase.End: return "end";
            default: return "instant";
        }
    }

    private static Boolean WriteTrace(KernelTelemetryEvent telemetryEvent, String category)
    {
        if (!KernelConsole.Write("[NOVAORYN:TRACE] category=")) return false;
        if (!KernelConsole.Write(category)) return false;
        if (!KernelConsole.Write(" name=\"")) return false;
        if (!KernelConsole.Write(telemetryEvent.Name)) return false;
        if (!KernelConsole.Write("\" phase=")) return false;
        if (!KernelConsole.Write(PhaseName(telemetryEvent.Phase))) return false;
        if (!WriteTimestamp(telemetryEvent)) return false;
        if (!WriteCommon(telemetryEvent)) return false;
        if (telemetryEvent.Kind == KernelTelemetryKind.DiagnosticEvent)
        {
            if (!KernelConsole.Write(" code=")) return false;
            if (!KernelConsole.WriteUInt64(telemetryEvent.Value0)) return false;
        }
        else if (telemetryEvent.Value0 != 0UL)
        {
            if (!KernelConsole.Write(" duration_ms=")) return false;
            if (!KernelConsole.WriteUInt64(telemetryEvent.Value0 / 1000000UL)) return false;
        }
        if (!WriteDetails(telemetryEvent.Detail)) return false;
        return KernelConsole.WriteLine("");
    }

    private static Boolean WriteBoot(KernelTelemetryEvent telemetryEvent)
    {
        if (!KernelConsole.Write("[NOVAORYN:BOOT] stage=\"")) return false;
        if (!KernelConsole.Write(telemetryEvent.Name)) return false;
        if (!KernelConsole.Write("\" phase=")) return false;
        if (!KernelConsole.Write(PhaseName(telemetryEvent.Phase))) return false;
        if (!WriteTimestamp(telemetryEvent)) return false;
        if (!WriteCommon(telemetryEvent)) return false;
        if (telemetryEvent.Phase == KernelTelemetryPhase.End)
        {
            if (!KernelConsole.Write(" status=complete duration_ms=")) return false;
            if (!KernelConsole.WriteUInt64(telemetryEvent.Value0 / 1000000UL)) return false;
        }
        if (!WriteDetails(telemetryEvent.Detail)) return false;
        return KernelConsole.WriteLine("");
    }

    private static Boolean WriteProfile(KernelTelemetryEvent telemetryEvent)
    {
        if (!KernelConsole.Write("[NOVAORYN:PROFILE] kind=sample category=")) return false;
        if (!KernelConsole.Write(telemetryEvent.Subsystem)) return false;
        if (!KernelConsole.Write(" name=\"")) return false;
        if (!KernelConsole.Write(telemetryEvent.Name)) return false;
        if (!KernelConsole.Write("\" samples=")) return false;
        if (!KernelConsole.WriteUInt64(telemetryEvent.Value0)) return false;
        if (!KernelConsole.Write(" duration_ms=")) return false;
        if (!KernelConsole.WriteUInt64(telemetryEvent.Value1 / 1000000UL)) return false;
        if (!WriteTimestamp(telemetryEvent)) return false;
        if (!WriteCommon(telemetryEvent)) return false;
        if (!WriteDetails(telemetryEvent.Detail)) return false;
        return KernelConsole.WriteLine("");
    }

    private static Boolean WriteCounter(KernelTelemetryEvent telemetryEvent)
    {
        if (!KernelConsole.Write("[NOVAORYN:PROFILE] kind=counter category=")) return false;
        if (!KernelConsole.Write(telemetryEvent.Subsystem)) return false;
        if (!KernelConsole.Write(" name=\"")) return false;
        if (!KernelConsole.Write(telemetryEvent.Name)) return false;
        if (!KernelConsole.Write("\" delta=")) return false;
        if (!KernelConsole.WriteUInt64(telemetryEvent.Value0)) return false;
        if (!WriteTimestamp(telemetryEvent)) return false;
        if (!WriteCommon(telemetryEvent)) return false;
        if (!WriteDetails(telemetryEvent.Detail)) return false;
        return KernelConsole.WriteLine("");
    }
}

/// <summary>Supplies live CPU/thread/time context without making logging depend on scheduler availability during early boot.</summary>
public sealed class KernelLogContextProvider : IKernelLogContextProvider, IKernelTelemetryContextProvider
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
