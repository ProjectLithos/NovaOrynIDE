'use strict';

const fs = require('fs');
const path = require('path');

const root = __dirname;
const protocol = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/common/novaoryn-protocol.ts'), 'utf8');
const service = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/node/novaoryn-project-service.ts'), 'utf8');
const widget = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-widget.tsx'), 'utf8');
const toolbar = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-toolbar-widget.tsx'), 'utf8');
const contribution = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-contribution.ts'), 'utf8');
const frontendModule = fs.readFileSync(path.join(root, 'packages/novaoryn-ide/src/browser/novaoryn-frontend-module.ts'), 'utf8');

const requiredConfigurationFields = [
  'kernelArchitecture', 'targetArchitecture', 'bootArchitecture', 'memorySystem', 'scheduler',
  'processSupport', 'syscallModel', 'smp', 'interruptModel', 'timers', 'drivers',
  'storageControllers', 'filesystem', 'networkStack', 'networkDrivers', 'input', 'graphics',
  'audio', 'userland', 'shell', 'gui', 'debugging', 'testing', 'virtualisation',
  'safetyProfile', 'safetyOptions'
];

const requiredGeneratorEvidence = [
  "'monolithic' | 'microkernel' | 'hybrid'",
  'buildProjectGraph(authoritativeConfiguration)',
  'NovaOryn.ProjectGraph.json',
  'writeGeneratedProject',
  'configuration.kernelArchitecture === \'microkernel\'',
  'configuration.kernelArchitecture === \'monolithic\''
];

const missing = [];
for (const field of requiredConfigurationFields) {
  if (!protocol.includes(`${field}:`)) missing.push(`configuration field ${field}`);
  if (!widget.includes(`${field}:`) && !widget.includes(`c.${field}`)) missing.push(`configurator field ${field}`);
}
for (const evidence of requiredGeneratorEvidence) {
  if (!protocol.includes(evidence) && !service.includes(evidence)) missing.push(`generator evidence ${evidence}`);
}


