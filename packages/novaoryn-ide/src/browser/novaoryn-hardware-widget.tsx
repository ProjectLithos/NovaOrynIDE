import * as React from 'react';
import { inject, injectable, postConstruct } from 'inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { WorkspaceService } from '@theia/workspace/lib/browser/workspace-service';
import { NovaOrynProjectConfiguration, NovaOrynProjectService } from '../common/novaoryn-protocol';

@injectable()
export class NovaOrynHardwareWidget extends ReactWidget {
    static readonly ID = 'novaoryn.hardware.tree';
    static readonly LABEL = 'NovaOryn Hardware';
    @inject(WorkspaceService) protected readonly workspaceService!: WorkspaceService;
    @inject(NovaOrynProjectService) protected readonly projectService!: NovaOrynProjectService;
    protected configuration?: NovaOrynProjectConfiguration;
    protected loading = false;

    @postConstruct() protected init(): void {
        this.id = NovaOrynHardwareWidget.ID; this.title.label = NovaOrynHardwareWidget.LABEL;
        this.title.caption = 'NovaOryn hardware and driver configuration tree'; this.title.closable = true;
        this.addClass('novaoryn-hardware-widget'); this.update();
        this.toDispose.push(this.workspaceService.onWorkspaceLocationChanged(() => void this.refresh()));
        void this.refresh();
    }
    async refresh(): Promise<void> {
        const workspace = this.workspaceService.workspace; if (!workspace) { this.configuration = undefined; this.update(); return; }
        this.loading = true; this.update();
        const result = await this.projectService.readProjectConfiguration(workspace.resource.path.fsPath());
        this.configuration = result.success ? result.configuration : undefined; this.loading = false; this.update();
    }
    protected group(title: string, icon: string, entries: string[]): React.ReactNode {
        return <details open className='novaoryn-hardware-group'><summary><span className={`codicon codicon-${icon}`}></span>{title}<span className='novaoryn-count'>{entries.length}</span></summary>
            <div className='novaoryn-hardware-children'>{entries.length ? entries.map(item => <div className='novaoryn-hardware-node' key={item}><span className='codicon codicon-circuit-board'></span><span>{item}</span><small>configured</small></div>) : <div className='novaoryn-hardware-empty'>None configured</div>}</div>
        </details>;
    }
    protected render(): React.ReactNode {
        const c = this.configuration;
        return <div className='novaoryn-tool-page'>
            <div className='novaoryn-tool-header'><div><h2>Hardware / Device Tree</h2><p>Devices and subsystems selected by the authoritative NovaOryn OS configuration.</p></div><button className='theia-button' onClick={() => void this.refresh()}>Refresh</button></div>
            {this.loading && <p>Loading hardware configuration…</p>}
            {!this.loading && !c && <p>Open a NovaOryn operating system to inspect its hardware configuration.</p>}
            {c && <div className='novaoryn-hardware-tree'>
                {this.group(`CPU — ${c.targetArchitecture}`, 'server-process', [c.smp ? 'SMP / per-CPU enabled' : 'Single CPU configuration', `Interrupts: ${c.interruptModel}`, ...c.timers])}
                {this.group('Platform / buses', 'circuit-board', c.drivers)}
                {this.group('Storage controllers', 'database', c.storageControllers)}
                {this.group('Networking', 'globe', [...c.networkDrivers, `Stack: ${c.networkStack}`])}
                {this.group('Input', 'keyboard', c.input)}
                {this.group('Graphics', 'device-camera-video', c.graphics)}
                {this.group('Audio', 'unmute', c.audio === 'none' ? [] : [c.audio])}
            </div>}
        </div>;
    }
}
