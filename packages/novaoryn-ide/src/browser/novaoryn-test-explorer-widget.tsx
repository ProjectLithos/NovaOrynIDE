import * as React from 'react';
import { inject, injectable, postConstruct } from 'inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { WorkspaceService } from '@theia/workspace/lib/browser/workspace-service';
import {
    NovaOrynProjectService,
    NovaOrynTestDescriptor,
    NovaOrynHardwareMatrixCase,
    NovaOrynHardwareMatrixPreset
} from '../common/novaoryn-protocol';

@injectable()
export class NovaOrynTestExplorerWidget extends ReactWidget {
    static readonly ID = 'novaoryn.test.explorer';
    static readonly LABEL = 'NovaOryn Tests';

    @inject(WorkspaceService) protected readonly workspaceService!: WorkspaceService;
    @inject(NovaOrynProjectService) protected readonly projectService!: NovaOrynProjectService;

    protected tests: NovaOrynTestDescriptor[] = [];
    protected running?: string;
    protected output = '';
    protected lastExit?: number;
    protected matrixPreset: NovaOrynHardwareMatrixPreset = 'balanced';
    protected matrixCases: NovaOrynHardwareMatrixCase[] = [];
    protected matrixRunning = false;
    protected matrixOutput = '';
    protected matrixExit?: number;
    protected matrixMessage = '';

    @postConstruct()
    protected init(): void {
        this.id = NovaOrynTestExplorerWidget.ID;
        this.title.label = NovaOrynTestExplorerWidget.LABEL;
        this.title.caption = 'Run NovaOryn test programs and QEMU hardware compatibility matrices';
        this.title.closable = true;
        this.addClass('novaoryn-test-widget');
        this.update();
        this.toDispose.push(this.workspaceService.onWorkspaceLocationChanged(() => void this.refresh()));
        void this.refresh();
    }

    async refresh(): Promise<void> {
        const p = this.workspaceService.workspace?.resource.path.fsPath();
        if (!p) {
            this.tests = [];
            this.matrixCases = [];
            this.matrixMessage = '';
            this.update();
            return;
        }
        const [tests, matrix] = await Promise.all([
            this.projectService.listTests(p),
            this.projectService.getHardwareMatrixPlan(p, this.matrixPreset)
        ]);
        this.tests = tests;
        this.matrixCases = matrix.cases;
        this.matrixMessage = matrix.error ?? matrix.message ?? '';
        this.update();
    }

    protected async setMatrixPreset(preset: NovaOrynHardwareMatrixPreset): Promise<void> {
        if (this.matrixRunning) return;
        this.matrixPreset = preset;
        await this.refresh();
    }

    protected async run(test: NovaOrynTestDescriptor): Promise<void> {
        const p = this.workspaceService.workspace?.resource.path.fsPath();
        if (!p || this.running || this.matrixRunning) return;
        this.running = test.id;
        this.output = '';
        this.lastExit = undefined;
        this.update();
        const started = await this.projectService.runTest(p, test.id);
        if (!started.success || !started.runId) {
            this.output = started.error ?? 'Could not start test.';
            this.running = undefined;
            this.update();
            return;
        }
        let offset = 0;
        for (;;) {
            const result = await this.projectService.readTestOutput(started.runId, offset);
            if (result.text) this.output += result.text;
            offset = result.nextOffset;
            this.update();
            if (result.complete) {
                this.lastExit = result.exitCode;
                this.running = undefined;
                this.update();
                break;
            }
            await new Promise(resolve => window.setTimeout(resolve, 100));
        }
    }

    protected async runMatrix(): Promise<void> {
        const p = this.workspaceService.workspace?.resource.path.fsPath();
        if (!p || this.running || this.matrixRunning) return;
        this.matrixRunning = true;
        this.matrixOutput = '';
        this.matrixExit = undefined;
        this.update();
        const started = await this.projectService.runHardwareMatrix(p, this.matrixPreset);
        if (!started.success || !started.runId) {
            this.matrixOutput = started.error ?? 'Could not start QEMU hardware matrix.';
            this.matrixRunning = false;
            this.update();
            return;
        }
        let offset = 0;
        for (;;) {
            const result = await this.projectService.readHardwareMatrixOutput(started.runId, offset);
            if (result.text) this.matrixOutput += result.text;
            offset = result.nextOffset;
            this.matrixCases = result.cases;
            this.update();
            if (result.complete) {
                this.matrixExit = result.exitCode;
                this.matrixRunning = false;
                this.update();
                break;
            }
            await new Promise(resolve => window.setTimeout(resolve, 150));
        }
    }

    protected matrixStatus(testCase: NovaOrynHardwareMatrixCase): React.ReactNode {
        const icon = testCase.status === 'passed' ? 'pass-filled'
            : testCase.status === 'failed' ? 'error'
            : testCase.status === 'running' ? 'loading'
            : testCase.status === 'skipped' ? 'circle-slash' : 'circle-large-outline';
        return <span className={`novaoryn-matrix-status ${testCase.status}`} title={testCase.message ?? testCase.status}>
            <span className={`codicon codicon-${icon}${testCase.status === 'running' ? ' codicon-modifier-spin' : ''}`}></span>
            {testCase.status.toUpperCase()}
        </span>;
    }

