import * as React from 'react';
import { inject, injectable, postConstruct } from 'inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { WorkspaceService } from '@theia/workspace/lib/browser/workspace-service';
import { NovaOrynProjectService, NovaOrynTestDescriptor } from '../common/novaoryn-protocol';

@injectable()
export class NovaOrynTestExplorerWidget extends ReactWidget {
    static readonly ID = 'novaoryn.test.explorer'; static readonly LABEL = 'NovaOryn Tests';
    @inject(WorkspaceService) protected readonly workspaceService!: WorkspaceService;
    @inject(NovaOrynProjectService) protected readonly projectService!: NovaOrynProjectService;
    protected tests: NovaOrynTestDescriptor[] = []; protected running?: string; protected output = ''; protected lastExit?: number;
    @postConstruct() protected init(): void { this.id = NovaOrynTestExplorerWidget.ID; this.title.label = NovaOrynTestExplorerWidget.LABEL; this.title.caption = 'Discover, run and inspect individual NovaOryn test programs'; this.title.closable = true; this.addClass('novaoryn-test-widget'); this.update(); this.toDispose.push(this.workspaceService.onWorkspaceLocationChanged(() => void this.refresh())); void this.refresh(); }
    async refresh(): Promise<void> { const p=this.workspaceService.workspace?.resource.path.fsPath(); this.tests=p ? await this.projectService.listTests(p) : []; this.update(); }
    protected async run(test: NovaOrynTestDescriptor): Promise<void> {
        const p=this.workspaceService.workspace?.resource.path.fsPath(); if (!p || this.running) return; this.running=test.id; this.output=''; this.lastExit=undefined; this.update();
        const started=await this.projectService.runTest(p,test.id); if (!started.success || !started.runId) { this.output=started.error ?? 'Could not start test.'; this.running=undefined; this.update(); return; }
        let offset=0; for (;;) { const result=await this.projectService.readTestOutput(started.runId,offset); if (result.text) this.output+=result.text; offset=result.nextOffset; this.update(); if (result.complete) { this.lastExit=result.exitCode; this.running=undefined; this.update(); break; } await new Promise(r=>window.setTimeout(r,100)); }
    }
    protected render(): React.ReactNode {
        const groups=new Map<string,NovaOrynTestDescriptor[]>(); for(const t of this.tests){const k=t.source==='os'?'Operating-system tests':'Bundled SDK tests';groups.set(k,[...(groups.get(k)??[]),t]);}
        return <div className='novaoryn-tool-page'><div className='novaoryn-tool-header'><div><h2>Test Explorer</h2><p>Each discovered NovaOryn test project is treated as an independent executable program.</p></div><button className='theia-button' onClick={()=>void this.refresh()}>Discover Tests</button></div>
            {this.tests.length===0 && <p>No individual test projects were discovered.</p>}
            {[...groups.entries()].map(([name,tests])=><section key={name}><h3>{name} <span className='novaoryn-count'>{tests.length}</span></h3><div className='novaoryn-test-list'>{tests.map(t=><div className='novaoryn-test-row' key={t.id}><span className='codicon codicon-beaker'></span><div><strong>{t.name}</strong><small>{t.category} · {t.projectPath}</small></div><button className='theia-button' disabled={!!this.running} onClick={()=>void this.run(t)}>{this.running===t.id?'Running…':'Run'}</button></div>)}</div></section>)}
            {(this.output || this.running) && <section><h3>Test Output {this.lastExit!==undefined && <span className={this.lastExit===0?'novaoryn-test-pass':'novaoryn-test-fail'}>{this.lastExit===0?'PASS':`FAIL (${this.lastExit})`}</span>}</h3><pre className='novaoryn-test-output'>{this.output}</pre></section>}
        </div>;
    }
}
