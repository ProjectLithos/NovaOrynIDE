import * as React from 'react';
import { inject, injectable, postConstruct } from 'inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { WorkspaceService } from '@theia/workspace/lib/browser/workspace-service';
import { NovaOrynProjectConfiguration, NovaOrynProjectService } from '../common/novaoryn-protocol';

@injectable()
export class NovaOrynDashboardWidget extends ReactWidget {
    static readonly ID='novaoryn.os.dashboard'; static readonly LABEL='NovaOryn Dashboard';
    @inject(WorkspaceService) protected readonly workspaceService!: WorkspaceService;
    @inject(NovaOrynProjectService) protected readonly projectService!: NovaOrynProjectService;
    protected configuration?: NovaOrynProjectConfiguration; protected testCount=0;
    @postConstruct() protected init():void{this.id=NovaOrynDashboardWidget.ID;this.title.label=NovaOrynDashboardWidget.LABEL;this.title.caption='NovaOryn operating-system engineering dashboard';this.title.closable=true;this.addClass('novaoryn-dashboard-widget');this.update();this.toDispose.push(this.workspaceService.onWorkspaceLocationChanged(()=>void this.refresh()));void this.refresh();}
    async refresh():Promise<void>{const p=this.workspaceService.workspace?.resource.path.fsPath();if(!p){this.configuration=undefined;this.testCount=0;this.update();return;}const [c,t]=await Promise.all([this.projectService.readProjectConfiguration(p),this.projectService.listTests(p)]);this.configuration=c.success?c.configuration:undefined;this.testCount=t.length;this.update();}
    protected stat(label:string,value:string,icon:string):React.ReactNode{return <div className='novaoryn-dashboard-stat'><span className={`codicon codicon-${icon}`}></span><div><small>{label}</small><strong>{value}</strong></div></div>}
    protected render():React.ReactNode{const c=this.configuration;if(!c)return <div className='novaoryn-tool-page'><h2>NovaOryn OS Dashboard</h2><p>Open a NovaOryn operating system to display its engineering dashboard.</p></div>;
        return <div className='novaoryn-tool-page'><div className='novaoryn-dashboard-title'><div className='novaoryn-dashboard-logo'></div><div><h1>{c.name}</h1><p>NovaOryn OS engineering dashboard</p></div><button className='theia-button' onClick={()=>void this.refresh()}>Refresh</button></div>
            <div className='novaoryn-dashboard-stats'>{this.stat('Kernel',c.kernelArchitecture,'symbol-structure')}{this.stat('Architecture',c.targetArchitecture,'server-process')}{this.stat('Scheduler',c.scheduler,'clock')}{this.stat('Syscalls',c.syscallModel,'symbol-method')}{this.stat('Drivers',String(c.drivers.length+c.storageControllers.length+c.networkDrivers.length),'circuit-board')}{this.stat('Tests',String(this.testCount),'beaker')}</div>
            <div className='novaoryn-dashboard-grid'>
                <section><h3>Target</h3><dl><dt>Boot</dt><dd>{c.bootArchitecture}</dd><dt>SMP</dt><dd>{c.smp?'Enabled':'Disabled'}</dd><dt>Interrupts</dt><dd>{c.interruptModel}</dd><dt>Virtualisation</dt><dd>{c.virtualisation}</dd></dl></section>
                <section><h3>Kernel</h3><dl><dt>Memory</dt><dd>{c.memorySystem}</dd><dt>Processes</dt><dd>{c.processSupport}</dd><dt>Safety</dt><dd>{c.safetyProfile}</dd><dt>Userland</dt><dd>{c.userland?'Enabled':'Disabled'}</dd></dl></section>
                <section><h3>Hardware</h3><p>{[...c.drivers,...c.storageControllers,...c.networkDrivers].slice(0,8).join(' · ') || 'No drivers selected'}</p></section>
                <section><h3>Diagnostics</h3><p>{c.debugging.join(' · ') || 'No diagnostic facilities selected'}</p><h4>Configured tests</h4><p>{c.testing.join(' · ') || 'None'}</p></section>
            </div>
        </div>}
}
