import * as React from 'react';
import { inject, injectable, postConstruct } from 'inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { WorkspaceService } from '@theia/workspace/lib/browser/workspace-service';
import { MessageService } from '@theia/core/lib/common';
import { NovaOrynProjectService, NovaOrynTraceSnapshot } from '../common/novaoryn-protocol';

@injectable()
export class NovaOrynTraceWidget extends ReactWidget {
    static readonly ID = 'novaoryn.trace.analyser';
    static readonly LABEL = 'Tracing / Boot Analyser';
    @inject(WorkspaceService) protected readonly workspaceService!: WorkspaceService;
    @inject(NovaOrynProjectService) protected readonly projectService!: NovaOrynProjectService;
    @inject(MessageService) protected readonly messageService!: MessageService;
    protected snapshot: NovaOrynTraceSnapshot = { active: false, capturedAtUtc: '', elapsedMs: 0, events: [], bootStages: [] };
    protected filter = 'all';
    protected polling?: ReturnType<typeof setInterval>;

    @postConstruct()
    protected init(): void {
        this.id = NovaOrynTraceWidget.ID;
        this.title.label = NovaOrynTraceWidget.LABEL;
        this.title.caption = 'NovaOryn kernel event tracing and boot-stage timeline';
        this.title.closable = true;
        this.addClass('novaoryn-trace-widget');
        this.update();
        this.toDispose.push(this.workspaceService.onWorkspaceLocationChanged(() => void this.refresh()));
        this.polling = setInterval(() => void this.refresh(), 900);
        this.toDispose.push({ dispose: () => { if (this.polling) clearInterval(this.polling); } });
        void this.refresh();
    }

    async refresh(): Promise<void> {
        const root = this.workspaceService.workspace?.resource.path.fsPath();
        if (!root) return;
        this.snapshot = await this.projectService.readTraceSnapshot(root);
        this.update();
    }

    protected async saveTrace(): Promise<void> {
        const root = this.workspaceService.workspace?.resource.path.fsPath(); if (!root) return;
        const result = await this.projectService.saveTrace(root);
        if (result.success) await this.messageService.info(`NovaOryn trace saved: ${result.path}`);
        else await this.messageService.warn(result.error ?? 'Trace could not be saved.');
    }

    protected async clearTrace(): Promise<void> {
        const root = this.workspaceService.workspace?.resource.path.fsPath(); if (!root) return;
        this.snapshot = await this.projectService.resetTrace(root); this.update();
    }

    protected render(): React.ReactNode {
        const categories = Array.from(new Set(this.snapshot.events.map(e => e.category))).sort();
        const events = this.filter === 'all' ? this.snapshot.events : this.snapshot.events.filter(e => e.category === this.filter);
        const maxEnd = Math.max(1, ...this.snapshot.bootStages.map(s => s.endMs ?? this.snapshot.elapsedMs));
        return <div className='novaoryn-tool-page'>
            <div className='novaoryn-tool-header'><div><h2>Tracing + Boot Analyser</h2><p>Live kernel-event timeline, boot stages, interrupts, syscalls, scheduler, driver and I/O telemetry.</p></div><div className='novaoryn-tool-actions'><button className='theia-button' onClick={() => void this.refresh()}>Refresh</button><button className='theia-button' onClick={() => void this.saveTrace()}>Save Trace</button><button className='theia-button' onClick={() => void this.clearTrace()}>Clear</button></div></div>
            <div className='novaoryn-dashboard-stats'>
                <div className='novaoryn-dashboard-stat'><span className='codicon codicon-pulse'></span><div><small>Session</small><strong>{this.snapshot.active ? 'Live' : 'Offline'}</strong></div></div>
                <div className='novaoryn-dashboard-stat'><span className='codicon codicon-history'></span><div><small>Elapsed</small><strong>{this.snapshot.elapsedMs.toFixed(1)} ms</strong></div></div>
                <div className='novaoryn-dashboard-stat'><span className='codicon codicon-list-tree'></span><div><small>Trace events</small><strong>{this.snapshot.events.length}</strong></div></div>
                <div className='novaoryn-dashboard-stat'><span className='codicon codicon-rocket'></span><div><small>Boot stages</small><strong>{this.snapshot.bootStages.length}</strong></div></div>
            </div>
            <section className='novaoryn-engineering-section'><h3>Boot timeline</h3>{this.snapshot.bootStages.length === 0 ? <p className='novaoryn-muted'>{this.snapshot.message ?? 'Waiting for kernel boot milestones…'}</p> : <div className='novaoryn-boot-timeline'>{this.snapshot.bootStages.map((stage, i) => { const left = Math.max(0, stage.startMs / maxEnd * 100); const width = Math.max(.8, ((stage.endMs ?? this.snapshot.elapsedMs) - stage.startMs) / maxEnd * 100); return <div className='novaoryn-boot-stage' key={`${stage.name}-${i}`}><div className='novaoryn-boot-stage-label'><strong>{stage.name}</strong><span>{stage.durationMs !== undefined ? `${stage.durationMs.toFixed(2)} ms` : 'running'}</span></div><div className='novaoryn-boot-track'><span className={`novaoryn-boot-bar ${stage.status}`} style={{ marginLeft: `${left}%`, width: `${Math.min(100-left,width)}%` }}></span></div><small>{stage.details}</small></div>})}</div>}</section>
            <section className='novaoryn-engineering-section'><div className='novaoryn-section-heading'><h3>Kernel event timeline</h3><select className='theia-select' value={this.filter} onChange={e => { this.filter=e.target.value; this.update(); }}><option value='all'>All categories</option>{categories.map(c => <option key={c} value={c}>{c}</option>)}</select></div>
                <div className='novaoryn-trace-events'>{events.slice(-1500).reverse().map(e => <div className={`novaoryn-trace-event ${e.severity ?? ''}`} key={e.id}><span className='novaoryn-trace-time'>{e.timestampMs.toFixed(3)} ms</span><span className='novaoryn-trace-category'>{e.category}</span><strong>{e.name}</strong><span>{e.cpuIndex !== undefined ? `CPU ${e.cpuIndex}${e.threadId !== undefined ? ` · T${e.threadId}` : ''}${e.processId ? ` · P${e.processId}` : ''}` : ''}</span><span>{e.durationMs !== undefined ? `${e.durationMs.toFixed(3)} ms` : e.phase}</span><small>{e.details}</small></div>)}</div>
            </section>
            <p className='novaoryn-telemetry-help'>SDK KernelTelemetry is consumed directly as <code>[NOVAORYN:TRACE] category=scheduler name=context-switch cpu=0</code>, <code>[NOVAORYN:BOOT] stage="Drivers" phase=end ms=12.4</code>, and profiler telemetry consumed by the Performance Profiler.</p>
        </div>;
    }
}
