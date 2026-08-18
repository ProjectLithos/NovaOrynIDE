const fs=require('fs'); const path=require('path');
const root=__dirname;
let checks=0;
function requireText(file, needles){ const text=fs.readFileSync(path.join(root,file),'utf8'); for(const n of needles){ if(!text.includes(n)) throw new Error(`${file} missing ${n}`); checks++; } return text; }
requireText('SDK/src/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs',[
 'KernelTelemetryPhase','IKernelTelemetryContextProvider','KernelTelemetryStatistics','UInt32 Cpu','UInt64 ThreadId','UInt64 ProcessId'
]);
requireText('SDK/src/NovaOryn.Kernel.SubsystemContracts/KernelDiagnosticsRuntime.cs',[
 'MaximumSinks=4','AddSink(IKernelTelemetrySink sink)','KernelTraceBegin','KernelTraceEnd','KernelBootBegin','KernelBootEnd','KernelDiagnosticEvent','GetStatistics()'
]);
requireText('SDK/src/NovaOryn.Kernel.Bootstrap/KernelStructuredLogging.cs',[
 'KernelConsoleTelemetrySink','[NOVAORYN:TRACE]','[NOVAORYN:BOOT]','[NOVAORYN:PROFILE]','IKernelTelemetryContextProvider'
]);
requireText('packages/novaoryn-ide/src/common/novaoryn-protocol.ts',['threadId?: number','processId?: number','sequence?: number','diagnosticCode?: number']);
requireText('packages/novaoryn-ide/src/node/novaoryn-project-service.ts',["this.numberField(values, 'thread')","this.numberField(values, 'process')","this.numberField(values, 'seq')","this.numberField(values, 'code')"]);
requireText('SDK/src/NovaOryn.Kernel.Bootstrap/Kernel.cs',[
 'KernelTelemetry.Configure','KernelBootBegin("SMP / per-CPU")','KernelBootBegin("Scheduler")','KernelBootBegin("Protection")','KernelBootBegin("System calls")','KernelBootBegin("Processes")','KernelBootBegin("Driver framework")','KernelCounter("driver", "registered-drivers"'
]);
const manifest=JSON.parse(fs.readFileSync(path.join(root,'SDK/NovaOryn.SdkManifest.json'),'utf8'));
if(manifest.sdkVersion!=='0.41.0') throw new Error('SDK version must be 0.41.0'); checks++;
if(manifest.apiVersion!=='1.2') throw new Error('API version must be 1.2'); checks++;
if(manifest.contracts.kernelTelemetry!=='1.1') throw new Error('telemetry contract must be 1.1'); checks++;
const pkg=JSON.parse(fs.readFileSync(path.join(root,'package.json'),'utf8')); if(pkg.version!=='0.4.6') throw new Error('IDE version must be 0.4.6'); checks++;
console.log(`[ OK ] NovaOryn structured kernel telemetry runtime verified (${checks} checks).`);