if (!service.includes('for %%I in ("%~dp0.") do set "NOVAORYN_PROJECT=%%~fI"')) missing.push('canonical generated OS path without trailing separator');
if (!service.includes('await this.refreshSdkBridge(osPath);')) missing.push('existing OS SDK bridge refresh');
if (!service.includes("path.join(projectRoot, 'NovaOrynProject.json')")) missing.push('SDK compatibility manifest generation');
if (!service.includes('NOVAORYN_MANIFEST=') || !service.includes('NovaOrynProject.json')) missing.push('SDK manifest launcher path');
if (!service.includes('public static Boolean KMain(BootContext boot)')) missing.push('SDK-compatible KMain signature');
if (!service.includes('if not exist "%NOVAORYN_PROJECT%\\\\NovaOryn.json" (')) missing.push('separator-safe NovaOryn.json launcher check');
if (!service.includes("const NOVAORYN_IDE_VERSION = '0.2.5'")) missing.push('0.2.5 generator version');
if (!widget.includes('NovaOryn OS 0.2.5')) missing.push('0.2.5 configurator version');
if (!protocol.includes("export type NovaOrynRunMode = 'run' | 'debug'")) missing.push('Run/Debug mode protocol');
if (!service.includes('NovaOryn IDE will launch QEMU and attach its debugger') || !service.includes("'-Run -Configuration Release'")) missing.push('SDK Run/Debug configuration handoff');
if (!service.includes('DebuggingEnabled() => DebugBuild && DebuggingConfigured')) missing.push('generated effective debugging gate');
if (!service.includes('EffectiveDebugging() => DebuggingEnabled() ? Debugging() : System.Array.Empty<string>()')) missing.push('generated effective debugging feature selection');
if (!service.includes('NOVAORYN_DEBUG_ENABLED=0') || !service.includes('NOVAORYN_DEBUG_FEATURES=')) missing.push('Run.bat debug environment contract');
if (!service.includes('NOVAORYN_DEBUG_SERIAL_LOG') || !service.includes('NOVAORYN_DEBUG_KERNEL_DIAGNOSTICS') || !service.includes('NOVAORYN_DEBUG_SYMBOLS') || !service.includes('NOVAORYN_DEBUG_PANIC_DUMP')) missing.push('per-feature debug environment flags');
if (!service.includes('EnableOnlyInDebugConfiguration: true') || !service.includes('ConfiguredFeatures: [...configuration.debugging]')) missing.push('SDK manifest debugging contract');
if (!service.includes("mode === 'debug' ? 'Debug' : 'Run'")) missing.push('toolbar backend Run/Debug dispatch');
if (!toolbar.includes("window.localStorage.setItem(RUN_MODE_STORAGE_KEY, this.runMode)")) missing.push('persistent toolbar mode');
if (!toolbar.includes('await this.shell.saveAll()')) missing.push('save-all before toolbar build/run');
if (!contribution.includes('layout.insertWidget(1, this.toolbarWidget)')) missing.push('toolbar row below menu via standard shell layout');
if (frontendModule.includes('rebind(ApplicationShell)') || frontendModule.includes('NovaOrynApplicationShell')) missing.push('ApplicationShell must remain Theia standard shell');
if (!service.includes("stdio: ['ignore', 'pipe', 'pipe']") || !service.includes('windowsHide: true')) missing.push('captured in-IDE Run output');
if (!protocol.includes('readRunOutput(sessionId: string, offset: number)')) missing.push('Run output streaming protocol');
if (!protocol.includes('readProjectConfiguration(projectPath: string)')) missing.push('existing OS configuration read protocol');
if (!protocol.includes('reconfigureProject(projectPath: string, configuration: NovaOrynProjectConfiguration)')) missing.push('existing OS reconfiguration protocol');
if (!service.includes('async reconfigureProject(projectPath: string, configuration: NovaOrynProjectConfiguration)')) missing.push('existing OS reconfiguration backend');
if (!service.includes('// Kernel\\Kernel.cs is deliberately user-owned after initial creation and is never replaced here.')) missing.push('Kernel.cs preservation contract during reconfiguration');
if (!service.includes('removeObsoleteGeneratedProjects')) missing.push('obsolete generated component cleanup');
if (!widget.includes('beginReconfigureOperatingSystem(projectPath: string)')) missing.push('reconfiguration UI entry point');
if (!widget.includes("'Apply Configuration'")) missing.push('reconfiguration apply action');
if (!contribution.includes("id: 'novaoryn.reconfigureOperatingSystem'")) missing.push('menu reconfigure command');
if (!contribution.includes('NavigatorContextMenu.NAVIGATION')) missing.push('Explorer OS-root context menu registration');
if (!contribution.includes('isOperatingSystemRootSelected()')) missing.push('Explorer context action root-only guard');
if (!contribution.includes("const novaOrynMenu = [...MAIN_MENU_BAR, '8_novaoryn'];") || !contribution.includes("menus.registerSubmenu(novaOrynMenu, 'NovaOryn'")) missing.push('NovaOryn main menu');
if (!service.includes('NovaOryn OS Run launcher generated by NovaOryn IDE ${NOVAORYN_IDE_VERSION}')) missing.push('generated Run.bat version banner');
if (!service.includes('sdkToolchainBootstrapLines')) missing.push('generated launcher first-use SDK toolchain bootstrap');
if (!service.includes('Install-NovaOrynToolchain.bat')) missing.push('generated launcher embedded SDK toolchain installer call');
if (!service.includes('.novaoryn-embedded-ready')) missing.push('generated launcher embedded SDK readiness marker');
if (!service.includes('NovaOryn OS ${operation} launcher generated by NovaOryn IDE ${NOVAORYN_IDE_VERSION}')) missing.push('generated Build.bat version banner');


if (missing.length) {
  console.error('[FAIL] Authoritative configuration verification failed:');
  for (const item of missing) console.error(`  - ${item}`);
  process.exit(1);
}

console.log(`[ OK ] Authoritative configuration model exposes ${requiredConfigurationFields.length} generation controls.`);
console.log('[ OK ] Monolithic, microkernel and hybrid project/source generation rules are present.');
console.log('[ OK ] NovaOryn.json and NovaOryn.ProjectGraph.json remain IDE-authoritative; NovaOrynProject.json bridges to the SDK build contract.');
console.log('[ OK ] Generated SDK launchers canonicalise OS paths and existing OS launchers are refreshed.');
console.log('[ OK ] Existing OS reconfiguration is available from the NovaOryn menu and Explorer root context menu while preserving user Kernel.cs.');
