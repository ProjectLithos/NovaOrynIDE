import * as React from 'react';
import { inject, injectable, postConstruct } from 'inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { NovaOrynProjectService } from '../common/novaoryn-protocol';

@injectable()
export class NovaOrynSdkApiWidget extends ReactWidget {
    static readonly ID = 'novaoryn.sdk.api';
    static readonly LABEL = 'SDK API';

    @inject(NovaOrynProjectService)
    protected readonly projectService!: NovaOrynProjectService;

    protected siteUrl = '';
    protected error = '';

    @postConstruct()
    protected init(): void {
        this.id = NovaOrynSdkApiWidget.ID;
        this.title.label = NovaOrynSdkApiWidget.LABEL;
        this.title.caption = 'NovaOryn SDK API documentation';
        this.title.closable = true;
        this.addClass('novaoryn-sdk-api-widget');
        void this.loadSite();
    }

    protected async loadSite(): Promise<void> {
        try {
            this.siteUrl = await this.projectService.getSdkApiSiteUrl();
            this.error = '';
        } catch (error) {
            this.error = error instanceof Error ? error.message : String(error);
        }
        this.update();
    }

    protected render(): React.ReactNode {
        if (this.error) {
            return <div className='novaoryn-tool-page'>
                <h2>NovaOryn SDK API</h2>
                <p>The bundled SDK API site could not be loaded.</p>
                <pre>{this.error}</pre>
                <button className='theia-button' onClick={() => void this.loadSite()}>Retry</button>
            </div>;
        }
        if (!this.siteUrl) {
            return <div className='novaoryn-tool-page'><h2>NovaOryn SDK API</h2><p>Loading bundled SDK documentation…</p></div>;
        }
        return <iframe
            className='novaoryn-sdk-api-frame'
            src={this.siteUrl}
            title='NovaOryn SDK API'
        />;
    }
}
