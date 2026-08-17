const fs = require('fs');
const path = require('path');
const root = __dirname;
function read(rel) { return fs.readFileSync(path.join(root, rel), 'utf8'); }
function fail(message) { console.error('[FAIL] ' + message); process.exit(1); }

const protocol = read('packages/novaoryn-ide/src/common/novaoryn-protocol.ts');
const service = read('packages/novaoryn-ide/src/node/novaoryn-project-service.ts');
const widget = read('packages/novaoryn-ide/src/browser/novaoryn-widget.tsx');
const contribution = read('packages/novaoryn-ide/src/browser/novaoryn-contribution.ts');
const toolbar = read('packages/novaoryn-ide/src/browser/novaoryn-toolbar-widget.tsx');
const frontendModule = read('packages/novaoryn-ide/src/browser/novaoryn-frontend-module.ts');
const css = read('packages/novaoryn-ide/src/browser/style/novaoryn.css');
const checks = [
  [protocol, "NOVAORYN_OS_ROOT = 'C:\\\\NovaOrynOSes'", 'authoritative OS root'],
  [service, 'listOperatingSystems()', 'OS discovery service'],
  [service, 'path.join(NOVAORYN_OS_ROOT, authoritativeConfiguration.name)', 'forced OS-root generation'],
  [widget, "page: Page = 'startup'", 'startup page'],
  [widget, 'Create New OS', 'create-new action'],
  [widget, 'Open Existing OS', 'existing OS chooser'],
  [widget, 'workspaceService.open(new URI(os.uri), { preserveWindow: true })', 'existing OS open action'],
  [widget, 'extends BaseWidget', 'native BaseWidget implementation'],
  [widget, 'renderStartup()', 'native startup renderer'],
  [widget, 'renderConfiguration()', 'native configuration renderer'],
  [css, 'novaoryn-logo.png', 'startup cat logo asset'],
  [css, 'height: 100%', 'bounded startup/configuration viewport'],
  [css, 'overflow-y: auto', 'startup/configuration scrolling'],
  [css, "background:url('./novaoryn-logo.png')", 'cat logo is used on startup'],
  [contribution, 'workspaceService.close()', 'automatic previous-workspace reset'],
  [contribution, 'NOVAORYN_EXPLICIT_WORKSPACE_OPEN', 'one-shot explicit workspace-open allowance'],
  [widget, 'sessionStorage.setItem(NOVAORYN_EXPLICIT_WORKSPACE_OPEN', 'explicit OS open marker'],
  [contribution, 'layout.insertWidget(1, this.toolbarWidget)', 'dedicated toolbar row below menu'],
  [contribution, 'this.shell.layout', 'standard Theia shell layout insertion'],
  [contribution, 'this.toolbarWidget.refresh()', 'run-button workspace refresh'],
  [toolbar, "RUN_MODE_STORAGE_KEY = 'novaoryn.ide.runMode'", 'persistent run-mode storage'],
  [toolbar, "<option value='run'>No Debug</option>", 'No Debug mode'],
  [toolbar, "<option value='debug'>Debug</option>", 'Debug mode'],
  [toolbar, 'await this.shell.saveAll()', 'save-all before build/run'],
  [toolbar, 'runOperatingSystem(projectPath, this.runMode, requestedBreakpoints)', 'toolbar run dispatch'],
  [toolbar, "getChannel(OUTPUT_CHANNEL_NAME)", 'in-IDE build output channel'],
  [service, "windowsHide: true", 'hidden build process'],
  [service, "stdio: ['ignore', 'pipe', 'pipe']", 'captured build stdout/stderr']
];
const missing = checks.filter(([text, token]) => !text.includes(token)).map(([, , label]) => label);
if (!fs.existsSync(path.join(root, 'packages/novaoryn-ide/src/browser/style/novaoryn-logo.png'))) missing.push('startup cat logo file');
if (widget.includes('ReactWidget')) missing.push('ReactWidget must not be used by startup/configuration widget');
if (frontendModule.includes('rebind(ApplicationShell)') || frontendModule.includes('NovaOrynApplicationShell')) missing.push('ApplicationShell must not be rebound (prevents toolbar/shell DI recursion)');
if (contribution.includes('@inject(WidgetManager)') || contribution.includes('protected readonly widgetManager')) missing.push('NovaOrynContribution must use inherited AbstractViewContribution.widgetManager');
const saveIndex = toolbar.indexOf('await this.shell.saveAll()');
const runIndex = toolbar.indexOf('runOperatingSystem(projectPath, this.runMode, requestedBreakpoints)');
if (saveIndex < 0 || runIndex < 0 || saveIndex > runIndex) missing.push('save-all must complete before build/run dispatch');
if (missing.length) fail(missing.join(', '));
console.log('[ OK ] NovaOryn IDE 0.1.39 native workspace/startup policy verified.');
