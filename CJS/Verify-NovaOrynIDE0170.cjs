const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'..'); let fail=0;
const read=p=>fs.readFileSync(path.join(root,p),'utf8'); const exists=p=>fs.existsSync(path.join(root,p));
function ok(name,cond){console.log(`${cond?'[ OK ]':'[FAIL]'} ${name}`); if(!cond)fail++;}
const version=read('VERSION').split(/\r?\n/)[0].trim(); ok('release is 0.17.0',version==='0.17.0');
const heap=read('SDK/src/NovaOryn.Kernel.Heap/KernelHeap.cs'); const hd=read('SDK/src/NovaOryn.Kernel.Heap/KernelHeap.Diagnostics.cs');
const pm=read('SDK/src/NovaOryn.Kernel.Memory/KernelPhysicalMemory.cs'); const vm=read('SDK/src/NovaOryn.Kernel.VirtualMemory/KernelVirtualMemory.Diagnostics.cs');
const facade=read('SDK/src/NovaOryn.Kernel.Memory.Diagnostics/KernelMemoryDiagnostics.cs'); const pro=read('SDK/src/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs');
ok('physical allocator statistics',pm.includes('KernelPhysicalMemoryStatistics')&&pm.includes('ReservedPages')&&facade.includes('TryGetPhysicalAllocatorStatistics'));
ok('heap diagnostic snapshot',hd.includes('KernelHeapDiagnosticSnapshot')&&hd.includes('GetDiagnosticSnapshot')&&hd.includes('GuardFailures')&&hd.includes('DoubleFreeFailures'));
ok('read-only page-table inspection',vm.includes('KernelPageTableInspection')&&vm.includes('TryInspectPageTable')&&vm.includes('Pml4Entry')&&vm.includes('PdptEntry')&&vm.includes('PdEntry')&&vm.includes('PtEntry'));
ok('page-table large-page inspection',vm.includes('Page1GiB')&&vm.includes('Page2MiB')&&vm.includes('Page4KiB'));
ok('leak checkpoint',hd.includes('TryCreateLeakCheckpoint')&&hd.includes('GetLeakCandidateCount')&&hd.includes('TryGetLeakCandidate'));
ok('leak candidate metadata',hd.includes('KernelHeapAllocationInfo')&&hd.includes('AllocationSequence')&&hd.includes('TagHash'));
ok('guarded allocation API',hd.includes('TryAllocateGuarded')&&hd.includes('TryReleaseGuarded')&&hd.includes('TryValidateGuards'));
ok('independent leading/trailing canaries',hd.includes('LeadingCanary')&&hd.includes('TrailingCanary')&&hd.includes('LeadingGuardAddresses')&&hd.includes('TrailingGuardAddresses'));
ok('double-free status',heap.includes('DoubleFreeDetected')&&hd.includes('ReleasedTokens')&&hd.includes('_doubleFreeFailures'));
ok('allocation tagging',hd.includes('TrySetAllocationTag')&&hd.includes('TryGetAllocationTagHash')&&hd.includes('HashTag'));
ok('freestanding tag hashes avoid managed string retention',hd.includes('FNV-1a')&&hd.includes('TagHashes')&&!hd.includes('String[]'));
ok('single memory diagnostics facade',facade.includes('class KernelMemoryDiagnostics')&&facade.includes('KernelPhysicalMemory.GetStatistics')&&facade.includes('KernelHeap.GetDiagnosticSnapshot')&&facade.includes('KernelVirtualMemory.TryInspectPageTable'));
ok('formal memory diagnostics contract extended',pro.includes('IKernelMemoryDiagnosticsContract')&&pro.includes('TryCreateLeakCheckpoint')&&pro.includes('TryGetLeakCandidate')&&pro.includes('TryGetAllocationTagHash')&&pro.includes('TryGetFailureCounters'));
ok('memory diagnostics project exists',exists('SDK/src/NovaOryn.Kernel.Memory.Diagnostics/NovaOryn.Kernel.Memory.Diagnostics.csproj'));
ok('memory diagnostics project in SDK solution',read('SDK/NovaOryn.sln').includes('NovaOryn.Kernel.Memory.Diagnostics'));
for(const base of ['SDK/templates/NovaOrynKernel/Sdk','SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/Sdk']){
  for(const rel of ['NovaOryn.Kernel.Heap/KernelHeap.cs','NovaOryn.Kernel.Heap/KernelHeap.Diagnostics.cs','NovaOryn.Kernel.VirtualMemory/KernelVirtualMemory.Diagnostics.cs','NovaOryn.Kernel.Memory.Diagnostics/KernelMemoryDiagnostics.cs','NovaOryn.Kernel.Memory.Diagnostics/NovaOryn.Kernel.Memory.Diagnostics.csproj','NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs'])
    ok(`${base} ${rel} synchronized`,read(`${base}/${rel}`)===read(`SDK/src/${rel}`));
}
for(const project of ['SDK/templates/NovaOrynKernel/NovaOrynKernel.csproj','SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/NovaOrynKernel.csproj']) ok(`${project} references memory diagnostics`,read(project).includes('NovaOryn.Kernel.Memory.Diagnostics\\NovaOryn.Kernel.Memory.Diagnostics.csproj'));
const vst=read('SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/NovaOrynKernel.vstemplate');
ok('Visual Studio template packages memory diagnostics',vst.includes('KernelHeap.Diagnostics.cs')&&vst.includes('KernelVirtualMemory.Diagnostics.cs')&&vst.includes('NovaOryn.Kernel.Memory.Diagnostics\\KernelMemoryDiagnostics.cs'));
ok('memory diagnostics documentation',exists('SDK/docs/Memory-Diagnostics.md')&&exists('SDK/docs/site-content/Memory-Diagnostics.md')&&read('SDK/docs/Memory-Diagnostics.md').includes('Double-free detection'));
const toolbar=read('packages/novaoryn-ide/src/browser/novaoryn-toolbar-widget.tsx'), toolbarJs=read('packages/novaoryn-ide/lib/browser/novaoryn-toolbar-widget.js');
ok('Run/Debug does not auto-attach Kernel Console',!toolbar.includes('addWidget(this.kernelConsole')&&!toolbarJs.includes('addWidget(this.kernelConsole'));
ok('Run/Debug still uses bottom Output channel',toolbar.includes('channel.show({ preserveFocus: false })')&&toolbarJs.includes('channel.show({ preserveFocus: false })'));
process.exitCode=fail?1:0;
