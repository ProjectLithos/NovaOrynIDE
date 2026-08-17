const fs = require('fs');
const path = require('path');

const root = __dirname;
const protocol = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/common/novaoryn-protocol.ts'), 'utf8');
const service = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/node/novaoryn-project-service.ts'), 'utf8');
const toolbar = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-toolbar-widget.tsx'), 'utf8');
const inspector = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-debug-inspector-widget.tsx'), 'utf8');
const css = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/style/novaoryn.css'), 'utf8');

function requireText(text, needle, message) {
  if (!text.includes(needle)) throw new Error(message);
}

requireText(protocol, 'breakpoints?: NovaOrynBreakpointResult[];', 'Debug state must report runtime breakpoint verification.');
requireText(protocol, 'breakpoints?: Array<{ sourcePath: string; line: number }>', 'Run request must carry pre-launch source breakpoints.');
requireText(protocol, 'callStack?: NovaOrynDebugFrame[];', 'Debug state must expose a call stack.');
requireText(protocol, 'registers?: NovaOrynDebugRegister[];', 'Debug state must expose registers.');
requireText(protocol, 'locals?: NovaOrynDebugVariable[];', 'Debug state must expose local/native-frame values.');
requireText(service, 'NovaOryn.DebugSymbols.json', 'Exact NativeAOT source map contract is missing.');
requireText(service, 'session.gdb.command(`Z0,${address},1`)', 'Exact runtime source breakpoints must be sent to QEMU GDB.');
requireText(service, 'return this.readRegister(gdb, 16);', 'Runtime RIP reading is required for EFI relocation and source stops.');
requireText(service, "Buffer.from('NODBG64!', 'ascii')", 'QEMU debugcon relocation rendezvous is missing.');
requireText(service, "'-debugcon', `file:${debugConLog}`", 'QEMU debugcon transport is not enabled for Debug launch.');
requireText(service, 'runtimeAnchor - linkedAnchor', 'EFI relocation delta calculation is missing.');
requireText(service, 'await this.writeRip(gdb, runtimeResume);', 'Debug rendezvous must resume at the generated post-rendezvous symbol.');
requireText(service, "kind: 'step-into' | 'step-over' | 'step-out'", 'Source stepping plan is missing.');
requireText(service, 'currentCallInstructionLength', 'Step Over must recognize native CALL instructions.');
requireText(service, 'Step Out running to the caller', 'Step Out must run to the current native frame return address.');
requireText(service, 'readCallStack', 'Paused debug state must collect a native call stack.');
requireText(service, 'readRegisterSet', 'Paused debug state must collect x64 registers.');
requireText(service, 'readFrameSlots', 'Paused debug state must expose native frame/local slots.');
requireText(toolbar, 'requestedBreakpoints', 'Toolbar must submit requested source breakpoints before Debug launch.');
requireText(toolbar, 'novaoryn-current-statement-line', 'Toolbar must decorate the current paused source line.');
requireText(toolbar, 'showDebugInspector', 'Toolbar must expose the debug inspector.');
requireText(inspector, 'Call Stack', 'Debug inspector must show call stack.');
requireText(inspector, 'Locals / Native Frame', 'Debug inspector must show locals/native frame.');
requireText(inspector, 'Registers', 'Debug inspector must show registers.');
requireText(css, '.novaoryn-current-statement-glyph', 'Paused-line arrow styling is missing.');

console.log('[ OK ] NovaOryn source stepping, stop indicator and native debug-inspection contract verified.');
