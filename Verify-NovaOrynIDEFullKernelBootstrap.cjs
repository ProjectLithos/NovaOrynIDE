const fs=require('fs');
function read(p){return fs.readFileSync(p,'utf8');}
const service=read('packages/novaoryn-ide/src/node/novaoryn-project-service.ts');
const creator=read('SDK/src/NovaOryn.ProjectCreator/Program.cs');
const kernel=read('SDK/templates/NovaOrynKernel/Kernel/Kernel.cs');
const boot=read('SDK/templates/NovaOrynKernel/Boot/BootStartup.cs');
const hal=read('SDK/templates/NovaOrynKernel/HAL/HardwareAbstractionLayer.cs');
const checks=[
 ['new IDE kernels enter BootStartup',service.includes('BootStartup.Initialize(boot)')],
 ['new IDE kernels enter HAL',service.includes('HardwareAbstractionLayer.Initialize()')],
 ['new IDE kernels initialize command line',service.includes('KernelCommandLine.Initialize()')],
 ['new IDE kernels enable interrupt dispatch',service.includes('KernelInterruptDispatch.Enable()')],
 ['new IDE kernels run interactively',service.includes('KernelConsole.RunInteractive()')],
 ['minimal IDE kernel migration is called',creator.includes('MigrateIdeGeneratedMinimalKernel(output, template)')],
 ['minimal IDE kernel is narrowly recognized',creator.includes('IsIdeGeneratedMinimalKernel')&&creator.includes('source.Length > 2200')&&creator.includes('private const UInt32 ConsoleFontSize = 32U;')],
 ['migration installs canonical full kernel',creator.includes('Migrated IDE-generated minimal Kernel.cs to the full NovaOryn runtime bootstrap')],
 ['template kernel enters full boot',kernel.includes('BootStartup.Initialize(boot)')&&kernel.includes('HardwareAbstractionLayer.Initialize()')],
 ['memory runtime starts',boot.includes('KernelPhysicalMemory.Initialize(boot)')&&boot.includes('KernelVirtualMemory.Initialize()')&&boot.includes('KernelHeap.Initialize()')],
 ['SMP and scheduler start',boot.includes('KernelSmp.Initialize(boot)')&&boot.includes('KernelScheduler.Initialize()')],
 ['protection and syscalls start',boot.includes('KernelProtection.Initialize()')&&boot.includes('KernelSystemCalls.Initialize()')&&boot.includes('System calls online.')],
 ['process runtime starts',hal.includes('KernelProcesses.Initialize()')],
 ['driver runtime starts when configured',hal.includes('KernelDrivers.Initialize()')&&hal.includes('KernelPci.Initialize()')&&hal.includes('KernelInterruptBroker.Initialize()')],
 ['storage runtime starts when configured',hal.includes('KernelStorage.Initialize()')&&hal.includes('KernelNvme.Initialize()')&&hal.includes('KernelAhci.Initialize()')],
 ['network runtime starts when configured',hal.includes('KernelNetworking.Initialize()')&&hal.includes('KernelVirtio.Initialize()')&&hal.includes('KernelE1000.Initialize()')&&hal.includes('KernelRtl8168.Initialize()')],
 ['USB runtime starts when configured',hal.includes('KernelXhci.Initialize()')&&hal.includes('UsbHid.Initialize()')&&hal.includes('UsbMassStorage.Initialize()')]
];
let bad=0;for(const [name,ok] of checks){console.log(`${ok?'[ OK ]':'[FAIL]'} ${name}`);if(!ok)bad++;}
if(bad)process.exit(1);
console.log(`[ OK ] NovaOryn IDE 0.4.15 full generated-kernel bootstrap contract verified (${checks.length} checks).`);
