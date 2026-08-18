const fs=require('fs');
const path=require('path');
const root=__dirname;
function read(p){return fs.readFileSync(path.join(root,p),'utf8');}
function requireText(text,needle,label){if(!text.includes(needle)){console.error(`[FAIL] ${label}`);process.exit(1);} console.log(`[ OK ] ${label}`);}
const contracts=read('SDK/src/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs');
const runtime=read('SDK/src/NovaOryn.Kernel.SubsystemContracts/KernelDiagnosticsRuntime.cs');
const boot=read('SDK/src/NovaOryn.Kernel.Bootstrap/Kernel.cs');
const sink=read('SDK/src/NovaOryn.Kernel.Bootstrap/KernelStructuredLogging.cs');
const scheduler=read('SDK/src/NovaOryn.Kernel.Scheduler/KernelScheduler.cs');
const manifest=read('SDK/NovaOryn.SdkManifest.json');
[
 [contracts,'KernelLogLevel : Byte { Trace=0, Debug=1, Info=2, Warning=3, Error=4, Critical=5 }','six structured log levels'],
 [contracts,'KernelLogStatistics','log statistics contract'],
 [runtime,'MaximumSinks=4','multi-sink logging'],
 [runtime,'SetMinimumLevel','runtime level filter'],
 [runtime,'GetStatistics','runtime statistics'],
 [sink,'[thread=','thread field emitted'],
 [sink,'[process=','process field emitted'],
 [sink,'record.TimestampNanoseconds','timestamp emitted'],
 [sink,'record.Subsystem','subsystem emitted'],
 [sink,'record.Source','source emitted'],
 [scheduler,'TryGetCurrentThreadId','live scheduler thread context'],
 [boot,'KernelLog.Configure(new KernelConsoleLogSink(), new KernelLogContextProvider(), KernelLogLevel.Info)','boot logging configured'],
 [boot,'KernelLog.Info("kernel","Kernel.KMain","SMP and per-CPU state online.")','boot milestones use structured log'],
 [manifest,'"structuredLogging": "1.1"','SDK manifest logging contract 1.1']
].forEach(x=>requireText(x[0],x[1],x[2]));
console.log('[ OK ] NovaOryn structured kernel logging verified (14 checks).');
