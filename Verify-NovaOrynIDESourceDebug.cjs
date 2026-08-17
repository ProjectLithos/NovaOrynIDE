const fs = require('fs');
const path = require('path');

const root = __dirname;
const protocol = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/common/novaoryn-protocol.ts'), 'utf8');
const service = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/node/novaoryn-project-service.ts'), 'utf8');
const toolbar = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-toolbar-widget.tsx'), 'utf8');

function requireText(text, needle, message) {
  if (!text.includes(needle)) throw new Error(message);
}

requireText(protocol, 'breakpoints?: NovaOrynBreakpointResult[];', 'Debug state must report runtime breakpoint verification.');
requireText(protocol, 'breakpoints?: Array<{ sourcePath: string; line: number }>', 'Run request must carry pre-launch source breakpoints.');
requireText(service, 'NovaOryn.DebugSymbols.json', 'Exact NativeAOT source map contract is missing.');
requireText(service, 'session.gdb.command(`Z0,${address},1`)', 'Exact runtime source breakpoints must be sent to QEMU GDB.');
requireText(service, "const reply = await gdb.command('p10');", 'Runtime RIP reading is required for EFI relocation and source stops.');
requireText(service, "Buffer.from('NODBG64!', 'ascii')", 'QEMU debugcon relocation rendezvous is missing.');
requireText(service, "'-debugcon', `file:${debugConLog}`", 'QEMU debugcon transport is not enabled for Debug launch.');
requireText(service, 'runtimeAnchor - linkedAnchor', 'EFI relocation delta calculation is missing.');
requireText(service, 'await this.writeRip(gdb, runtimeResume);', 'Debug rendezvous must resume at the generated post-rendezvous symbol.');
requireText(toolbar, 'requestedBreakpoints', 'Toolbar must submit requested source breakpoints before Debug launch.');

console.log('[ OK ] NovaOryn exact NativeAOT source-debug contract verified.');
