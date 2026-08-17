import * as React from 'react';
import { inject, injectable, postConstruct } from 'inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { WorkspaceService } from '@theia/workspace/lib/browser/workspace-service';
import { NovaOrynAnalyzerDiagnostic, NovaOrynAnalyzerSeverity, NovaOrynAnalyzerSnapshot, NovaOrynProjectService } from '../common/novaoryn-protocol';

@injectable()
export class NovaOrynStaticAnalyzerWidget extends ReactWidget {
    static readonly ID = 'novaoryn.static.analyzers';
    static readonly LABEL = 'NovaOryn OS Analyzers';

    @inject(WorkspaceService) protected readonly workspaceService!: WorkspaceService;
    @inject(NovaOrynProjectService) protected readonly projectService!: NovaOrynProjectService;

    protected snapshot: NovaOrynAnalyzerSnapshot | undefined;
    protected loading = false;
    protected severity: 'all' | NovaOrynAnalyzerSeverity = 'all';

    @postConstruct()
    protected init(): void {
        this.id = NovaOrynStaticAnalyzerWidget.ID;
        this.title.label = NovaOrynStaticAnalyzerWidget.LABEL;
        this.title.caption = 'NovaOryn kernel, driver, architecture and userland static analyzers';
        this.title.closable = true;
        this.addClass('novaoryn-static-analyzer-widget');
        this.toDispose.push(this.workspaceService.onWorkspaceLocationChanged(() => { this.snapshot = undefined; this.update(); }));
    }

    protected root(): string | undefined { return this.workspaceService.workspace?.resource.path.fsPath(); }

    async analyze(): Promise<void> {
        const root = this.root(); if (!root) return;
        this.loading = true; this.update();
        try { this.snapshot = await this.projectService.analyzeOperatingSystem(root); }
        finally { this.loading = false; this.update(); }
    }

    protected visibleDiagnostics(): NovaOrynAnalyzerDiagnostic[] {
        if (!this.snapshot) return [];
        return this.severity === 'all' ? this.snapshot.diagnostics : this.snapshot.diagnostics.filter(item => item.severity === this.severity);
    }

    protected shortPath(filePath: string): string {
        const root = this.root();
        return root && filePath.toLowerCase().startsWith(root.toLowerCase()) ? filePath.slice(root.length).replace(/^[/\\]+/, '') : filePath;
    }

    protected render(): React.ReactNode {
        const diagnostics = this.visibleDiagnostics();
        return <div className='novaoryn-tool-page'>
            <div className='novaoryn-tool-header'><div><h2>OS-specific Static Analyzers</h2><p>Analyze NovaOryn kernel, driver and userland source using OS architecture and capability rules rather than generic C# style checks.</p></div><div className='novaoryn-tool-actions'><select value={this.severity} onChange={e => { this.severity = e.target.value as typeof this.severity; this.update(); }}><option value='all'>All severities</option><option value='error'>Errors</option><option value='warning'>Warnings</option><option value='info'>Information</option></select><button className='theia-button main' disabled={!this.root() || this.loading} onClick={() => void this.analyze()}>{this.loading ? 'Analyzing…' : 'Analyze OS'}</button></div></div>
            {!this.root() && <p>Open a NovaOryn operating system to run the analyzers.</p>}
            {this.root() && !this.snapshot && !this.loading && <section className='novaoryn-engineering-section'><h3>Analyzer scope</h3><p>Checks kernel/userland boundaries, architecture leakage, blocking/exception/async kernel patterns, interrupt-handler allocations, and driver capability declarations. Generated output, SDK sources, bin/obj and IDE metadata are excluded.</p></section>}
            {this.snapshot && <>
                <div className='novaoryn-analyzer-summary'>
                    <div><strong>{this.snapshot.errorCount}</strong><span>Errors</span></div><div><strong>{this.snapshot.warningCount}</strong><span>Warnings</span></div><div><strong>{this.snapshot.infoCount}</strong><span>Info</span></div><div><strong>{this.snapshot.filesAnalyzed}</strong><span>C# files</span></div><div><strong>{this.snapshot.targetArchitecture ?? 'unknown'}</strong><span>Active target</span></div>
                </div>
                <section className='novaoryn-engineering-section'><div className='novaoryn-section-heading'><h3>Diagnostics <span className='novaoryn-count'>{diagnostics.length}</span></h3><small className='novaoryn-muted'>NOA rules are NovaOryn OS contracts.</small></div>
                    {diagnostics.length === 0 ? <p className='novaoryn-analyzer-clean'>No diagnostics at the selected severity.</p> : <div className='novaoryn-analyzer-list'>{diagnostics.map((item, index) => <div className={`novaoryn-analyzer-row ${item.severity}`} key={`${item.filePath}:${item.line}:${item.code}:${index}`}>
                        <span className='novaoryn-analyzer-severity'>{item.severity.toUpperCase()}</span><strong>{item.code}</strong><span className='novaoryn-analyzer-message'>{item.message}<small>{item.rule}</small></span><code>{this.shortPath(item.filePath)}:{item.line}:{item.column}</code>
                    </div>)}</div>}
                </section>
                <section className='novaoryn-engineering-section'><h3>Rules currently enforced</h3><div className='novaoryn-analyzer-rule-grid'><span>NOA1001/1002 · userland isolation</span><span>NOA2001–2003 · kernel execution safety</span><span>NOA3001–3003 · hardware/architecture boundaries</span><span>NOA4001 · interrupt allocation safety</span><span>NOA5001 · driver capability declarations</span></div></section>
            </>}
        </div>;
    }
}
