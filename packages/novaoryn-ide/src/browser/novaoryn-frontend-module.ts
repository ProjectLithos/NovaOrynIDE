import { ContainerModule } from 'inversify';
import { CommandContribution, MenuContribution } from '@theia/core/lib/common';
import { FrontendApplicationContribution, WebSocketConnectionProvider, WidgetFactory } from '@theia/core/lib/browser';
import {
    NOVAORYN_PROJECT_SERVICE_PATH,
    NovaOrynProjectService
} from '../common/novaoryn-protocol';
import { NovaOrynContribution } from './novaoryn-contribution';
import { NovaOrynWidget } from './novaoryn-widget';
import { NovaOrynToolbarWidget } from './novaoryn-toolbar-widget';
import { NovaOrynEditorEnvironmentContribution } from './novaoryn-editor-environment';
import { NovaOrynBreakpointManager } from './novaoryn-breakpoint-manager';
import './style/novaoryn.css';

export default new ContainerModule(bind => {
    bind(NovaOrynProjectService).toDynamicValue(ctx => {
        const provider = ctx.container.get(WebSocketConnectionProvider);
        return provider.createProxy<NovaOrynProjectService>(NOVAORYN_PROJECT_SERVICE_PATH);
    }).inSingletonScope();

    bind(NovaOrynWidget).toSelf();
    bind(NovaOrynToolbarWidget).toSelf().inSingletonScope();
    bind(WidgetFactory).toDynamicValue(ctx => ({
        id: NovaOrynWidget.ID,
        createWidget: () => ctx.container.get(NovaOrynWidget)
    })).inSingletonScope();

    bind(NovaOrynBreakpointManager).toSelf().inSingletonScope();

    bind(NovaOrynEditorEnvironmentContribution).toSelf().inSingletonScope();
    bind(FrontendApplicationContribution).toService(NovaOrynEditorEnvironmentContribution);

    bind(NovaOrynContribution).toSelf().inSingletonScope();
    bind(CommandContribution).toService(NovaOrynContribution);
    bind(MenuContribution).toService(NovaOrynContribution);
    bind(FrontendApplicationContribution).toService(NovaOrynContribution);
});
