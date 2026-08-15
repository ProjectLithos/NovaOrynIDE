import * as React from '@theia/core/shared/react';
import { inject, injectable, postConstruct } from '@theia/core/shared/inversify';
import { MessageService } from '@theia/core/lib/common/message-service';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import {
    KernelArchitecture,
    NovaOrynProjectService
} from '../common/novaoryn-protocol';

export const NOVAORYN_WIDGET_ID = 'novaoryn.project.configurator';

@injectable()
export class NovaOrynWidget extends ReactWidget {
    static readonly ID = NOVAORYN_WIDGET_ID;
    static readonly LABEL = 'NovaOryn OS';

    @inject(NovaOrynProjectService)
    protected readonly projectService!: NovaOrynProjectService;

    @inject(MessageService)
    protected readonly messages!: MessageService;

    protected projectName = 'MyNovaOrynOS';
    protected projectLocation = 'C:\\NovaOrynProjects';
    protected kernelArchitecture: KernelArchitecture = 'monolithic';
    protected creating = false;

    @postConstruct()
    protected init(): void {
        this.id = NovaOrynWidget.ID;
        this.title.label = NovaOrynWidget.LABEL;
        this.title.caption = 'Create and configure a NovaOryn operating system';
        this.title.closable = true;
        this.update();
    }

    protected render(): React.ReactNode {
        return <div className='novaoryn-page'>
            <div className='novaoryn-card'>
                <div className='novaoryn-brand'>NOVAORYN</div>
                <h1>NovaOryn OS SDK</h1>
                <p className='novaoryn-version'>IDE 0.0.10</p>
                <p>Create a new operating system from a configuration that determines the generated source architecture.</p>

                <label>Operating system name</label>
                <input className='theia-input' defaultValue={this.projectName}
                    onChange={event => this.projectName = event.currentTarget.value} />

                <label>Project location</label>
                <input className='theia-input' defaultValue={this.projectLocation}
                    onChange={event => this.projectLocation = event.currentTarget.value} />

                <label>Target architecture</label>
                <select className='theia-select' disabled defaultValue='x86_64'>
                    <option value='x86_64'>x86-64</option>
                </select>

                <label>Kernel architecture</label>
                <select className='theia-select' defaultValue={this.kernelArchitecture}
                    onChange={event => this.kernelArchitecture = event.currentTarget.value as KernelArchitecture}>
                    <option value='monolithic'>Monolithic</option>
                    <option value='microkernel'>Microkernel</option>
                </select>

                <div className='novaoryn-actions'>
                    <button className='theia-button main' disabled={this.creating} onClick={() => this.createProject()}>
                        {this.creating ? 'Creating…' : 'Create Operating System'}
                    </button>
                </div>

                <div className='novaoryn-note'>
                    0.0.10 generates distinct monolithic and microkernel directory layouts, a NovaOryn.json configuration,
                    Kernel.cs with KMain, and Build.bat / Run.bat entry points.
                </div>
            </div>
        </div>;
    }

    protected async createProject(): Promise<void> {
        if (this.creating) {
            return;
        }
        this.creating = true;
        this.update();
        const result = await this.projectService.createProject({
            name: this.projectName.trim(),
            location: this.projectLocation.trim(),
            targetArchitecture: 'x86_64',
            kernelArchitecture: this.kernelArchitecture
        });
        this.creating = false;
        this.update();

        if (result.success) {
            await this.messages.info(`NovaOryn operating system created at ${result.projectPath}`);
        } else {
            await this.messages.error(`Could not create NovaOryn operating system: ${result.error ?? 'Unknown error'}`);
        }
    }
}
