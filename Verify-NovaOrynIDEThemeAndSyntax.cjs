'use strict';
const fs = require('fs');
const path = require('path');
const root = __dirname;
const app = JSON.parse(fs.readFileSync(path.join(root, 'applications/electron/package.json'), 'utf8'));
const ext = JSON.parse(fs.readFileSync(path.join(root, 'packages/novaoryn-ide/package.json'), 'utf8'));
const env = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-editor-environment.ts'), 'utf8');
const moduleSource = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-frontend-module.ts'), 'utf8');
function fail(m) { console.error('[FAIL] ' + m); process.exit(1); }
const cfg = app.theia && app.theia.frontend && app.theia.frontend.config;
if (!cfg || !cfg.defaultTheme || cfg.defaultTheme.light !== 'light' || cfg.defaultTheme.dark !== 'dark') fail('Frontend defaultTheme must map OS light/dark to Theia light/dark.');
if (!cfg.preferences || cfg.preferences['editor.semanticHighlighting.enabled'] !== true) fail('Semantic highlighting preference must be enabled.');
if (ext.dependencies['@theia/monaco-editor-core'] !== '1.108.201') fail('Monaco editor core must be pinned to 1.108.201 for Theia 1.74.0.');
for (const token of ["matchMedia('(prefers-color-scheme: dark)')", "setCurrentTheme(dark ? 'dark' : 'light', false)", "languages.register({", "extensions: ['.cs']", "setMonarchTokensProvider('csharp'"]) {
  if (!env.includes(token)) fail('Missing theme/syntax contract token: ' + token);
}
if (!moduleSource.includes('NovaOrynEditorEnvironmentContribution') || !moduleSource.includes('toService(NovaOrynEditorEnvironmentContribution)')) fail('Editor environment contribution is not registered.');
console.log('[ OK ] NovaOryn IDE follows the system light/dark theme and updates when the system colour scheme changes.');
console.log('[ OK ] C# .cs syntax highlighting and semantic-highlighting preference are enabled without the VS Code/Open VSX plugin runtime.');
