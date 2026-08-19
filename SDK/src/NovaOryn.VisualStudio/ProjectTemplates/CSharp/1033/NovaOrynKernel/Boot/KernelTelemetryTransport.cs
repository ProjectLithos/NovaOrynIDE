using System;
using NovaOryn.Kernel.Console;
using NovaOryn.Kernel.Contracts;
using NovaOryn.Kernel.Smp;
using NovaOryn.Kernel.Scheduler;
using NovaOryn.Kernel.Processes;
using NovaOryn.Kernel.Time;

namespace NovaOryn.Kernel.Bootstrap;

/// <summary>Supplies live execution context to the structured telemetry contract after the kernel heap is online.</summary>
public sealed class KernelTelemetryContextProvider : IKernelLogContextProvider
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

/// <summary>Serializes the official NovaOryn structured telemetry protocol to the kernel console/debug stream.</summary>
public sealed class KernelConsoleTelemetrySink : IKernelTelemetrySink
{
    public Boolean TryEmit(KernelTelemetryEvent telemetryEvent)
    {
        if(!KernelConsole.Write("[NOVAORYN:"))return false;
        if(!KernelConsole.Write(Prefix(telemetryEvent.Kind)))return false;
        if(!KernelConsole.Write("] timestamp_ns="))return false;
        if(!KernelConsole.WriteUInt64(telemetryEvent.TimestampNanoseconds))return false;
        if(!KernelConsole.Write(" sequence="))return false;
        if(!KernelConsole.WriteUInt64(telemetryEvent.CorrelationId))return false;
        if(!KernelConsole.Write(" cpu="))return false;
        if(!KernelConsole.WriteUInt64(telemetryEvent.Cpu))return false;
        if(!KernelConsole.Write(" thread="))return false;
        if(!KernelConsole.WriteUInt64(telemetryEvent.ThreadId))return false;
        if(!KernelConsole.Write(" process="))return false;
        if(!KernelConsole.WriteUInt64(telemetryEvent.ProcessId))return false;
        if(!WriteFields(telemetryEvent))return false;
        return KernelConsole.WriteLine("");
    }

    private static Boolean WriteFields(KernelTelemetryEvent telemetryEvent)
    {
        switch(telemetryEvent.Kind)
        {
            case KernelTelemetryKind.Trace:
                return WriteNamed(" category=",telemetryEvent.Subsystem," name=",telemetryEvent.Name," details=",telemetryEvent.Detail);
            case KernelTelemetryKind.Profile:
                if(!KernelConsole.Write(" kind=sample category="))return false;
                if(!WriteQuoted(telemetryEvent.Subsystem))return false;
                if(!KernelConsole.Write(" function="))return false;
                if(!WriteQuoted(telemetryEvent.Name))return false;
                if(!KernelConsole.Write(" samples="))return false;
                if(!KernelConsole.WriteUInt64(telemetryEvent.Value0))return false;
                if(!KernelConsole.Write(" duration_ns="))return false;
                return KernelConsole.WriteUInt64(telemetryEvent.Value1);
            case KernelTelemetryKind.BootEvent:
                if(!KernelConsole.Write(" stage="))return false;
                if(!WriteQuoted(telemetryEvent.Name))return false;
                if(!KernelConsole.Write(" stage_id="))return false;
                if(!KernelConsole.WriteUInt64(telemetryEvent.Value0))return false;
                if(!KernelConsole.Write(" phase="))return false;
                if(!KernelConsole.Write(BootPhase((KernelBootPhase)telemetryEvent.Value1)))return false;
                if(!KernelConsole.Write(" details="))return false;
                return WriteQuoted(telemetryEvent.Detail);
            case KernelTelemetryKind.Counter:
                if(!KernelConsole.Write(" category="))return false;
                if(!WriteQuoted(telemetryEvent.Subsystem))return false;
                if(!KernelConsole.Write(" name="))return false;
                if(!WriteQuoted(telemetryEvent.Name))return false;
                if(!KernelConsole.Write(" value="))return false;
                return KernelConsole.WriteUInt64(telemetryEvent.Value0);
            case KernelTelemetryKind.DiagnosticEvent:
                if(!KernelConsole.Write(" category="))return false;
                if(!WriteQuoted(telemetryEvent.Subsystem))return false;
                if(!KernelConsole.Write(" name="))return false;
                if(!WriteQuoted(telemetryEvent.Name))return false;
                if(!KernelConsole.Write(" code="))return false;
                if(!KernelConsole.WriteUInt64(telemetryEvent.Value0))return false;
                if(!KernelConsole.Write(" details="))return false;
                return WriteQuoted(telemetryEvent.Detail);
            default:return false;
        }
    }

    private static Boolean WriteNamed(String firstName,String firstValue,String secondName,String secondValue,String thirdName,String thirdValue)
    {
        if(!KernelConsole.Write(firstName))return false;if(!WriteQuoted(firstValue))return false;
        if(!KernelConsole.Write(secondName))return false;if(!WriteQuoted(secondValue))return false;
        if(!KernelConsole.Write(thirdName))return false;return WriteQuoted(thirdValue);
    }
    private static Boolean WriteQuoted(String value)
    {
        if(!KernelConsole.Write("\""))return false;
        if(!KernelConsole.Write(value??String.Empty))return false;
        return KernelConsole.Write("\"");
    }
    private static String Prefix(KernelTelemetryKind kind)
    {
        switch(kind){case KernelTelemetryKind.Trace:return "TRACE";case KernelTelemetryKind.Profile:return "PROFILE";case KernelTelemetryKind.BootEvent:return "BOOT";case KernelTelemetryKind.Counter:return "COUNTER";case KernelTelemetryKind.DiagnosticEvent:return "DIAGNOSTIC";default:return "UNKNOWN";}
    }
    private static String BootPhase(KernelBootPhase phase)
    {
        switch(phase){case KernelBootPhase.Instant:return "instant";case KernelBootPhase.Begin:return "begin";case KernelBootPhase.End:return "end";case KernelBootPhase.Warning:return "warning";case KernelBootPhase.Failed:return "failed";default:return "instant";}
    }
}
