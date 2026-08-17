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
import { NovaOrynDebugInspectorWidget } from './novaoryn-debug-inspector-widget';
import { NovaOrynDashboardWidget } from './novaoryn-dashboard-widget';
import { NovaOrynKernelConsoleWidget } from './novaoryn-kernel-console-widget';
import { NovaOrynHardwareWidget } from './novaoryn-hardware-widget';
import { NovaOrynTestExplorerWidget } from './novaoryn-test-explorer-widget';
import { NovaOrynTraceWidget } from './novaoryn-trace-widget';
import { NovaOrynProfilerWidget } from './novaoryn-profiler-widget';
import { NovaOrynDriverCentreWidget } from './novaoryn-driver-centre-widget';
import { NovaOrynTargetManagerWidget } from './novaoryn-target-manager-widget';
import { NovaOrynStaticAnalyzerWidget } from './novaoryn-static-analyzer-widget';
import './style/novaoryn.css';

export default new ContainerModule(bind => {
    bind(NovaOrynProjectService).toDynamicValue(ctx => {
        const provider = ctx.container.get(WebSocketConnectionProvider);
        return provider.createProxy<NovaOrynProjectService>(NOVAORYN_PROJECT_SERVICE_PATH);
    }).inSingletonScope();

    bind(NovaOrynWidget).toSelf();
    bind(NovaOrynToolbarWidget).toSelf().inSingletonScope();
    bind(NovaOrynDebugInspectorWidget).toSelf().inSingletonScope();
    bind(NovaOrynDashboardWidget).toSelf().inSingletonScope();
    bind(NovaOrynKernelConsoleWidget).toSelf().inSingletonScope();
    bind(NovaOrynHardwareWidget).toSelf().inSingletonScope();
    bind(NovaOrynTestExplorerWidget).toSelf().inSingletonScope();
    bind(NovaOrynTraceWidget).toSelf().inSingletonScope();
    bind(NovaOrynProfilerWidget).toSelf().inSingletonScope();
    bind(NovaOrynDriverCentreWidget).toSelf().inSingletonScope();
    bind(NovaOrynTargetManagerWidget).toSelf().inSingletonScope();
    bind(NovaOrynStaticAnalyzerWidget).toSelf().inSingletonScope();
    bind(WidgetFactory).toDynamicValue(ctx => ({
        id: NovaOrynWidget.ID,
        createWidget: () => ctx.container.get(NovaOrynWidget)
    })).inSingletonScope();
    bind(WidgetFactory).toDynamicValue(ctx => ({
        id: NovaOrynDebugInspectorWidget.ID,
        createWidget: () => ctx.container.get(NovaOrynDebugInspectorWidget)
    })).inSingletonScope();
    bind(WidgetFactory).toDynamicValue(ctx => ({ id: NovaOrynDashboardWidget.ID, createWidget: () => ctx.container.get(NovaOrynDashboardWidget) })).inSingletonScope();
    bind(WidgetFactory).toDynamicValue(ctx => ({ id: NovaOrynKernelConsoleWidget.ID, createWidget: () => ctx.container.get(NovaOrynKernelConsoleWidget) })).inSingletonScope();
    bind(WidgetFactory).toDynamicValue(ctx => ({ id: NovaOrynHardwareWidget.ID, createWidget: () => ctx.container.get(NovaOrynHardwareWidget) })).inSingletonScope();
    bind(WidgetFactory).toDynamicValue(ctx => ({ id: NovaOrynTestExplorerWidget.ID, createWidget: () => ctx.container.get(NovaOrynTestExplorerWidget) })).inSingletonScope();
    bind(WidgetFactory).toDynamicValue(ctx => ({ id: NovaOrynTraceWidget.ID, createWidget: () => ctx.container.get(NovaOrynTraceWidget) })).inSingletonScope();
    bind(WidgetFactory).toDynamicValue(ctx => ({ id: NovaOrynProfilerWidget.ID, createWidget: () => ctx.container.get(NovaOrynProfilerWidget) })).inSingletonScope();
    bind(WidgetFactory).toDynamicValue(ctx => ({ id: NovaOrynDriverCentreWidget.ID, createWidget: () => ctx.container.get(NovaOrynDriverCentreWidget) })).inSingletonScope();
    bind(WidgetFactory).toDynamicValue(ctx => ({ id: NovaOrynTargetManagerWidget.ID, createWidget: () => ctx.container.get(NovaOrynTargetManagerWidget) })).inSingletonScope();
    bind(WidgetFactory).toDynamicValue(ctx => ({ id: NovaOrynStaticAnalyzerWidget.ID, createWidget: () => ctx.container.get(NovaOrynStaticAnalyzerWidget) })).inSingletonScope();

    bind(NovaOrynBreakpointManager).toSelf().inSingletonScope();

    bind(NovaOrynEditorEnvironmentContribution).toSelf().inSingletonScope();
    bind(FrontendApplicationContribution).toService(NovaOrynEditorEnvironmentContribution);

    bind(NovaOrynContribution).toSelf().inSingletonScope();
    bind(CommandContribution).toService(NovaOrynContribution);
    bind(MenuContribution).toService(NovaOrynContribution);
    bind(FrontendApplicationContribution).toService(NovaOrynContribution);
});
