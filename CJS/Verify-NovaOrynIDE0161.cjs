const fs=require('fs'),path=require('path');
const root=path.resolve(__dirname,'..'); let fail=0;
const read=p=>fs.readFileSync(path.join(root,p),'utf8');
const exists=p=>fs.existsSync(path.join(root,p));
function ok(name,condition){console.log(`${condition?'[ OK ]':'[FAIL]'} ${name}`);if(!condition)fail++;}
const version=read('VERSION').split(/\r?\n/)[0].trim(); ok('release is 0.16.1',version==='0.16.1');
const dir='SDK/src/NovaOryn.Kernel.Synchronization';
for(const f of ['NovaOryn.Kernel.Synchronization.csproj','KernelSynchronizationContracts.cs','KernelAtomic.cs','KernelSpinLock.cs','KernelMutex.cs','KernelSemaphore.cs','KernelEvent.cs','KernelReaderWriterLock.cs','KernelBarrier.cs','KernelLockFree.cs']) ok(`synchronization source ${f}`,exists(`${dir}/${f}`));
const atomic=read(`${dir}/KernelAtomic.cs`), spin=read(`${dir}/KernelSpinLock.cs`), mutex=read(`${dir}/KernelMutex.cs`), sem=read(`${dir}/KernelSemaphore.cs`), ev=read(`${dir}/KernelEvent.cs`), rw=read(`${dir}/KernelReaderWriterLock.cs`), barrier=read(`${dir}/KernelBarrier.cs`), lf=read(`${dir}/KernelLockFree.cs`), contracts=read(`${dir}/KernelSynchronizationContracts.cs`);
ok('atomic compare-exchange',atomic.includes('TryCompareExchange')); ok('atomic exchange',atomic.includes('TryExchange')); ok('atomic fetch-add',atomic.includes('TryFetchAdd')); ok('atomic increment/decrement',atomic.includes('TryIncrement')&&atomic.includes('TryDecrement')); ok('atomic memory barrier',atomic.includes('MemoryBarrier')); ok('atomic ordering contract',contracts.includes('KernelMemoryOrder'));
ok('professional spinlock',spin.includes('struct KernelSpinLock')&&spin.includes('TryEnter')&&spin.includes('Exit'));
ok('professional mutex ownership',mutex.includes('struct KernelMutex')&&mutex.includes('_owner')&&mutex.includes('GetOwnerToken')&&mutex.includes('Unlock'));
ok('bounded semaphore',sem.includes('struct KernelSemaphore')&&sem.includes('_maximum')&&sem.includes('TryWait')&&sem.includes('Release'));
ok('manual/auto reset event',ev.includes('struct KernelEvent')&&ev.includes('_manualReset')&&ev.includes('Set()')&&ev.includes('Reset()'));
ok('writer-preferring reader/writer lock',rw.includes('struct KernelReaderWriterLock')&&rw.includes('_waitingWriters')&&rw.includes('TryEnterRead')&&rw.includes('EnterWrite')&&rw.includes('ExitWrite'));
ok('generation-counted reusable barrier',barrier.includes('struct KernelBarrier')&&barrier.includes('_generation')&&barrier.includes('SignalAndWait'));
ok('tagged lock-free stack',lf.includes('struct KernelLockFreeStack64')&&lf.includes('high 32 bits tag')&&lf.includes('TryPush')&&lf.includes('TryPop'));
const asm=read('SDK/native/x64/Cpu.asm'); for(const sym of ['NovaOrynX64AtomicCompareExchange64','NovaOrynX64AtomicExchange64','NovaOrynX64AtomicFetchAdd64','NovaOrynX64AtomicLoad64','NovaOrynX64AtomicStore64','NovaOrynX64MemoryBarrier'])ok(`x64 native ${sym}`,asm.includes(`global ${sym}`)); ok('x64 locked cmpxchg',asm.includes('lock cmpxchg')); ok('x64 locked xadd',asm.includes('lock xadd')); ok('x64 full fence',asm.includes('mfence'));
const native=read('SDK/src/NovaOryn.Kernel.X64.LowLevel/Native.cs'), boundary=read('SDK/src/NovaOryn.Arch.X64/X64ArchitectureBoundary.cs'); ok('private native atomic ABI',native.includes('AtomicCompareExchange64')&&native.includes('AtomicFetchAdd64')&&native.includes('MemoryBarrier')); ok('canonical x64 boundary exposes atomics',boundary.includes('AtomicCompareExchange64')&&boundary.includes('AtomicExchange64')&&boundary.includes('SpinWaitHint'));
const pro=read('SDK/src/NovaOryn.Kernel.SubsystemContracts/ProfessionalSdkContracts.cs'); ok('formal synchronization contract covers all eight kinds',['SpinLock=1','Mutex=2','Semaphore=3','Event=4','ReaderWriterLock=5','Atomic=6','Barrier=7','LockFree=8'].every(x=>pro.includes(x))); ok('formal synchronization contract expanded atomics',pro.includes('TryAtomicExchange')&&pro.includes('TryAtomicFetchAdd')&&pro.includes('TrySpinWaitHint'));
const sln=read('SDK/NovaOryn.sln'); ok('synchronization project participates in SDK solution',sln.includes('NovaOryn.Kernel.Synchronization'));
for(const base of ['SDK/templates/NovaOrynKernel/Sdk','SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/Sdk']){
 for(const f of ['KernelSynchronizationContracts.cs','KernelAtomic.cs','KernelSpinLock.cs','KernelMutex.cs','KernelSemaphore.cs','KernelEvent.cs','KernelReaderWriterLock.cs','KernelBarrier.cs','KernelLockFree.cs','NovaOryn.Kernel.Synchronization.csproj']) ok(`${base} ${f} synchronized`,read(`${base}/NovaOryn.Kernel.Synchronization/${f}`)===read(`${dir}/${f}`));
 ok(`${base} low-level native declarations synchronized`,read(`${base}/NovaOryn.Kernel.X64.LowLevel/Native.cs`)===native);
 ok(`${base} x64 architecture boundary synchronized`,read(`${base}/NovaOryn.Arch.X64/X64ArchitectureBoundary.cs`)===boundary);
}
for(const project of ['SDK/templates/NovaOrynKernel/NovaOrynKernel.csproj','SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/NovaOrynKernel.csproj']) ok(`${project} references synchronization`,read(project).includes('NovaOryn.Kernel.Synchronization\\NovaOryn.Kernel.Synchronization.csproj'));
const vst=read('SDK/src/NovaOryn.VisualStudio/ProjectTemplates/CSharp/1033/NovaOrynKernel/NovaOrynKernel.vstemplate'); ok('Visual Studio template packages synchronization files',vst.includes('Sdk\\NovaOryn.Kernel.Synchronization\\KernelAtomic.cs')&&vst.includes('Sdk\\NovaOryn.Kernel.Synchronization\\KernelLockFree.cs'));
ok('synchronization documentation exists',exists('SDK/docs/Synchronization-Primitives.md')&&read('SDK/docs/Synchronization-Primitives.md').includes('KernelReaderWriterLock'));
// Preserve the 0.15.1 automatic-console regression rule without brittle method extraction.
const toolbar=read('packages/novaoryn-ide/src/browser/novaoryn-toolbar-widget.tsx');
const toolbarJs=read('packages/novaoryn-ide/lib/browser/novaoryn-toolbar-widget.js');
ok('Run/Debug source does not auto-attach Kernel Console',!toolbar.includes('addWidget(this.kernelConsole'));
ok('Run/Debug source does not auto-activate Kernel Console',!toolbar.includes('activateWidget(this.kernelConsole.id)'));
ok('Run/Debug runtime does not auto-attach Kernel Console',!toolbarJs.includes('addWidget(this.kernelConsole'));
ok('Run/Debug runtime does not auto-activate Kernel Console',!toolbarJs.includes('activateWidget(this.kernelConsole.id)'));
ok('Run/Debug still uses bottom Output channel',toolbar.includes('channel.show({ preserveFocus: false })')&&toolbarJs.includes('channel.show({ preserveFocus: false })'));
ok('Kernel Console remains explicit Engineering tool',read('packages/novaoryn-ide/src/browser/novaoryn-contribution.ts').includes('NovaOrynCommands.CONSOLE'));
process.exitCode=fail?1:0;
