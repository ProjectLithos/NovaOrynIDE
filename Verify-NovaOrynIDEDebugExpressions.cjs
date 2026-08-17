const fs = require('fs');
const path = require('path');
const root = __dirname;
const protocol = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/common/novaoryn-protocol.ts'), 'utf8');
const service = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/node/novaoryn-project-service.ts'), 'utf8');
const manager = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-breakpoint-manager.ts'), 'utf8');
const contribution = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-contribution.ts'), 'utf8');
const inspector = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-debug-inspector-widget.tsx'), 'utf8');

function requireText(text, needle, message) {
  if (!text.includes(needle)) throw new Error(message);
}

requireText(protocol, 'condition?: string;', 'Breakpoint request must carry a condition.');
requireText(protocol, 'hitCondition?: string;', 'Breakpoint request must carry a hit-count rule.');
requireText(protocol, 'evaluateExpression(sessionId: string, expression: string)', 'Debugger protocol must expose expression evaluation.');
requireText(service, 'class NovaOrynExpressionParser', 'Native debugger expression parser is missing.');
requireText(service, 'hitConditionMatches', 'Hit-count matching is missing.');
requireText(service, 'Breakpoint skipped', 'Conditional/hit-count breakpoints must auto-resume when rules do not match.');
requireText(service, 'readPointer', 'Expression evaluator must support guest-memory reads.');
requireText(manager, 'BREAKPOINT_OPTIONS_STORAGE_KEY', 'Breakpoint conditions/hit counts must persist across IDE restarts.');
requireText(manager, 'updateBreakpoint(this.sessionId', 'Live breakpoint option updates must reach the backend.');
requireText(contribution, 'Edit Breakpoint Condition…', 'Editor Debug menu must expose breakpoint conditions.');
requireText(contribution, 'Edit Breakpoint Hit Count…', 'Editor Debug menu must expose breakpoint hit counts.');
requireText(inspector, "WATCH_STORAGE_KEY = 'novaoryn.ide.watchExpressions'", 'Watch expressions must persist across IDE restarts.');
requireText(inspector, 'refreshWatches', 'Watch values must refresh while paused.');
requireText(inspector, 'GDB RSP is deliberately serialized', 'Watch evaluation must serialize GDB requests.');

console.log('[ OK ] NovaOryn conditional/hit-count breakpoint and Watch expression contract verified.');
