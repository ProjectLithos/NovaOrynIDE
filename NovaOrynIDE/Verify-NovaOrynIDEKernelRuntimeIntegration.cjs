const fs=require('fs');
const read=p=>fs.readFileSync(p,'utf8');
const drivers=read('SDK/src/NovaOryn.Kernel.Drivers/KernelDrivers.cs');
const driverMath=read('SDK/src/NovaOryn.Kernel.Drivers/KernelDriverMath.cs');
const pci=read('SDK/src/NovaOryn.Kernel.Pci/KernelPci.cs');
const bootstrap=read('SDK/src/NovaOryn.Kernel.Bootstrap/Kernel.cs');
const runtime=read('SDK/src/NovaOryn.Kernel.Bootstrap/KernelSubsystemRuntime.cs');
const e1000=read('SDK/src/NovaOryn.Kernel.E1000/KernelE1000.cs');
const rtl=read('SDK/src/NovaOryn.Kernel.Rtl8168/KernelRtl8168.cs');
const virtio=read('SDK/src/NovaOryn.Kernel.Virtio/KernelVirtio.cs');
const gpu=read('SDK/src/NovaOryn.Kernel.Virtio.Gpu/KernelVirtioGpu.cs');
const checks=[
 ['kernel contract runtime bridge',runtime.includes('KernelSubsystemRuntime')&&runtime.includes('SubsystemCount=13U')&&runtime.includes('KernelSubsystemContractVersion.Major')],
 ['all formal subsystem IDs consumed', ['Memory','Interrupts','Scheduler','Processes','Syscalls','Drivers','Filesystem','Networking','Graphics','Input','Time','Power','Smp'].every(x=>runtime.includes(`KernelSubsystemId.${x}`))],
 ['boot gated by formal contracts',bootstrap.includes('KernelSubsystemRuntime.ValidateAll')&&bootstrap.includes('Kernel runtime is gated by the public subsystem boundaries')],
 ['binding issues kernel grants',drivers.includes('GrantDeclaredCapabilities(&context,d,r)')&&drivers.includes('AllDeclaredCapabilitiesGranted')],
 ['live grant lookup',drivers.includes('HasCapabilityGrant')&&drivers.includes('TryGetCapabilityGrant')],
 ['PCI DMA resource authority',pci.includes('KernelDeviceResourceType.Dma')&&driverMath.includes('resource.Type==KernelDeviceResourceType.Dma')],
 ['MSI-only interrupt authority',pci.includes('TryGetMsiCapability(location,out _)||TryGetMsixCapability(location,out _)')],
 ['E1000 declares capabilities',e1000.includes('KernelDriverCapabilityDeclaration declaration=')&&e1000.includes('KernelDriverCapability.Networking')&&e1000.includes('KernelDriverCapability.Dma')],
 ['RTL8168 declares capabilities',rtl.includes('KernelDriverCapabilityDeclaration declaration=')&&rtl.includes('KernelDriverCapability.Networking')],
 ['VirtIO declares storage/network authority',virtio.includes('KernelDriverCapability.Filesystem')&&virtio.includes('KernelDriverCapability.Networking')],
 ['VirtIO GPU declares MMIO/DMA authority',gpu.includes('KernelDriverCapability.Mmio')&&gpu.includes('KernelDriverCapability.Dma')]
];
let bad=0;for(const [name,ok] of checks){console.log(`${ok?'[ OK ]':'[FAIL]'} ${name}`);if(!ok)bad++;}
if(bad)process.exit(1);console.log(`[ OK ] NovaOryn IDE 0.4.2 kernel runtime integration verified (${checks.length} checks).`);
