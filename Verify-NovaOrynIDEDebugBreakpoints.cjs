const fs = require('fs');
const path = require('path');
const root = __dirname;
const toolbar = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-toolbar-widget.tsx'), 'utf8');
const editor = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-editor-environment.ts'), 'utf8');
const manager = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-breakpoint-manager.ts'), 'utf8');
const contribution = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-contribution.ts'), 'utf8');
const extensionPackage = JSON.parse(fs.readFileSync(path.join(root, 'packages/novaoryn-ide/package.json'), 'utf8'));
const electronPackage = JSON.parse(fs.readFileSync(path.join(root, 'applications/electron/package.json'), 'utf8'));
const service = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/node/novaoryn-project-service.ts'), 'utf8');
function requireText(text, needle, message) { if (!text.includes(needle)) throw new Error(message); }
function rejectText(text, needle, message) { if (text.includes(needle)) throw new Error(message); }
if (extensionPackage.dependencies['@theia/debug'] !== '1.74.0' || electronPackage.dependencies['@theia/debug'] !== '1.74.0') {
    throw new Error('Theia native debugger package must be shipped in both the extension and Electron application.');
}
requireText(toolbar, 'const canSetBreakpoint = debug && hasWorkspace;', 'Breakpoint toolbar control must be usable before a debug session.');
requireText(editor, "Theia's @theia/debug package is the authoritative breakpoint editor UI", 'Breakpoint editor UI must be delegated to Theia native debugger.');
requireText(editor, 'glyphMargin: true', 'Monaco glyph margin must remain enabled.');
requireText(editor, "document.addEventListener('contextmenu', this.documentContextMenuListener, true)", 'NovaOryn context-menu source location tracking is required.');
rejectText(editor, "document.addEventListener('mousedown'", 'NovaOryn must not compete with Theia native debugger for gutter mouse-down events.');
rejectText(editor, 'deltaDecorations(', 'NovaOryn must not draw a second custom breakpoint decoration layer.');
requireText(manager, "BreakpointManager as TheiaBreakpointManager", 'NovaOryn breakpoint backend must bridge Theia BreakpointManager.');
requireText(manager, 'SourceBreakpoint.create(uri, { line })', 'NovaOryn toolbar/context breakpoint command must create native Theia source breakpoints.');
requireText(manager, 'this.theiaBreakpoints.onDidChangeBreakpoints', 'Native gutter/F9 breakpoint changes must be mirrored into NovaOryn runtime debugging.');
requireText(manager, 'this.theiaBreakpoints.getBreakpoints()', 'Theia breakpoints must be authoritative for Debug launch arming.');
requireText(contribution, "id: 'novaoryn.debug.toggleBreakpoint'", 'NovaOryn Debug -> Toggle Breakpoint command is missing.');
requireText(contribution, "menus.registerSubmenu(editorDebugMenu, 'Debug')", 'Editor context menu must contain Debug submenu.');
requireText(contribution, 'EDITOR_LINENUMBER_CONTEXT_MENU', 'The line-number/glyph context menu must be supported.');
requireText(contribution, "label: 'Toggle Breakpoint'", 'Debug context submenu must contain Toggle Breakpoint.');
rejectText(contribution, "keybinding: 'f9'", 'NovaOryn must not compete with Theia native F9 breakpoint keybinding.');
requireText(service, 'await gdb.connect(gdbPort, 15000);', 'Debugger attach retry is required.');
requireText(service, 'NovaOryn.DebugSymbols.json', 'Exact source-line debug manifest is required.');
requireText(service, 'requested line ${line} -> executable line ${resolved.resolvedLine}', 'Breakpoint binding must report requested and resolved executable lines.');
requireText(service, 'Kernel is held before KMain because', 'Unverified requested breakpoints must hold the kernel before KMain.');
requireText(service, 'entry.line > line && entry.line - line <= 8', 'Non-executable C# lines must bind forward to a nearby NativeAOT sequence point.');
requireText(service, 'entry.line < line && line - entry.line <= 3', 'Breakpoint resolver must have a bounded backward fallback.');
requireText(service, "message: 'Unverified breakpoint removed.'", 'Removing a pending unverified breakpoint must not retry it.');
rejectText(service, 'await this.waitForPort(gdbPort', 'Disposable GDB probe must not race the debugger connection.');
console.log('[ OK ] Theia-native breakpoint UI, pre-KMain verification hold, and NativeAOT source-line binding verified.');
