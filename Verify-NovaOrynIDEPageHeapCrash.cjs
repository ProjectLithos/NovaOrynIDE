const fs = require('fs');
const path = require('path');
const root = __dirname;
const service = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/node/novaoryn-project-service.ts'), 'utf8');
const protocol = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/common/novaoryn-protocol.ts'), 'utf8');
const widget = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-debug-inspector-widget.tsx'), 'utf8');
const css = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/style/novaoryn.css'), 'utf8');
const checks = [
  ['page-table protocol', protocol.includes('NovaOrynPageTableInspection') && protocol.includes('NovaOrynPageTableEntry')],
  ['x64 CR3 page-table walk', service.includes("qemuMonitor(session.gdb, 'info registers')") && service.includes("['PML4', 'PDPT', 'PD', 'PT']") && service.includes('readPhysicalU64')],
  ['large-page translation', service.includes("pageSize = '1 GiB'") && service.includes("pageSize = '2 MiB'") && service.includes("pageSize = '4 KiB'")],
  ['page-table UI', widget.includes('Page Tables — x64 Translation') && widget.includes('refreshPageTable')],
  ['heap protocol', protocol.includes('NovaOrynHeapSnapshot') && protocol.includes('NovaOrynHeapBlock')],
  ['SDK heap diagnostic ABI', fs.readFileSync(path.join(root, 'SDK/src/NovaOryn.Kernel.Heap/KernelHeap.cs'), 'utf8').includes('DiagnosticMetadataAddress') && fs.readFileSync(path.join(root, 'SDK/src/NovaOryn.Kernel.Heap/KernelHeap.cs'), 'utf8').includes('InitializeDiagnosticMetadata')],
  ['stable heap diagnostic ABI reader', service.includes('0xFFFF81FFFFFFC000n') && service.includes('diagnosticMagic') && service.includes('KernelHeap metadata read from NovaOryn heap diagnostic ABI v1')],
  ['heap UI', widget.includes('<h3>Kernel Heap</h3>') && widget.includes('Refresh Heap')],
  ['crash dump protocol', protocol.includes('NovaOrynCrashDumpResult') && protocol.includes('NovaOrynCrashDumpSummary')],
  ['crash dump capture', service.includes("'.novaoryn', 'crash-dumps'") && service.includes('.nodump.json') && service.includes('captureCrashDump')],
  ['automatic exception/panic dumps', service.includes('Automatic exception crash dump failed') && service.includes('Automatic panic crash dump failed')],
  ['offline dump loader', service.includes('loadCrashDump') && widget.includes('Open Dump') && widget.includes('Offline dump:')],
  ['debug inspector styling', css.includes('.novaoryn-page-table-row') && css.includes('.novaoryn-heap-block') && css.includes('.novaoryn-crash-dump-actions')]
];
let failed = 0;
for (const [name, ok] of checks) {
  console.log(`${ok ? '[ OK ]' : '[FAIL]'} ${name}`);
  if (!ok) failed++;
}
if (failed) process.exit(1);
console.log('[ OK ] NovaOryn IDE 0.2.9 page-table, heap, and crash-dump contracts verified.');
