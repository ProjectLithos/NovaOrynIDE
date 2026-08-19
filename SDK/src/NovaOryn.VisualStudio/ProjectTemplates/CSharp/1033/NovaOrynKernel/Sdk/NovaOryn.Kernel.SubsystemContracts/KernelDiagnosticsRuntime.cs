using System;

namespace NovaOryn.Kernel.Contracts;

public static class KernelLog
{
    private const Int32 MaximumSinks=4;
    private static readonly IKernelLogSink[] _sinks=new IKernelLogSink[MaximumSinks];
    private static IKernelLogContextProvider _context;
    private static KernelLogLevel _minimum=KernelLogLevel.Info;
    private static UInt64 _trace,_debug,_info,_warning,_error,_critical,_dropped;

    public static Boolean Configure(IKernelLogSink sink,IKernelLogContextProvider context,KernelLogLevel minimum)
    {
        if(sink==null)return false;
        RemoveAllSinks();
        _sinks[0]=sink;
        _context=context;
        _minimum=minimum;
        return true;
    }
    public static Boolean AddSink(IKernelLogSink sink)
    {
        if(sink==null)return false;
        for(Int32 i=0;i<MaximumSinks;i++)
        {
            if(_sinks[i]==sink)return true;
            if(_sinks[i]==null){_sinks[i]=sink;return true;}
        }
        return false;
    }
    public static Boolean RemoveAllSinks(){for(Int32 i=0;i<MaximumSinks;i++)_sinks[i]=null;return true;}
    public static Boolean SetMinimumLevel(KernelLogLevel minimum){_minimum=minimum;return true;}
    public static KernelLogLevel GetMinimumLevel()=>_minimum;
    public static Boolean IsConfigured(){for(Int32 i=0;i<MaximumSinks;i++)if(_sinks[i]!=null)return true;return false;}
    public static KernelLogStatistics GetStatistics()=>new KernelLogStatistics(_trace,_debug,_info,_warning,_error,_critical,_dropped);
    public static Boolean ResetStatistics(){_trace=_debug=_info=_warning=_error=_critical=_dropped=0UL;return true;}

    public static Boolean Write(KernelLogLevel level,String subsystem,String source,String message)
    {
        if(level<_minimum)return true;
        UInt32 cpu=0;UInt64 thread=0,process=0,time=0;
        if(_context!=null)_context.TryGetContext(out cpu,out thread,out process,out time);
        KernelLogRecord record=new KernelLogRecord(level,subsystem??String.Empty,cpu,thread,process,time,source??String.Empty,message??String.Empty);
        Boolean any=false,ok=true;
        for(Int32 i=0;i<MaximumSinks;i++)
        {
            IKernelLogSink sink=_sinks[i];
            if(sink==null)continue;
            any=true;
            if(!sink.TryWrite(record))ok=false;
        }
        Count(level);
        if(!any||!ok)_dropped++;
        return any&&ok;
    }
    private static Boolean Count(KernelLogLevel level)
    {
        switch(level){case KernelLogLevel.Trace:_trace++;break;case KernelLogLevel.Debug:_debug++;break;case KernelLogLevel.Info:_info++;break;case KernelLogLevel.Warning:_warning++;break;case KernelLogLevel.Error:_error++;break;case KernelLogLevel.Critical:_critical++;break;default:return false;}return true;
    }
    public static Boolean Trace(String subsystem,String source,String message)=>Write(KernelLogLevel.Trace,subsystem,source,message);
    public static Boolean Debug(String subsystem,String source,String message)=>Write(KernelLogLevel.Debug,subsystem,source,message);
    public static Boolean Info(String subsystem,String source,String message)=>Write(KernelLogLevel.Info,subsystem,source,message);
    public static Boolean Warning(String subsystem,String source,String message)=>Write(KernelLogLevel.Warning,subsystem,source,message);
    public static Boolean Error(String subsystem,String source,String message)=>Write(KernelLogLevel.Error,subsystem,source,message);
    public static Boolean Critical(String subsystem,String source,String message)=>Write(KernelLogLevel.Critical,subsystem,source,message);
}

public static class KernelTelemetry
{
    private const Int32 MaximumSinks=4;
    private static readonly IKernelTelemetrySink[] _sinks=new IKernelTelemetrySink[MaximumSinks];
    private static IKernelLogContextProvider _context;
    private static UInt64 _sequence=1,_trace,_profile,_boot,_counter,_diagnostic,_dropped;

