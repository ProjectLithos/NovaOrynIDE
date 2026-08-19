const fs=require('fs'),path=require('path');
const root=__dirname;
function read(p){return fs.readFileSync(path.join(root,p),'utf8');}
const files=[
 'SDK/templates/NovaOrynKernel/HAL/HardwareAbstractionLayer.cs',
 'SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/HAL/HardwareAbstractionLayer.cs'
];
let checks=[];
for(const f of files){const s=read(f);checks.push([f+' PS/2 handler',s.includes('SetKeyboardEventHandler(&HandlePs2KeyboardEvent)')]);checks.push([f+' timer input drain',s.includes('KernelTimerDispatch.Register(1000000UL, &ServiceInput')]);checks.push([f+' command-line bridge',s.includes('KernelCommandLine.HandleCharacter(input.Character)')]);checks.push([f+' IRQ route',s.includes('RegisterLegacyGsi(1U')]);}
const ps2=read('SDK/src/NovaOryn.Kernel.Ps2/KernelPs2.cs');
checks.push(['PS/2 explicit initialized state',ps2.includes('private static Boolean _initialized,')&&ps2.includes('_initialized=true;')]);
const sched=read('SDK/src/NovaOryn.Kernel.Scheduler/KernelScheduler.cs');
checks.push(['scheduler nopreinit defaults',sched.includes('_nextThreadId = 1UL;')&&sched.includes('_quantum = DefaultQuantumNanoseconds;')]);
let bad=false;for(const [n,ok] of checks){console.log(`${ok?'[ OK ]':'[FAIL]'} ${n}`);if(!ok)bad=true;}if(bad)process.exit(1);console.log(`[ OK ] NovaOryn IDE 0.10.1 interactive console/input bridge verified (${checks.length} checks).`);
