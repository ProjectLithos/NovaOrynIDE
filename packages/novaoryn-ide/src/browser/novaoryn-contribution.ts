
import { inject, injectable } from 'inversify';
import { BoxLayout } from '@lumino/widgets';
import { Command, CommandContribution, CommandRegistry, MAIN_MENU_BAR, MenuContribution, MenuModelRegistry, MessageService } from '@theia/core/lib/common';
import { SelectionService } from '@theia/core/lib/common/selection-service';
import { AbstractViewContribution, FrontendApplicationContribution } from '@theia/core/lib/browser';
import { NavigatorContextMenu } from '@theia/navigator/lib/browser/navigator-contribution';
import { WorkspaceService } from '@theia/workspace/lib/browser/workspace-service';
import { EDITOR_CONTEXT_MENU, EDITOR_LINENUMBER_CONTEXT_MENU, EditorManager } from '@theia/editor/lib/browser';
import { NovaOrynBreakpointManager } from './novaoryn-breakpoint-manager';
import { NovaOrynWidget, NOVAORYN_EXPLICIT_WORKSPACE_OPEN } from './novaoryn-widget';
import { NovaOrynToolbarWidget } from './novaoryn-toolbar-widget';
import { NovaOrynDashboardWidget } from './novaoryn-dashboard-widget';
import { NovaOrynKernelConsoleWidget } from './novaoryn-kernel-console-widget';
import { NovaOrynHardwareWidget } from './novaoryn-hardware-widget';
import { NovaOrynTestExplorerWidget } from './novaoryn-test-explorer-widget';
import { NovaOrynTraceWidget } from './novaoryn-trace-widget';
import { NovaOrynProfilerWidget } from './novaoryn-profiler-widget';
import { NovaOrynDriverCentreWidget } from './novaoryn-driver-centre-widget';
import { NovaOrynTargetManagerWidget } from './novaoryn-target-manager-widget';
import { NovaOrynStaticAnalyzerWidget } from './novaoryn-static-analyzer-widget';
import { NovaOrynBinarySymbolExplorerWidget } from './novaoryn-binary-symbol-explorer-widget';
import { NovaOrynMemoryMapVisualizerWidget } from './novaoryn-memory-map-visualizer-widget';
import { NovaOrynInterruptApicVisualizerWidget } from './novaoryn-interrupt-apic-visualizer-widget';

export namespace NovaOrynCommands {
    export const OPEN: Command = {
        id: 'novaoryn.openConfigurator',
        label: 'NovaOryn: Create Operating System'
    };

    export const RECONFIGURE: Command = {
        id: 'novaoryn.reconfigureOperatingSystem',
        label: 'Reconfigure NovaOryn OS'
    };

    export const RECONFIGURE_ROOT_CONTEXT: Command = {
        id: 'novaoryn.reconfigureOperatingSystem.rootContext',
        label: 'Reconfigure NovaOryn OS'
    };

    export const TOGGLE_BREAKPOINT: Command = {
        id: 'novaoryn.debug.toggleBreakpoint',
        label: 'Toggle Breakpoint'
    };

    export const BREAKPOINT_CONDITION: Command = {
        id: 'novaoryn.debug.breakpointCondition',
        label: 'Edit Breakpoint Condition…'
    };

    export const BREAKPOINT_HIT_COUNT: Command = {
        id: 'novaoryn.debug.breakpointHitCount',
        label: 'Edit Breakpoint Hit Count…'
    };
    export const DASHBOARD: Command = { id: 'novaoryn.dashboard', label: 'Open OS Dashboard' };
    export const CONSOLE: Command = { id: 'novaoryn.console', label: 'Open Kernel Console' };
    export const HARDWARE: Command = { id: 'novaoryn.hardware', label: 'Open Hardware / Device Tree' };
    export const TESTS: Command = { id: 'novaoryn.tests', label: 'Open Test Explorer' };
    export const TRACE: Command = { id: 'novaoryn.trace', label: 'Open Tracing / Boot Analyser' };
    export const PROFILER: Command = { id: 'novaoryn.profiler', label: 'Open Performance Profiler' };
    export const DRIVERS: Command = { id: 'novaoryn.engineering.drivers', label: 'Driver Development Centre' };
    export const TARGETS: Command = { id: 'novaoryn.engineering.targets', label: 'Target Manager' };
    export const ANALYZERS: Command = { id: 'novaoryn.engineering.analyzers', label: 'OS-specific Static Analyzers' };
    export const BINARIES: Command = { id: 'novaoryn.engineering.binarySymbols', label: 'Binary / Symbol Explorer' };
    export const MEMORY_MAP: Command = { id: 'novaoryn.engineering.memoryMap', label: 'Memory-map Visualiser' };
    export const INTERRUPTS: Command = { id: 'novaoryn.engineering.interruptApic', label: 'Interrupt / APIC Visualiser' };
}

