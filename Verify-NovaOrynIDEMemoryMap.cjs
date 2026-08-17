'use strict';
const fs = require('fs');
const path = require('path');
const root = __dirname;
const read = p => fs.readFileSync(path.join(root,p),'utf8');
const checks = [];
function requireText(file, needle, label) {
  const text = read(file);
  if (!text.includes(needle)) throw new Error(`${label}: missing ${needle} in ${file}`);
  checks.push(label);
}
function requireRegex(file, regex, label) {
  const text = read(file);
  if (!regex.test(text)) throw new Error(`${label}: pattern not found in ${file}`);
  checks.push(label);
}
try {
  requireText('packages/novaoryn-ide/src/common/novaoryn-protocol.ts', 'export interface NovaOrynMemoryMapSnapshot', 'memory-map protocol');
  requireText('packages/novaoryn-ide/src/common/novaoryn-protocol.ts', 'inspectMemoryMap(projectPath: string): Promise<NovaOrynMemoryMapSnapshot>', 'memory-map service contract');
  requireText('packages/novaoryn-ide/src/node/novaoryn-project-service.ts', "findLinkedNativeSymbol(session, 'NovaOrynBootContext')", 'boot-context symbol resolution');
  requireText('packages/novaoryn-ide/src/node/novaoryn-project-service.ts', 'header.readBigUInt64LE(0x38)', 'final map address ABI');
  requireText('packages/novaoryn-ide/src/node/novaoryn-project-service.ts', 'header.readBigUInt64LE(0x68)', 'final map flag ABI');
  requireText('packages/novaoryn-ide/src/node/novaoryn-project-service.ts', 'bytes.readBigUInt64LE(offset + 24)', 'UEFI page-count parser');
  requireText('packages/novaoryn-ide/src/node/novaoryn-project-service.ts', "case 7: return { name: 'Conventional Memory', category: 'usable' }", 'UEFI conventional-memory classification');
  requireText('packages/novaoryn-ide/src/browser/novaoryn-memory-map-visualizer-widget.tsx', 'Memory-map Visualiser', 'memory-map widget');
  requireText('packages/novaoryn-ide/src/browser/novaoryn-memory-map-visualizer-widget.tsx', 'Read Memory Map', 'runtime read action');
  requireText('packages/novaoryn-ide/src/browser/novaoryn-memory-map-visualizer-widget.tsx', 'this.update();', 'initial React render');
  requireText('packages/novaoryn-ide/src/browser/novaoryn-contribution.ts', "id: 'novaoryn.engineering.memoryMap'", 'Engineering command');
  requireText('packages/novaoryn-ide/src/browser/novaoryn-contribution.ts', "label: 'Memory-map Visualiser'", 'Engineering menu entry');
  requireText('packages/novaoryn-ide/src/browser/novaoryn-frontend-module.ts', 'NovaOrynMemoryMapVisualizerWidget', 'widget registration');
  requireRegex('packages/novaoryn-ide/src/browser/style/novaoryn.css', /\.novaoryn-memory-track\s*\{/, 'visual memory track styling');
  requireText('Build-NovaOrynIDE.bat', 'Verify-NovaOrynIDEMemoryMap.cjs', 'build gate');
  console.log(`[ OK ] NovaOryn IDE 0.2.7 Memory-map Visualiser contract verified (${checks.length} checks).`);
  process.exit(0);
} catch (error) {
  console.error(`[FAIL] ${error.message}`);
  process.exit(1);
}
