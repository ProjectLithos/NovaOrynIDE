const fs = require('fs');
const path = require('path');
const root = __dirname;
const protocol = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/common/novaoryn-protocol.ts'), 'utf8');
const service = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/node/novaoryn-project-service.ts'), 'utf8');
const inspector = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-debug-inspector-widget.tsx'), 'utf8');
const css = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/style/novaoryn.css'), 'utf8');

function requireText(text, needle, message) {
  if (!text.includes(needle)) throw new Error(message);
}

requireText(protocol, 'export interface NovaOrynMemoryReadResult', 'Memory read result contract is missing.');
requireText(protocol, 'readMemoryRange(sessionId: string, addressExpression: string, length: number)', 'Debugger protocol does not expose guest-memory reads.');
requireText(service, 'async readMemoryRange(', 'Backend memory viewer read implementation is missing.');
requireText(service, "Math.min(1024", 'Memory reads must be bounded.');
requireText(inspector, '<h3>Memory</h3>', 'NovaOryn Debug Memory section is missing.');
requireText(inspector, "this.refreshWatches().then(() => this.refreshMemory())", 'Watch and Memory refreshes must be serialized over GDB RSP.');
requireText(css, '.novaoryn-memory-view', 'Memory hex/ASCII view styling is missing.');

requireText(service, 'ensureNativeVariableMap', 'NativeAOT named-variable map loader is missing.');
requireText(service, "llvm-pdbutil.exe", 'NativeAOT locals must be read from the bundled LLVM PDB tooling.');
requireText(service, "S_DEFRANGE_REGISTER", 'Register live-range support is missing.');
requireText(service, "S_DEFRANGE_REGISTER_REL", 'Register-relative live-range support is missing.');
requireText(service, "S_DEFRANGE_FRAMEPOINTER_REL", 'Frame-pointer-relative live-range support is missing.');
requireText(service, 'readNamedNativeVariables', 'Paused debugger does not resolve named locals/arguments.');
requireText(service, 'resolveNamedVariableValue', 'Watch/condition evaluator cannot resolve named NativeAOT variables.');
requireText(inspector, "variable.location", 'Locals UI does not show native variable locations.');

console.log('[ OK ] NovaOryn IDE 0.1.53 Memory viewer and named NativeAOT locals/arguments contract verified.');
