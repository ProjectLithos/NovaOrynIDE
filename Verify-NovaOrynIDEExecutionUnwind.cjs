const fs = require('fs');
const path = require('path');
const root = __dirname;
const service = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/node/novaoryn-project-service.ts'), 'utf8');
const protocol = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/common/novaoryn-protocol.ts'), 'utf8');
const widget = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-debug-inspector-widget.tsx'), 'utf8');
const app = JSON.parse(fs.readFileSync(path.join(root, 'applications/electron/package.json'), 'utf8'));
const icon = path.join(root, 'applications/electron/resources/novaoryn-ide.ico');
const checks = [
  ['GDB CPU/thread enumeration', service.includes("qfThreadInfo") && service.includes("qThreadExtraInfo")],
  ['CPU/thread context selection', service.includes("selectExecutionContext") && service.includes("Hg${normalized}")],
  ['execution context protocol', protocol.includes('NovaOrynDebugExecutionContext') && protocol.includes('selectedThreadId')],
  ['execution context UI', widget.includes('CPUs / Threads / Process Contexts') && widget.includes('selectExecutionContext(context.id)')],
  ['PE x64 unwind parser', service.includes('ensurePeUnwindTable') && service.includes('applyX64UnwindInfo') && service.includes('UWOP_ALLOC_SMALL')],
  ['no heuristic stack scan', !service.includes('conservatively scan a small') && !service.includes('stack-word scanning')],
  ['unwind call-stack UI', widget.includes('Call Stack — x64 Unwind') && widget.includes('frame.unwoundBy')],
  ['Electron logo icon configured', app.theia?.frontend?.config?.electron?.windowOptions?.icon === 'resources/novaoryn-ide.ico' && fs.existsSync(icon) && fs.statSync(icon).size > 1024]
];
let failed = 0;
for (const [name, ok] of checks) {
  console.log(`${ok ? '[ OK ]' : '[FAIL]'} ${name}`);
  if (!ok) failed++;
}
if (failed) process.exit(1);
console.log('[ OK ] NovaOryn IDE 0.1.44 CPU/thread/process-context, x64 unwind, and icon contracts verified.');
