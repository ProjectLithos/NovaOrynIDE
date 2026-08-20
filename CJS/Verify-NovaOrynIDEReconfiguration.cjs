'use strict';

const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');
const protocol = read('packages/novaoryn-ide/src/common/novaoryn-protocol.ts');
const service = read('packages/novaoryn-ide/src/node/novaoryn-project-service.ts');
const widget = read('packages/novaoryn-ide/src/browser/novaoryn-widget.tsx');
const contribution = read('packages/novaoryn-ide/src/browser/novaoryn-contribution.ts');

const failures = [];
const requireText = (text, evidence, label) => {
    if (!text.includes(evidence)) failures.push(label);
};

requireText(protocol, 'readProjectConfiguration(projectPath: string)', 'read existing configuration protocol');
requireText(protocol, 'reconfigureProject(projectPath: string, configuration: NovaOrynProjectConfiguration)', 'reconfigure existing OS protocol');
requireText(service, 'async reconfigureProject(projectPath: string, configuration: NovaOrynProjectConfiguration)', 'reconfigure backend');
requireText(service, 'removeObsoleteGeneratedProjects', 'obsolete generated component cleanup');
requireText(service, 'generatedProjectDirectory(projectRoot: string, relativePath: string)', 'safe generated-project path validation');
requireText(service, "await fs.rm(projectFile, { force: true });", 'generated project-file removal only');
requireText(service, "await fs.rm(path.join(projectDirectory, 'GeneratedFeature.cs'), { force: true });", 'generated starter-file removal only');
requireText(widget, 'beginReconfigureOperatingSystem(projectPath: string)', 'reconfiguration UI entry point');
requireText(widget, "'Apply Configuration'", 'Apply Configuration action');
requireText(widget, "this.readonlyPath('Operating system name', c.name)", 'OS name locked while reconfiguring');
requireText(widget, 'this.reconfiguringProjectPath', 'reconfiguration state');
requireText(contribution, "id: 'novaoryn.reconfigureOperatingSystem'", 'main reconfigure command');
requireText(contribution, "const novaOrynMenu = [...MAIN_MENU_BAR, '8_novaoryn'];", 'NovaOryn top-level main-menu path');
requireText(contribution, "menus.registerSubmenu(novaOrynMenu, 'NovaOryn'", 'NovaOryn main menu');
requireText(contribution, "pathFromNavigatorSelection(selection, new Set<object>())", 'robust Explorer root selection resolution');
requireText(contribution, 'NavigatorContextMenu.NAVIGATION', 'Explorer context menu');
requireText(contribution, 'isOperatingSystemRootSelected()', 'Explorer root-only visibility guard');
requireText(contribution, 'await this.shell.saveAll();', 'save user editors before opening reconfiguration');

const start = service.indexOf('async reconfigureProject(');
const end = service.indexOf('\n    async createProject(', start);
if (start < 0 || end < 0) {
    failures.push('could not isolate reconfigureProject body');
} else {
    const body = service.slice(start, end);
    if (body.includes("path.join(projectRoot, 'Kernel', 'Kernel.cs')") || body.includes('this.kernelSource(')) {
        failures.push('reconfigureProject must never rewrite Kernel\\Kernel.cs');
    }
    for (const required of [
        "'NovaOryn.json'",
        "'NovaOryn.ProjectGraph.json'",
        "'NovaOrynProject.json'",
        "'GeneratedConfiguration.cs'",
        "'NovaOryn.slnx'",
        "'Build.bat'",
        "'Run.bat'"
    ]) {
        if (!body.includes(required)) failures.push(`reconfigureProject missing generated artifact ${required}`);
    }
}

if (failures.length) {
    console.error('[FAIL] NovaOryn IDE reconfiguration verification failed:');
    for (const failure of failures) console.error(`  - ${failure}`);
    process.exit(1);
}

console.log('[ OK ] Existing NovaOryn OS configuration can be reopened from the NovaOryn menu or Explorer root context menu.');
console.log('[ OK ] Reconfiguration regenerates NovaOryn-owned project/configuration artifacts and preserves user Kernel\\Kernel.cs.');
console.log('[ OK ] Obsolete generator-owned component files are removed without deleting user files or escaping the OS root.');