    public static Boolean Configure(IKernelTelemetrySink sink){return Configure(sink,null);}
    public static Boolean Configure(IKernelTelemetrySink sink,IKernelLogContextProvider context)
    {
        if(sink==null)return false;
        RemoveAllSinks();_sinks[0]=sink;_context=context;return true;
    }
    public static Boolean AddSink(IKernelTelemetrySink sink)
    {
        if(sink==null)return false;
        for(Int32 i=0;i<MaximumSinks;i++){if(_sinks[i]==sink)return true;if(_sinks[i]==null){_sinks[i]=sink;return true;}}
        return false;
    }
    public static Boolean RemoveAllSinks(){for(Int32 i=0;i<MaximumSinks;i++)_sinks[i]=null;return true;}
    public static KernelTelemetryStatistics GetStatistics()=>new KernelTelemetryStatistics(_trace,_profile,_boot,_counter,_diagnostic,_dropped);
    public static Boolean ResetStatistics(){_trace=_profile=_boot=_counter=_diagnostic=_dropped=0;return true;}

    public static Boolean Emit(KernelTelemetryKind kind,String subsystem,String name,UInt64 timestamp,UInt64 value0=0,UInt64 value1=0,String detail="")
    {
        UInt32 cpu=0;UInt64 thread=0,process=0,contextTime=0;
        if(_context!=null)_context.TryGetContext(out cpu,out thread,out process,out contextTime);
        if(timestamp==0)timestamp=contextTime;
        KernelTelemetryEvent record=new KernelTelemetryEvent(kind,subsystem??String.Empty,name??String.Empty,timestamp,value0,value1,_sequence++,detail??String.Empty,cpu,thread,process);
        Boolean any=false,ok=true;
        for(Int32 i=0;i<MaximumSinks;i++){IKernelTelemetrySink sink=_sinks[i];if(sink==null)continue;any=true;if(!sink.TryEmit(record))ok=false;}
        Count(kind);if(!any||!ok)_dropped++;return any&&ok;
    }
    private static Boolean Count(KernelTelemetryKind kind)
    {
        switch(kind){case KernelTelemetryKind.Trace:_trace++;break;case KernelTelemetryKind.Profile:_profile++;break;case KernelTelemetryKind.BootEvent:_boot++;break;case KernelTelemetryKind.Counter:_counter++;break;case KernelTelemetryKind.DiagnosticEvent:_diagnostic++;break;default:return false;}return true;
    }
    public static Boolean KernelTrace(String subsystem,String name,String detail="")=>Emit(KernelTelemetryKind.Trace,subsystem,name,0,0,0,detail);
    public static Boolean KernelTrace(String subsystem,String name,UInt64 timestamp,String detail="")=>Emit(KernelTelemetryKind.Trace,subsystem,name,timestamp,0,0,detail);
    public static Boolean KernelProfile(String subsystem,String name,UInt64 samples,UInt64 durationNanoseconds)=>Emit(KernelTelemetryKind.Profile,subsystem,name,0,samples,durationNanoseconds,String.Empty);
    public static Boolean KernelProfile(String subsystem,String name,UInt64 timestamp,UInt64 samples,UInt64 duration)=>Emit(KernelTelemetryKind.Profile,subsystem,name,timestamp,samples,duration,String.Empty);
    public static Boolean KernelBootEvent(String name,UInt64 stage,KernelBootPhase phase,String detail="")=>Emit(KernelTelemetryKind.BootEvent,"boot",name,0,stage,(UInt64)phase,detail);
    public static Boolean KernelBootEvent(String name,UInt64 timestamp,UInt64 stage)=>Emit(KernelTelemetryKind.BootEvent,"boot",name,timestamp,stage,(UInt64)KernelBootPhase.End,String.Empty);
    public static Boolean KernelCounter(String subsystem,String name,UInt64 value)=>Emit(KernelTelemetryKind.Counter,subsystem,name,0,value,0,String.Empty);
    public static Boolean KernelCounter(String subsystem,String name,UInt64 timestamp,UInt64 value)=>Emit(KernelTelemetryKind.Counter,subsystem,name,timestamp,value,0,String.Empty);
    public static Boolean KernelDiagnosticEvent(String subsystem,String name,UInt64 code,String detail)=>Emit(KernelTelemetryKind.DiagnosticEvent,subsystem,name,0,code,0,detail);
    public static Boolean KernelDiagnosticEvent(String subsystem,String name,UInt64 timestamp,UInt64 code,String detail)=>Emit(KernelTelemetryKind.DiagnosticEvent,subsystem,name,timestamp,code,0,detail);
}

public static class KernelPanic
{
    private static IKernelPanicBackend _backend; private static Boolean _panicking;
    public static Boolean Configure(IKernelPanicBackend backend){_backend=backend;return backend!=null;}
    public static Boolean Raise(KernelPanicInfo info){if(_panicking)return false;_panicking=true;KernelLog.Critical("panic","KernelPanic",info.Reason??String.Empty);KernelLog.Critical("panic","KernelPanic",info.Message??String.Empty);if(_backend==null)return false;Boolean ok=_backend.TryCaptureRegisters(info);ok=_backend.TryCaptureCallStack(info)&&ok;if(info.WriteCrashDump)ok=_backend.TryWriteCrashDump(info)&&ok;if(info.BreakDebugger)ok=_backend.TryBreakDebugger(info)&&ok;return _backend.TryHaltOrReboot(info)&&ok;}
}