@injectable()
export class NovaOrynContribution extends AbstractViewContribution<NovaOrynWidget>
    implements CommandContribution, MenuContribution, FrontendApplicationContribution {

    @inject(WorkspaceService)
    protected readonly workspaceService!: WorkspaceService;


    @inject(SelectionService)
    protected readonly selectionService!: SelectionService;

    @inject(EditorManager)
    protected readonly editorManager!: EditorManager;

    @inject(NovaOrynBreakpointManager)
    protected readonly breakpointManager!: NovaOrynBreakpointManager;

    @inject(MessageService)
    protected readonly messageService!: MessageService;

    @inject(NovaOrynToolbarWidget) protected readonly toolbarWidget!: NovaOrynToolbarWidget;
    @inject(NovaOrynDashboardWidget) protected readonly dashboardWidget!: NovaOrynDashboardWidget;
    @inject(NovaOrynKernelConsoleWidget) protected readonly consoleWidget!: NovaOrynKernelConsoleWidget;
    @inject(NovaOrynHardwareWidget) protected readonly hardwareWidget!: NovaOrynHardwareWidget;
    @inject(NovaOrynTestExplorerWidget) protected readonly testExplorerWidget!: NovaOrynTestExplorerWidget;
    @inject(NovaOrynTraceWidget) protected readonly traceWidget!: NovaOrynTraceWidget;
    @inject(NovaOrynProfilerWidget) protected readonly profilerWidget!: NovaOrynProfilerWidget;
    @inject(NovaOrynDriverCentreWidget) protected readonly driverCentreWidget!: NovaOrynDriverCentreWidget;
    @inject(NovaOrynTargetManagerWidget) protected readonly targetManagerWidget!: NovaOrynTargetManagerWidget;
    @inject(NovaOrynStaticAnalyzerWidget) protected readonly staticAnalyzerWidget!: NovaOrynStaticAnalyzerWidget;
    @inject(NovaOrynBinarySymbolExplorerWidget) protected readonly binarySymbolExplorerWidget!: NovaOrynBinarySymbolExplorerWidget;
    @inject(NovaOrynMemoryMapVisualizerWidget) protected readonly memoryMapVisualizerWidget!: NovaOrynMemoryMapVisualizerWidget;
    @inject(NovaOrynInterruptApicVisualizerWidget) protected readonly interruptApicVisualizerWidget!: NovaOrynInterruptApicVisualizerWidget;

    protected toolbarInstalled = false;
    protected titleLogoInstalled = false;

    constructor() {
        super({
            widgetId: NovaOrynWidget.ID,
            widgetName: NovaOrynWidget.LABEL,
            defaultWidgetOptions: { area: 'main' },
            toggleCommandId: NovaOrynCommands.OPEN.id
        });
    }

    registerCommands(commands: CommandRegistry): void {
        commands.registerCommand(NovaOrynCommands.OPEN, {
            execute: () => this.openView({ activate: true, reveal: true })
        });

        commands.registerCommand(NovaOrynCommands.RECONFIGURE, {
            execute: () => this.reconfigureCurrentOperatingSystem(),
            isEnabled: () => !!this.currentOperatingSystemPath(),
            isVisible: () => !!this.currentOperatingSystemPath()
        });

        commands.registerCommand(NovaOrynCommands.RECONFIGURE_ROOT_CONTEXT, {
            execute: () => this.reconfigureCurrentOperatingSystem(),
            isEnabled: () => this.isOperatingSystemRootSelected(),
            isVisible: () => this.isOperatingSystemRootSelected()
        });

        commands.registerCommand(NovaOrynCommands.TOGGLE_BREAKPOINT, {
            execute: () => this.toggleCurrentBreakpoint(),
            // Keep the command present in the editor context menu even when Theia
            // temporarily has no currentEditor while the context menu owns focus.
            isEnabled: () => true,
            isVisible: () => true
        });

        commands.registerCommand(NovaOrynCommands.BREAKPOINT_CONDITION, {
            execute: () => this.editCurrentBreakpointCondition(),
            isEnabled: () => true,
            isVisible: () => true
        });

        commands.registerCommand(NovaOrynCommands.BREAKPOINT_HIT_COUNT, {
            execute: () => this.editCurrentBreakpointHitCount(),
            isEnabled: () => true,
            isVisible: () => true
        });
        commands.registerCommand(NovaOrynCommands.DASHBOARD, { execute: () => this.showEngineeringWidget(this.dashboardWidget, 'main'), isEnabled: () => !!this.currentOperatingSystemPath() });
        commands.registerCommand(NovaOrynCommands.CONSOLE, { execute: () => this.showEngineeringWidget(this.consoleWidget, 'bottom') });
        commands.registerCommand(NovaOrynCommands.HARDWARE, { execute: () => this.showEngineeringWidget(this.hardwareWidget, 'left'), isEnabled: () => !!this.currentOperatingSystemPath() });
        commands.registerCommand(NovaOrynCommands.TESTS, { execute: () => this.showEngineeringWidget(this.testExplorerWidget, 'left'), isEnabled: () => !!this.currentOperatingSystemPath() });
        commands.registerCommand(NovaOrynCommands.TRACE, { execute: () => this.showEngineeringWidget(this.traceWidget, 'main'), isEnabled: () => !!this.currentOperatingSystemPath() });
        commands.registerCommand(NovaOrynCommands.PROFILER, { execute: () => this.showEngineeringWidget(this.profilerWidget, 'main'), isEnabled: () => !!this.currentOperatingSystemPath() });
        commands.registerCommand(NovaOrynCommands.DRIVERS, { execute: () => this.showEngineeringWidget(this.driverCentreWidget, 'main'), isEnabled: () => !!this.currentOperatingSystemPath() });
        commands.registerCommand(NovaOrynCommands.TARGETS, { execute: () => this.showEngineeringWidget(this.targetManagerWidget, 'main'), isEnabled: () => !!this.currentOperatingSystemPath() });
        commands.registerCommand(NovaOrynCommands.ANALYZERS, {
            execute: () => {
                this.staticAnalyzerWidget.setProjectPath(this.currentOperatingSystemPath());
                return this.showEngineeringWidget(this.staticAnalyzerWidget, 'main');
            },
            isEnabled: () => !!this.currentOperatingSystemPath()
        });
        commands.registerCommand(NovaOrynCommands.BINARIES, {
            execute: () => {
                this.binarySymbolExplorerWidget.setProjectPath(this.currentOperatingSystemPath());
                return this.showEngineeringWidget(this.binarySymbolExplorerWidget, 'main');
            },
            isEnabled: () => !!this.currentOperatingSystemPath()
        });
        commands.registerCommand(NovaOrynCommands.MEMORY_MAP, {
            execute: () => {
                this.memoryMapVisualizerWidget.setProjectPath(this.currentOperatingSystemPath());
                return this.showEngineeringWidget(this.memoryMapVisualizerWidget, 'main');
            },
            isEnabled: () => !!this.currentOperatingSystemPath()
        });
        commands.registerCommand(NovaOrynCommands.INTERRUPTS, {
            execute: () => {
                this.interruptApicVisualizerWidget.setProjectPath(this.currentOperatingSystemPath());
                return this.showEngineeringWidget(this.interruptApicVisualizerWidget, 'main');
            },
            isEnabled: () => !!this.currentOperatingSystemPath()
        });
    }


    registerMenus(menus: MenuModelRegistry): void {
        menus.registerMenuAction(['1_file', '1_new'], {
            commandId: NovaOrynCommands.OPEN.id,
            label: 'NovaOryn Operating System'
        });

        const novaOrynMenu = [...MAIN_MENU_BAR, '8_novaoryn'];
        menus.registerSubmenu(novaOrynMenu, 'NovaOryn', { sortString: '8' });
        menus.registerSubmenu([...novaOrynMenu, '2_engineering'], 'Engineering');
        menus.registerMenuAction([...novaOrynMenu, '1_configuration'], {
            commandId: NovaOrynCommands.RECONFIGURE.id,
            label: 'Reconfigure OS'
        });
        menus.registerMenuAction([...novaOrynMenu, '2_engineering'], { commandId: NovaOrynCommands.DASHBOARD.id, label: 'OS Dashboard', order: '0' });
        menus.registerMenuAction([...novaOrynMenu, '2_engineering'], { commandId: NovaOrynCommands.CONSOLE.id, label: 'Kernel Console', order: '1' });
        menus.registerMenuAction([...novaOrynMenu, '2_engineering'], { commandId: NovaOrynCommands.HARDWARE.id, label: 'Hardware / Device Tree', order: '2' });
        menus.registerMenuAction([...novaOrynMenu, '2_engineering'], { commandId: NovaOrynCommands.TESTS.id, label: 'Test Explorer', order: '3' });
        menus.registerMenuAction([...novaOrynMenu, '2_engineering'], { commandId: NovaOrynCommands.TRACE.id, label: 'Tracing / Boot Analyser', order: '4' });
        menus.registerMenuAction([...novaOrynMenu, '2_engineering'], { commandId: NovaOrynCommands.PROFILER.id, label: 'Performance Profiler', order: '5' });
        menus.registerMenuAction([...novaOrynMenu, '2_engineering'], { commandId: NovaOrynCommands.DRIVERS.id, label: 'Driver Development Centre', order: '6' });
        menus.registerMenuAction([...novaOrynMenu, '2_engineering'], { commandId: NovaOrynCommands.TARGETS.id, label: 'Target Manager', order: '7' });
        menus.registerMenuAction([...novaOrynMenu, '2_engineering'], { commandId: NovaOrynCommands.ANALYZERS.id, label: 'OS-specific Static Analyzers', order: '8' });
        menus.registerMenuAction([...novaOrynMenu, '2_engineering'], { commandId: NovaOrynCommands.BINARIES.id, label: 'Binary / Symbol Explorer', order: '9' });
        menus.registerMenuAction([...novaOrynMenu, '2_engineering'], { commandId: NovaOrynCommands.MEMORY_MAP.id, label: 'Memory-map Visualiser', order: '10' });
        menus.registerMenuAction([...novaOrynMenu, '2_engineering'], { commandId: NovaOrynCommands.INTERRUPTS.id, label: 'Interrupt / APIC Visualiser', order: '11' });

        menus.registerMenuAction(NavigatorContextMenu.NAVIGATION, {
            commandId: NovaOrynCommands.RECONFIGURE_ROOT_CONTEXT.id,
            label: 'Reconfigure NovaOryn OS',
            order: '0'
        });

        const editorDebugMenu = [...EDITOR_CONTEXT_MENU, '2_novaoryn_debug'];
        menus.registerSubmenu(editorDebugMenu, 'Debug');
        menus.registerMenuAction([...editorDebugMenu, '1_breakpoints'], {
            commandId: NovaOrynCommands.TOGGLE_BREAKPOINT.id,
            label: 'Toggle Breakpoint',
            order: '0'
        });
        menus.registerMenuAction([...editorDebugMenu, '1_breakpoints'], {
            commandId: NovaOrynCommands.BREAKPOINT_CONDITION.id,
            label: 'Edit Breakpoint Condition…',
            order: '1'
        });
        menus.registerMenuAction([...editorDebugMenu, '1_breakpoints'], {
            commandId: NovaOrynCommands.BREAKPOINT_HIT_COUNT.id,
            label: 'Edit Breakpoint Hit Count…',
            order: '2'
        });

        // Theia uses a distinct menu for right-clicks on the line-number/glyph
        // gutter. Link the same Debug submenu there so both source and gutter
        // context menus expose Debug -> Toggle Breakpoint.
        menus.linkCompoundMenuNode({
            newParentPath: EDITOR_LINENUMBER_CONTEXT_MENU,
            submenuPath: editorDebugMenu
        });
    }

    async onStart(): Promise<void> {
        this.installTitleLogo();
        this.installToolbarBelowMenu();
        await this.workspaceService.ready;
        this.toolbarWidget.refresh();
        this.staticAnalyzerWidget.setProjectPath(this.currentOperatingSystemPath());

        // NovaOryn IDE must always start at its own OS chooser. The only time an
        // already-open workspace is allowed through startup is the one-shot reload
        // initiated by an explicit Open Existing OS action in NovaOryn itself.
        const explicitOpen = window.sessionStorage.getItem(NOVAORYN_EXPLICIT_WORKSPACE_OPEN);
        if (explicitOpen) {
            window.sessionStorage.removeItem(NOVAORYN_EXPLICIT_WORKSPACE_OPEN);
            if (this.workspaceService.opened) {
                await this.showEngineeringWidget(this.dashboardWidget, 'main');
                return;
            }
        } else if (this.workspaceService.opened) {
            await this.workspaceService.close();
            return;
        }

        await this.openView({ activate: true, reveal: true });
    }



    protected installTitleLogo(): void {
        if (this.titleLogoInstalled || document.getElementById('novaoryn-title-logo')) { return; }
        const logo = document.createElement('div');
        logo.id = 'novaoryn-title-logo';
        logo.setAttribute('role', 'img');
        logo.setAttribute('aria-label', 'NovaOryn IDE');
        logo.title = 'NovaOryn IDE';
        document.body.appendChild(logo);
        document.body.classList.add('novaoryn-has-title-logo');
        this.titleLogoInstalled = true;
    }

    protected async toggleCurrentBreakpoint(): Promise<void> {
        const context = this.breakpointManager.consumeContextLocation();
        if (context && context.sourcePath.toLowerCase().endsWith('.cs')) {
            await this.breakpointManager.toggle(context.sourcePath, context.line);
            return;
        }

        const widget = this.editorManager.currentEditor ?? this.editorManager.activeEditor;
        if (!widget) {
            await this.messageService.warn('Open a C# source file before toggling a breakpoint.');
            return;
        }
        const sourcePath = widget.editor.uri.path.fsPath();
        if (!sourcePath.toLowerCase().endsWith('.cs')) {
            await this.messageService.warn('Breakpoints can currently be placed in C# source files.');
            return;
        }
        const line = widget.editor.cursor.line + 1;
        if (line < 1) {
            await this.messageService.warn('Place the caret on the source line where you want the breakpoint.');
            return;
        }
        await this.breakpointManager.toggle(sourcePath, line);
    }


    protected currentSourceLocation(): { sourcePath: string; line: number } | undefined {
        const context = this.breakpointManager.consumeContextLocation();
        if (context && context.sourcePath.toLowerCase().endsWith('.cs')) { return context; }
        const widget = this.editorManager.currentEditor ?? this.editorManager.activeEditor;
        if (!widget) { return undefined; }
        const sourcePath = widget.editor.uri.path.fsPath();
        if (!sourcePath.toLowerCase().endsWith('.cs')) { return undefined; }
        return { sourcePath, line: widget.editor.cursor.line + 1 };
    }

    protected async editCurrentBreakpointCondition(): Promise<void> {
        const location = this.currentSourceLocation();
        if (!location) {
            await this.messageService.warn('Open a C# source file and select the breakpoint line first.');
            return;
        }
        const current = this.breakpointManager.getOptions(location.sourcePath, location.line).condition ?? '';
        const value = window.prompt(
            'Breakpoint condition. Use x64 registers and integer expressions, for example: rax == 0x10, (rflags & 1) != 0, or [rsp+8] == 0. Leave blank to remove the condition.',
            current
        );
        if (value === null) { return; }
        const result = await this.breakpointManager.setCondition(location.sourcePath, location.line, value);
        if (result && !result.success) { await this.messageService.warn(result.message ?? 'Could not update breakpoint condition.'); }
    }

    protected async editCurrentBreakpointHitCount(): Promise<void> {
        const location = this.currentSourceLocation();
        if (!location) {
            await this.messageService.warn('Open a C# source file and select the breakpoint line first.');
            return;
        }
        const current = this.breakpointManager.getOptions(location.sourcePath, location.line).hitCondition ?? '';
        const value = window.prompt(
            'Breakpoint hit count. Examples: 5 (break on 5th hit), >=10, >20, <=3, <3, or %100 (every 100th hit). Leave blank to remove the hit-count rule.',
            current
        );
        if (value === null) { return; }
        const trimmed = value.trim();
        if (trimmed && !/^(?:=|==|>=|<=|>|<|%)?\s*[1-9][0-9]*$/.test(trimmed)) {
            await this.messageService.warn('Invalid hit-count rule. Use N, =N, >=N, >N, <=N, <N, or %N.');
            return;
        }
        const result = await this.breakpointManager.setHitCondition(location.sourcePath, location.line, trimmed);
        if (result && !result.success) { await this.messageService.warn(result.message ?? 'Could not update breakpoint hit count.'); }
    }

    protected currentOperatingSystemPath(): string | undefined {
        const workspace = this.workspaceService.workspace;
        if (!workspace) {
            return undefined;
        }
        return workspace.resource.path.fsPath();
    }

    protected selectedNavigatorPath(): string | undefined {
        const rawSelection = this.selectionService.selection as unknown;
        const selection = Array.isArray(rawSelection) ? rawSelection[0] : rawSelection;
        return this.pathFromNavigatorSelection(selection, new Set<object>());
    }

    protected pathFromNavigatorSelection(selection: unknown, visited: Set<object>): string | undefined {
        if (!selection || typeof selection !== 'object' || visited.has(selection)) {
            return undefined;
        }
        visited.add(selection);

        const candidate = selection as Record<string, unknown>;
        const path = candidate['path'];
        if (path && typeof path === 'object') {
            const fsPath = (path as { fsPath?: () => string }).fsPath;
            if (typeof fsPath === 'function') {
                return fsPath.call(path);
            }
        }

        const fsPath = candidate['fsPath'];
        if (typeof fsPath === 'string') {
            return fsPath;
        }
        if (typeof fsPath === 'function') {
            return (fsPath as () => string).call(selection);
        }

        for (const key of ['uri', 'resource', 'fileStat', 'stat']) {
            const nestedPath = this.pathFromNavigatorSelection(candidate[key], visited);
            if (nestedPath) {
                return nestedPath;
            }
        }
        return undefined;
    }

    protected isOperatingSystemRootSelected(): boolean {
        const projectPath = this.currentOperatingSystemPath();
        const selectedPath = this.selectedNavigatorPath();
        if (!projectPath || !selectedPath) {
            return false;
        }
        return projectPath.toLowerCase() === selectedPath.toLowerCase();
    }

    protected async reconfigureCurrentOperatingSystem(): Promise<void> {
        const projectPath = this.currentOperatingSystemPath();
        if (!projectPath) {
            return;
        }

        // The context-menu form is intended for the OS root. Menu-bar invocation
        // has no navigator selection requirement and always targets the open OS.
        await this.shell.saveAll();
        const widget = await this.widgetManager.getOrCreateWidget<NovaOrynWidget>(NovaOrynWidget.ID);
        if (await widget.beginReconfigureOperatingSystem(projectPath)) {
            await this.openView({ activate: true, reveal: true });
        }
    }

    protected async showEngineeringWidget(widget: any, area: 'main' | 'bottom' | 'left'): Promise<void> {
        if (!widget.isAttached) {
            await this.shell.addWidget(widget, { area });
        }
        if (typeof widget.refresh === 'function') await widget.refresh();
        this.shell.activateWidget(widget.id);
    }

    /**
     * Inserts the NovaOryn controls into the already-created Theia shell layout.
     * Do not replace/rebind ApplicationShell: the toolbar itself depends on the
     * shell for saveAll(), so rebinding the shell to a class that injects the
     * toolbar creates a circular Inversify dependency and can exhaust V8 memory.
     */
    protected installToolbarBelowMenu(): void {
        if (this.toolbarInstalled || this.toolbarWidget.parent) {
            this.toolbarInstalled = true;
            return;
        }

        const layout = this.shell.layout;
        if (!(layout instanceof BoxLayout)) {
            throw new Error('NovaOryn could not install the Run toolbar: Theia root layout is not a BoxLayout.');
        }

        layout.insertWidget(1, this.toolbarWidget);
        this.toolbarInstalled = true;
    }
}
