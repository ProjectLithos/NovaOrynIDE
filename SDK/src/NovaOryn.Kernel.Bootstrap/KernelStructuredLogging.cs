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

/// <summary>Streams the official NovaOryn kernel telemetry protocol to the debugger/serial console.</summary>
public sealed class KernelConsoleTelemetrySink : IKernelTelemetrySink
{
    public Boolean TryEmit(KernelTelemetryEvent record)
    {
        switch(record.Kind)
        {
            case KernelTelemetryKind.Trace:
                return Prefix("TRACE") && Field("category",record.Subsystem) && Field("name",record.Name) && Common(record) && Detail(record.Detail);
            case KernelTelemetryKind.BootEvent:
                return Prefix("BOOT") && Field("stage",record.Name) && Text(" phase=") && Text(PhaseName((KernelBootPhase)record.Value1)) && Number(" stage_id",record.Value0) && Common(record) && Detail(record.Detail);
            case KernelTelemetryKind.Profile:
                return Prefix("PROFILE") && Text(" kind=sample") && Field("category",record.Subsystem) && Field("name",record.Name) && Number(" samples",record.Value0) && Number(" duration_ns",record.Value1) && Common(record) && End();
            case KernelTelemetryKind.Counter:
                return Prefix("PROFILE") && Text(" kind=counter") && Field("category",record.Subsystem) && Field("name",record.Name) && Number(" delta",record.Value0) && Common(record) && End();
            case KernelTelemetryKind.DiagnosticEvent:
                return Prefix("TRACE") && Text(" category=diagnostic") && Field("name",record.Name) && Field("subsystem",record.Subsystem) && Number(" code",record.Value0) && Common(record) && Detail(record.Detail);
            default:return false;
        }
    }
    private static Boolean Prefix(String kind)=>Text("[NOVAORYN:")&&Text(kind)&&Text("]");
    private static Boolean Common(KernelTelemetryEvent record)=>Number(" timestamp_ns",record.TimestampNanoseconds)&&Number(" cpu",record.Cpu)&&Number(" thread",record.ThreadId)&&Number(" process",record.ProcessId)&&Number(" correlation",record.CorrelationId);
    private static Boolean Field(String name,String value)=>Text(" ")&&Text(name)&&Text("=\"")&&Text(value??String.Empty)&&Text("\"");
    private static Boolean Number(String name,UInt64 value)=>Text(name)&&Text("=")&&KernelConsole.WriteUInt64(value);
    private static Boolean Detail(String value)=>Field("details",value)&&End();
    private static Boolean Text(String value)=>KernelConsole.Write(value);
    private static Boolean End()=>KernelConsole.WriteLine(String.Empty);
    private static String PhaseName(KernelBootPhase phase)
    {
        switch(phase){case KernelBootPhase.Begin:return "begin";case KernelBootPhase.End:return "end";case KernelBootPhase.Warning:return "warning";case KernelBootPhase.Failed:return "failed";default:return "instant";}
    }
}
