const fs = require('fs');
function must(file, text, label) {
  const value = fs.readFileSync(file, 'utf8');
  if (!value.includes(text)) { console.error(`[FAIL] ${label}`); process.exit(1); }
  console.log(`[ OK ] ${label}`);
}
must('packages/novaoryn-ide/src/common/novaoryn-protocol.ts', 'analyzeOperatingSystem(projectPath: string): Promise<NovaOrynAnalyzerSnapshot>', 'static analyzer service protocol');
must('packages/novaoryn-ide/src/node/novaoryn-project-service.ts', "'NOA1001'", 'kernel/userland boundary rule');
must('packages/novaoryn-ide/src/node/novaoryn-project-service.ts', "'NOA2001'", 'kernel blocking-operation rule');
must('packages/novaoryn-ide/src/node/novaoryn-project-service.ts', "'NOA3001'", 'hardware abstraction boundary rule');
must('packages/novaoryn-ide/src/node/novaoryn-project-service.ts', "'NOA4001'", 'interrupt allocation rule');
must('packages/novaoryn-ide/src/node/novaoryn-project-service.ts', "'NOA5001'", 'driver capability declaration rule');
must('packages/novaoryn-ide/src/node/novaoryn-project-service.ts', 'activeTarget?.architecture', 'Target Manager architecture integration');
must('packages/novaoryn-ide/src/browser/novaoryn-static-analyzer-widget.tsx', 'OS-specific Static Analyzers', 'static analyzer Engineering view');
must('packages/novaoryn-ide/src/browser/novaoryn-frontend-module.ts', 'NovaOrynStaticAnalyzerWidget', 'static analyzer widget registration');
must('packages/novaoryn-ide/src/browser/novaoryn-contribution.ts', 'NovaOrynCommands.ANALYZERS', 'static analyzer Engineering menu command');
must('packages/novaoryn-ide/src/browser/novaoryn-static-analyzer-widget.tsx', 'this.update();', 'initial analyzer render request');
console.log('[ OK ] NovaOryn IDE 0.3.2 OS-specific static analyzer contract verified.');

must('packages/novaoryn-ide/src/browser/novaoryn-static-analyzer-widget.tsx', 'setProjectPath(projectPath: string | undefined)', 'explicit analyzer OS-path hand-off');
must('packages/novaoryn-ide/src/browser/novaoryn-static-analyzer-widget.tsx', '?? this.projectPath', 'workspace-path fallback');
must('packages/novaoryn-ide/src/browser/novaoryn-contribution.ts', 'this.staticAnalyzerWidget.setProjectPath(this.currentOperatingSystemPath())', 'analyzer command path hand-off');
