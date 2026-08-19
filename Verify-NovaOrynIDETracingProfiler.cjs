const fs=require('fs'); const path=require('path'); const root=__dirname;
const read=p=>fs.readFileSync(path.join(root,p),'utf8');
const checks=[
 ['packages/novaoryn-ide/src/common/novaoryn-protocol.ts',['NovaOrynTraceSnapshot','NovaOrynProfilerSnapshot','readTraceSnapshot','readProfilerSnapshot','saveTrace']],
 ['packages/novaoryn-ide/src/node/novaoryn-project-service.ts',['ingestTelemetry','ingestStructuredTelemetry','ingestBootMilestone','traceSnapshotForSession','profilerSnapshotForSession','[NOVAORYN:PROFILE]']],
 ['packages/novaoryn-ide/src/browser/novaoryn-trace-widget.tsx',['Tracing + Boot Analyser','Boot timeline','Kernel event timeline','Save Trace']],
 ['packages/novaoryn-ide/src/browser/novaoryn-profiler-widget.tsx',['Performance Profiler','CPU utilisation','Hot functions / boot stages','Kernel counters and latency']],
 ['packages/novaoryn-ide/src/browser/novaoryn-contribution.ts',['NovaOrynCommands.TRACE','NovaOrynCommands.PROFILER','Tracing / Boot Analyser','Performance Profiler']],
 ['packages/novaoryn-ide/src/browser/novaoryn-frontend-module.ts',['NovaOrynTraceWidget','NovaOrynProfilerWidget']],
 ['packages/novaoryn-ide/src/browser/style/novaoryn.css',['novaoryn-boot-timeline','novaoryn-trace-event','novaoryn-profile-function']]
];
const missing=[]; for(const [file,tokens] of checks){const s=read(file);for(const token of tokens)if(!s.includes(token))missing.push(`${file}: ${token}`)}
if(missing.length){console.error('[FAIL] NovaOryn IDE tracing/profiler contract missing:\n'+missing.join('\n'));process.exit(1)}
console.log('[ OK ] NovaOryn IDE 0.8.3 Tracing/Boot Analyser and Performance Profiler contracts verified.');
