const fs=require('fs'); const path=require('path'); const root=__dirname;
const read=p=>fs.readFileSync(path.join(root,p),'utf8');
const checks=[
 ['packages/novaoryn-ide/src/browser/novaoryn-dashboard-widget.tsx',['NovaOryn OS engineering dashboard','listTests']],
 ['packages/novaoryn-ide/src/browser/novaoryn-kernel-console-widget.tsx',['Dedicated NovaOryn build, serial and kernel console','Filter console']],
 ['packages/novaoryn-ide/src/browser/novaoryn-hardware-widget.tsx',['Hardware / Device Tree','PCI','USB','ACPI','platform','virtual','logical']],
 ['packages/novaoryn-ide/src/browser/novaoryn-test-explorer-widget.tsx',['Test Explorer','runTest']],
 ['packages/novaoryn-ide/src/node/novaoryn-project-service.ts',['async listTests','async runTest','readTestOutput']],
 ['packages/novaoryn-ide/src/browser/novaoryn-toolbar-widget.tsx',['NovaOrynKernelConsoleWidget','this.kernelConsole.append']],
 ['packages/novaoryn-ide/src/browser/novaoryn-contribution.ts',['NovaOrynCommands.DASHBOARD','NovaOrynCommands.CONSOLE','NovaOrynCommands.HARDWARE','NovaOrynCommands.TESTS']]
];
const missing=[]; for(const [file,tokens] of checks){const s=read(file);for(const token of tokens)if(!s.includes(token))missing.push(`${file}: ${token}`)}
if(missing.length){console.error('[FAIL] NovaOryn IDE professional tools contract missing:\n'+missing.join('\n'));process.exit(1)}
console.log('[ OK ] NovaOryn IDE 0.8.3 Dashboard, Kernel Console, unified Hardware Tree and Test Explorer contracts verified.');
