import * as React from 'react';
import { inject, injectable, postConstruct } from 'inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { WorkspaceService } from '@theia/workspace/lib/browser/workspace-service';
import { NovaOrynDeviceTreeNode, NovaOrynDeviceTreeSnapshot, NovaOrynProjectService } from '../common/novaoryn-protocol';

@injectable()
export class NovaOrynHardwareWidget extends ReactWidget {
    static readonly ID = 'novaoryn.hardware.tree';
    static readonly LABEL = 'NovaOryn Hardware';
    @inject(WorkspaceService) protected readonly workspaceService!: WorkspaceService;
    @inject(NovaOrynProjectService) protected readonly projectService!: NovaOrynProjectService;
    protected snapshot?: NovaOrynDeviceTreeSnapshot;
    protected deviceTree: NovaOrynDeviceTreeNode[] = [];
    protected loading = false;

    @postConstruct() protected init(): void {
        this.id = NovaOrynHardwareWidget.ID; this.title.label = NovaOrynHardwareWidget.LABEL;
        this.title.caption = 'NovaOryn hardware and driver configuration tree'; this.title.closable = true;
        this.addClass('novaoryn-hardware-widget'); this.update();
        this.toDispose.push(this.workspaceService.onWorkspaceLocationChanged(() => void this.refresh()));
        void this.refresh();
    }
    async refresh(): Promise<void> {
        const workspace = this.workspaceService.workspace;
        if (!workspace) { this.snapshot = undefined; this.deviceTree = []; this.update(); return; }
        this.loading = true; this.update();
        this.snapshot = await this.projectService.inspectDeviceTree(workspace.resource.path.fsPath());
        this.deviceTree = this.snapshot.roots;
        this.loading = false; this.update();
    }
    protected renderDeviceNode(n: NovaOrynDeviceTreeNode): React.ReactNode {
        return <details open className='novaoryn-hardware-group' key={n.id}><summary><span className='codicon codicon-circuit-board'></span>{n.name}<span className='novaoryn-count'>{n.children.length}</span></summary>
            <div className='novaoryn-hardware-children'>{n.children.length ? n.children.map(c => this.renderDeviceNode(c)) : <div className='novaoryn-hardware-node'><span className='codicon codicon-circle-outline'></span><span>{n.bus}</span><small>{n.state}</small></div>}</div>
        </details>;
    }
    protected group(title: string, icon: string, entries: string[]): React.ReactNode {
        return <details open className='novaoryn-hardware-group'><summary><span className={`codicon codicon-${icon}`}></span>{title}<span className='novaoryn-count'>{entries.length}</span></summary>
            <div className='novaoryn-hardware-children'>{entries.length ? entries.map(item => <div className='novaoryn-hardware-node' key={item}><span className='codicon codicon-circuit-board'></span><span>{item}</span><small>configured</small></div>) : <div className='novaoryn-hardware-empty'>None configured</div>}</div>
        </details>;
    }
    protected render(): React.ReactNode {
        const snapshot = this.snapshot;
        return <div className='novaoryn-tool-page'>
            <div className='novaoryn-tool-header'><div><h2>Hardware / Device Tree</h2><p>The same unified PCI, USB, ACPI, platform, virtual and logical device model exposed by NovaOryn.Kernel.Drivers.</p></div><button className='theia-button' onClick={() => void this.refresh()}>Refresh</button></div>
            {this.loading && <p>Loading hardware configuration…</p>}
            {!this.loading && (!snapshot || !snapshot.roots.length) && <p>{snapshot?.message || 'Open a NovaOryn operating system to inspect its device tree.'}</p>}
            {snapshot && snapshot.roots.length > 0 && <><p className='novaoryn-tool-summary'>{snapshot.counts.total} device nodes — PCI {snapshot.counts.pci}, USB {snapshot.counts.usb}, ACPI {snapshot.counts.acpi}, platform {snapshot.counts.platform}, virtual {snapshot.counts.virtual}, logical {snapshot.counts.logical}.</p><div className='novaoryn-hardware-tree'>{this.deviceTree.map(n => this.renderDeviceNode(n))}</div></>}
        </div>;
    }
}
