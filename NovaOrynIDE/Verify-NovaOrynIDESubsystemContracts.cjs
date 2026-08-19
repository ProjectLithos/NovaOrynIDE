const fs=require('fs'),path=require('path');
const root=__dirname, sdk=path.join(root,'SDK');
function read(p){return fs.readFileSync(path.join(root,p),'utf8')}
const map=JSON.parse(read('SDK/NovaOryn.SubsystemContracts.json'));
const source=read('SDK/src/NovaOryn.Kernel.SubsystemContracts/SubsystemContracts.cs');
const manifest=JSON.parse(read('SDK/NovaOryn.SdkManifest.json'));
const expected=[['Memory','IKernelMemoryContract'],['Interrupts','IKernelInterruptContract'],['Scheduler','IKernelSchedulerContract'],['Processes','IKernelProcessContract'],['Syscalls','IKernelSyscallContract'],['Drivers','IKernelDriverContract'],['Filesystem','IKernelFilesystemContract'],['Networking','IKernelNetworkingContract'],['Graphics','IKernelGraphicsContract'],['Input','IKernelInputContract'],['Time','IKernelTimeContract'],['Power','IKernelPowerContract'],['Smp','IKernelSmpContract']];
let bad=false; function ok(n,v){console.log(`${v?'[ OK ]':'[FAIL]'} ${n}`);bad ||= !v;}
ok('contract version 1.0',map.contractVersion==='1.0'&&manifest.contracts.subsystemContracts==='1.0');
ok('all 13 subsystem mappings',map.subsystems.length===13);
for(const [id,intf] of expected){const e=map.subsystems.find(x=>x.id===id);ok(`${id} formal contract`,!!e&&e.interface===intf&&source.includes(`interface ${intf}`));}
ok('common status/version boundary',source.includes('KernelSubsystemStatus')&&source.includes('IsCompatible(UInt16 requiredMajor, UInt16 requiredMinor)'));
ok('documentation',fs.existsSync(path.join(sdk,'docs','Kernel-Subsystem-Contracts.md')));
ok('offline API-browser guide',fs.existsSync(path.join(sdk,'docs','site','guides','Kernel-Subsystem-Contracts.html')));
if(bad)process.exit(1); console.log('[ OK ] NovaOryn IDE 0.4.2 formal kernel subsystem contract verified.');
