const fs=require('fs');
const build=fs.readFileSync('Build-NovaOrynIDE.bat','utf8');
const run=fs.readFileSync('Run-NovaOrynIDE.bat','utf8');
let fail=false;
for (const [name,text,need] of [
 ['build',build,'applications\\electron\\lib\\.novaoryn-build-version'],
 ['build',build,'echo 0.11.16'],
 ['run',run,'applications\\electron\\lib\\.novaoryn-build-version'],
 ['run',run,'NOVAORYN_BUILT_VERSION'],
 ['run',run,'=="0.11.16"']
]) { if(!text.includes(need)){console.error(`[FAIL] ${name}: missing ${need}`);fail=true;} }
if(run.includes('ConvertFrom-Json') && run.includes('BuildState')){console.error('[FAIL] Run still depends on JSON build-state marker.');fail=true;}
if(fail) process.exitCode=1; else console.log('[ OK ] NovaOryn IDE 0.11.16 generated-build marker contract verified.');

// 0.11.16: user-facing comprehensive Kernel.cs files must not call the private panic transport helper.
const service=fs.readFileSync('packages/novaoryn-ide/src/node/novaoryn-project-service.ts','utf8');
if(service.includes('KernelPanicTransport.Initialize()')) { console.error('[FAIL] comprehensive kernel generator still exposes KernelPanicTransport.Initialize().'); fail=true; }
else console.log('[ OK ] Comprehensive kernels avoid private KernelPanicTransport calls.');
if(!service.includes('KernelStructuredLogging.Initialize()')) { console.error('[FAIL] explicit structured logging initialization is missing.'); fail=true; }
else console.log('[ OK ] Explicit structured logging initialization remains.');
if(fail) process.exitCode=1;