    protected render(): React.ReactNode {
        const groups = new Map<string, NovaOrynTestDescriptor[]>();
        for (const t of this.tests) {
            const key = t.source === 'os' ? 'Operating-system tests' : 'Bundled SDK tests';
            groups.set(key, [...(groups.get(key) ?? []), t]);
        }
        const passed = this.matrixCases.filter(item => item.status === 'passed').length;
        const failed = this.matrixCases.filter(item => item.status === 'failed').length;
        const skipped = this.matrixCases.filter(item => item.status === 'skipped').length;
        return <div className='novaoryn-tool-page'>
            <div className='novaoryn-tool-header'>
                <div><h2>Test Explorer</h2><p>Independent program tests plus automated QEMU hardware compatibility coverage.</p></div>
                <button className='theia-button' disabled={this.matrixRunning} onClick={() => void this.refresh()}>Discover Tests</button>
            </div>

            <section className='novaoryn-matrix-section'>
                <div className='novaoryn-matrix-heading'>
                    <div>
                        <h3>QEMU Hardware Test Matrix <span className='novaoryn-count'>{this.matrixCases.length}</span></h3>
                        <p>Builds the OS once, then boots clean QEMU instances across CPU, RAM, storage, networking, graphics, USB/xHCI and firmware variants.</p>
                    </div>
                    <div className='novaoryn-matrix-actions'>
                        <select className='theia-select' value={this.matrixPreset} disabled={this.matrixRunning} onChange={event => void this.setMatrixPreset(event.target.value as NovaOrynHardwareMatrixPreset)}>
                            <option value='balanced'>Balanced matrix</option>
                            <option value='full'>Full Cartesian matrix</option>
                        </select>
                        <button className='theia-button main' disabled={!!this.running || this.matrixRunning} onClick={() => void this.runMatrix()}>{this.matrixRunning ? 'Testing…' : 'Run Hardware Matrix'}</button>
                    </div>
                </div>
                {this.matrixMessage && <p className='novaoryn-debug-note'>{this.matrixMessage}</p>}
                <div className='novaoryn-matrix-summary'>
                    <span><strong>{passed}</strong> passed</span><span><strong>{failed}</strong> failed</span><span><strong>{skipped}</strong> skipped</span><span><strong>{this.matrixCases.length - passed - failed - skipped}</strong> pending/running</span>
                </div>
                <div className='novaoryn-matrix-table'>
                    <div className='novaoryn-matrix-row header'><span>Status</span><span>CPU</span><span>RAM</span><span>Storage</span><span>Network</span><span>Graphics</span><span>USB</span><span>Firmware</span></div>
                    {this.matrixCases.map(testCase => <div className={`novaoryn-matrix-row ${testCase.status}`} key={testCase.id} title={testCase.message}>
                        <span>{this.matrixStatus(testCase)}</span><span>{testCase.cpuCount}</span><span>{testCase.memoryMiB} MiB</span><span>{testCase.storage}</span><span>{testCase.network}</span><span>{testCase.graphics}</span><span>{testCase.usb}</span><span>{testCase.firmware.toUpperCase()}</span>
                    </div>)}
                </div>
                {(this.matrixOutput || this.matrixRunning) && <div className='novaoryn-matrix-output-wrap'>
                    <h4>Matrix Output {this.matrixExit !== undefined && <span className={this.matrixExit === 0 ? 'novaoryn-test-pass' : 'novaoryn-test-fail'}>{this.matrixExit === 0 ? 'PASS' : `FAIL (${this.matrixExit})`}</span>}</h4>
                    <pre className='novaoryn-test-output'>{this.matrixOutput}</pre>
                </div>}
            </section>

            {this.tests.length === 0 && <p>No individual test projects were discovered.</p>}
            {[...groups.entries()].map(([name, tests]) => <section key={name}>
                <h3>{name} <span className='novaoryn-count'>{tests.length}</span></h3>
                <div className='novaoryn-test-list'>{tests.map(t => <div className='novaoryn-test-row' key={t.id}>
                    <span className='codicon codicon-beaker'></span>
                    <div><strong>{t.name}</strong><small>{t.category} · {t.projectPath}</small></div>
                    <button className='theia-button' disabled={!!this.running || this.matrixRunning} onClick={() => void this.run(t)}>{this.running === t.id ? 'Running…' : 'Run'}</button>
                </div>)}</div>
            </section>)}
            {(this.output || this.running) && <section><h3>Test Output {this.lastExit !== undefined && <span className={this.lastExit === 0 ? 'novaoryn-test-pass' : 'novaoryn-test-fail'}>{this.lastExit === 0 ? 'PASS' : `FAIL (${this.lastExit})`}</span>}</h3><pre className='novaoryn-test-output'>{this.output}</pre></section>}
        </div>;
    }
}
