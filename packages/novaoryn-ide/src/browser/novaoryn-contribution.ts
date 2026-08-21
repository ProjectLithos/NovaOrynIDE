import { CommandService } from '@theia/core/lib/common/command';

import { inject, injectable } from 'inversify';
import { BoxLayout } from '@lumino/widgets';
import { Command, CommandContribution, CommandRegistry, MAIN_MENU_BAR, MenuContribution, MenuModelRegistry, MessageService } from '@theia/core/lib/common';
import { SelectionService } from '@theia/core/lib/common/selection-service';
import { AbstractViewContribution, CommonMenus, FrontendApplicationContribution } from '@theia/core/lib/browser';
import { NavigatorContextMenu } from '@theia/navigator/lib/browser/navigator-contribution';
import { WorkspaceService } from '@theia/workspace/lib/browser/workspace-service';
import { EDITOR_CONTEXT_MENU, EDITOR_LINENUMBER_CONTEXT_MENU, EditorManager } from '@theia/editor/lib/browser';
import { OutputChannelManager } from '@theia/output/lib/browser/output-channel';
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
import { NovaOrynSyscallExplorerWidget } from './novaoryn-syscall-explorer-widget';
import { NovaOrynSdkApiWidget } from './novaoryn-sdk-api-widget';
import { NovaOrynImageDiskExplorerWidget } from './novaoryn-image-disk-explorer-widget';
import { NovaOrynPhysicalDebuggerWidget } from './novaoryn-physical-debugger-widget';

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
    export const SYSCALLS: Command = { id: 'novaoryn.engineering.syscalls', label: 'Syscall Explorer' };
    export const IMAGES: Command = { id: 'novaoryn.engineering.imageDiskExplorer', label: 'Image / Disk Explorer' };
    export const PHYSICAL_DEBUGGER: Command = { id: 'novaoryn.engineering.physicalDebugger', label: 'Physical-machine Debugger Transport' };
    export const SDK_API: Command = { id: 'novaoryn.help.sdkApi', label: 'SDK API' };
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

    @inject(CommandService)
    protected readonly commandService!: CommandService;

    @inject(OutputChannelManager)
    protected readonly outputChannelManager!: OutputChannelManager;

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
    @inject(NovaOrynSyscallExplorerWidget) protected readonly syscallExplorerWidget!: NovaOrynSyscallExplorerWidget;
    @inject(NovaOrynSdkApiWidget) protected readonly sdkApiWidget!: NovaOrynSdkApiWidget;
    @inject(NovaOrynImageDiskExplorerWidget) protected readonly imageDiskExplorerWidget!: NovaOrynImageDiskExplorerWidget;
    @inject(NovaOrynPhysicalDebuggerWidget) protected readonly physicalDebuggerWidget!: NovaOrynPhysicalDebuggerWidget;

    protected toolbarInstalled = false;
    protected titleLogoInstalled = false;
    protected bottomPanelControlsInstalled = false;
    protected bottomPanelObserver: MutationObserver | undefined;

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
        commands.registerCommand(NovaOrynCommands.DASHBOARD, { execute: () => this.showEngineeringWidget(this.dashboardWidget), isEnabled: () => !!this.currentOperatingSystemPath() });
        commands.registerCommand(NovaOrynCommands.CONSOLE, { execute: () => this.showEngineeringWidget(this.consoleWidget) });
        commands.registerCommand(NovaOrynCommands.HARDWARE, { execute: () => this.showEngineeringWidget(this.hardwareWidget), isEnabled: () => !!this.currentOperatingSystemPath() });
        commands.registerCommand(NovaOrynCommands.TESTS, { execute: () => this.showEngineeringWidget(this.testExplorerWidget), isEnabled: () => !!this.currentOperatingSystemPath() });
        commands.registerCommand(NovaOrynCommands.TRACE, { execute: () => this.showEngineeringWidget(this.traceWidget), isEnabled: () => !!this.currentOperatingSystemPath() });
        commands.registerCommand(NovaOrynCommands.PROFILER, { execute: () => this.showEngineeringWidget(this.profilerWidget), isEnabled: () => !!this.currentOperatingSystemPath() });
        commands.registerCommand(NovaOrynCommands.DRIVERS, { execute: () => this.showEngineeringWidget(this.driverCentreWidget), isEnabled: () => !!this.currentOperatingSystemPath() });
        commands.registerCommand(NovaOrynCommands.TARGETS, { execute: () => this.showEngineeringWidget(this.targetManagerWidget), isEnabled: () => !!this.currentOperatingSystemPath() });
        commands.registerCommand(NovaOrynCommands.ANALYZERS, {
            execute: () => {
                this.staticAnalyzerWidget.setProjectPath(this.currentOperatingSystemPath());
                return this.showEngineeringWidget(this.staticAnalyzerWidget);
            },
            isEnabled: () => !!this.currentOperatingSystemPath()
        });
        commands.registerCommand(NovaOrynCommands.BINARIES, {
            execute: () => {
                this.binarySymbolExplorerWidget.setProjectPath(this.currentOperatingSystemPath());
                return this.showEngineeringWidget(this.binarySymbolExplorerWidget);
            },
            isEnabled: () => !!this.currentOperatingSystemPath()
        });
        commands.registerCommand(NovaOrynCommands.MEMORY_MAP, {
            execute: () => {
                this.memoryMapVisualizerWidget.setProjectPath(this.currentOperatingSystemPath());
                return this.showEngineeringWidget(this.memoryMapVisualizerWidget);
            },
            isEnabled: () => !!this.currentOperatingSystemPath()
        });
        commands.registerCommand(NovaOrynCommands.INTERRUPTS, {
            execute: () => {
                this.interruptApicVisualizerWidget.setProjectPath(this.currentOperatingSystemPath());
                return this.showEngineeringWidget(this.interruptApicVisualizerWidget);
            },
            isEnabled: () => !!this.currentOperatingSystemPath()
        });
        commands.registerCommand(NovaOrynCommands.SYSCALLS, {
            execute: () => {
                this.syscallExplorerWidget.setProjectPath(this.currentOperatingSystemPath());
                return this.showEngineeringWidget(this.syscallExplorerWidget);
            },
            isEnabled: () => !!this.currentOperatingSystemPath()
        });
        commands.registerCommand(NovaOrynCommands.IMAGES, { execute: async () => { await this.showEngineeringWidget(this.imageDiskExplorerWidget); await this.imageDiskExplorerWidget.refresh(); }, isEnabled: () => !!this.currentOperatingSystemPath() });
        commands.registerCommand(NovaOrynCommands.PHYSICAL_DEBUGGER, { execute: async () => { await this.showEngineeringWidget(this.physicalDebuggerWidget); await this.physicalDebuggerWidget.refresh(); }, isEnabled: () => !!this.currentOperatingSystemPath() });
        commands.registerCommand(NovaOrynCommands.SDK_API, { execute: () => this.showEngineeringWidget(this.sdkApiWidget) });
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
        menus.registerMenuAction([...novaOrynMenu, '2_engineering'], { commandId: NovaOrynCommands.SYSCALLS.id, label: 'Syscall Explorer', order: '12' });
        menus.registerMenuAction([...novaOrynMenu, '2_engineering'], { commandId: NovaOrynCommands.IMAGES.id, label: 'Image / Disk Explorer', order: '13' });
        menus.registerMenuAction([...novaOrynMenu, '2_engineering'], { commandId: NovaOrynCommands.PHYSICAL_DEBUGGER.id, label: 'Physical-machine Debugger Transport', order: '14' });
        menus.registerMenuAction(CommonMenus.HELP, { commandId: NovaOrynCommands.SDK_API.id, label: 'SDK API', order: 'a20' });

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
        this.installBottomPanelControls();
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
                await this.showEngineeringWidget(this.dashboardWidget);
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

    protected async showEngineeringWidget(widget: any): Promise<void> {
        if (!widget.isAttached) {
            await this.shell.addWidget(widget, { area: 'main' });
        }
        if (typeof widget.refresh === 'function') await widget.refresh();
        this.shell.activateWidget(widget.id);
    }

    /**
     * Reinstates Theia/Lumino's normal bottom-panel tab/control strip at the shell
     * level. The previous CSS-only fix could not restore a TabBar after Lumino had
     * hidden or collapsed it.
     */
    protected installBottomPanelControls(): void {
        if (this.bottomPanelControlsInstalled) {
            this.ensureBottomPanelControlStrip();
            return;
        }

        const repair = (): void => {
            window.requestAnimationFrame(() => this.ensureBottomPanelControlStrip());
        };
        this.bottomPanelObserver = new MutationObserver(repair);
        this.bottomPanelObserver.observe(document.body, { childList: true, subtree: true });

        this.ensureBottomPanelControlStrip();
        window.requestAnimationFrame(() => this.ensureBottomPanelControlStrip());
        this.bottomPanelControlsInstalled = true;
    }

    protected ensureBottomPanelControlStrip(): void {
        // Eclipse Theia 1.74 exposes the real bottom DockPanel directly from
        // ApplicationShell. Do not depend on a guessed DOM id.
        const bottom = this.shell.bottomPanel?.node;
        if (!bottom) {
            return;
        }

        bottom.classList.add('novaoryn-bottom-panel-host');

        let strip = bottom.querySelector<HTMLElement>(':scope > .novaoryn-bottom-control-strip');
        if (!strip) {
            strip = document.createElement('div');
            strip.className = 'novaoryn-bottom-control-strip';
            strip.setAttribute('role', 'toolbar');
            strip.setAttribute('aria-label', 'Bottom panel controls');

            const left = document.createElement('div');
            left.className = 'novaoryn-bottom-control-tabs';

            const problems = document.createElement('button');
            problems.type = 'button';
            problems.textContent = 'Problems';
            problems.title = 'Show Problems';
            problems.addEventListener('click', () => void this.activateBottomPanelView('Problems'));

            const output = document.createElement('button');
            output.type = 'button';
            output.textContent = 'Output';
            output.title = 'Show Output';
            output.addEventListener('click', () => void this.activateBottomPanelView('Output'));

            left.append(problems, output);
            output.classList.add('novaoryn-bottom-tab-selected');
            output.setAttribute('aria-selected', 'true');
            problems.setAttribute('aria-selected', 'false');

            const right = document.createElement('div');
            right.className = 'novaoryn-bottom-control-actions';

            const channel = document.createElement('span');
            channel.className = 'novaoryn-bottom-output-channel';
            channel.textContent = 'NovaOryn Build';
            channel.title = 'Active NovaOryn output channel';

            const clear = document.createElement('button');
            clear.type = 'button';
            clear.textContent = 'Clear';
            clear.title = 'Clear NovaOryn Build output';
            clear.addEventListener('click', () => {
                this.outputChannelManager.getChannel('NovaOryn Build').clear();
            });

            const maximize = document.createElement('button');
            maximize.type = 'button';
            maximize.textContent = '↕';
            maximize.title = 'Maximize / restore bottom panel';
            maximize.addEventListener('click', () => {
                this.shell.bottomPanel.toggleMaximized();
            });

            const close = document.createElement('button');
            close.type = 'button';
            close.textContent = '×';
            close.title = 'Close bottom panel';
            close.addEventListener('click', () => {
                // ApplicationShell.collapseBottomPanel() is protected. The public
                // Lumino DockPanel inherits Widget.hide(), which is the correct
                // external way to hide the bottom area.
                this.shell.bottomPanel.hide();
            });

            right.append(channel, clear, maximize, close);
            strip.append(left, right);

            // Lumino owns the DockPanel's managed child layout. The toolbar is
            // therefore an absolute overlay on the shell-owned panel node.
            bottom.appendChild(strip);
        }

        strip.hidden = false;
        strip.style.removeProperty('display');
        strip.style.removeProperty('visibility');
        strip.style.removeProperty('opacity');
    }

    protected async activateBottomPanelView(label: 'Problems' | 'Output'): Promise<void> {
        // Prefer Theia's real view commands. This switches the actual bottom widget,
        // not just the visual state of NovaOryn's replacement control strip.
        const commandIds = label === 'Problems'
            ? ['problems:toggle', 'problems:show', 'problemsView:focus']
            : ['output:toggle', 'output:show', 'workbench.action.output.toggleOutput'];

        for (const commandId of commandIds) {
            try {
                // Theia 1.74 CommandService exposes executeCommand(), but not
                // getCommand(). Unknown command IDs reject/throw, so simply try
                // the compatible command IDs in order and fall through on failure.
                await this.commandService.executeCommand(commandId);
                this.markBottomPanelSelection(label);
                return;
            } catch {
                // Try the next compatible Theia/VS Code command id.
            }
        }

        // Fallback: activate the actual ApplicationShell widget by title.
        const widgets = this.shell.getWidgets('bottom');
        const match = widgets.find(widget =>
            (widget.title?.label ?? '').trim().toLowerCase() === label.toLowerCase()
        );
        if (match) {
            await this.shell.activateWidget(match.id);
            this.markBottomPanelSelection(label);
        }
    }

    protected markBottomPanelSelection(label: 'Problems' | 'Output'): void {
        const bottom = this.shell.bottomPanel?.node;
        if (!bottom) {
            return;
        }

        bottom.querySelectorAll<HTMLButtonElement>('.novaoryn-bottom-control-tabs button').forEach(button => {
            const selected = (button.textContent ?? '').trim().toLowerCase() === label.toLowerCase();
            button.classList.toggle('novaoryn-bottom-tab-selected', selected);
            button.setAttribute('aria-selected', selected ? 'true' : 'false');
        });
    }

    protected clickBottomPanelNativeControl(labels: string[]): void {
        const bottom =
            document.querySelector<HTMLElement>('#theia-bottom-panel')
            ?? document.querySelector<HTMLElement>('.theia-bottom-panel');
        if (!bottom) {
            return;
        }

        const controls = Array.from(bottom.querySelectorAll<HTMLElement>('button,[role="button"]'))
            .filter(node => !node.closest('.novaoryn-bottom-control-strip'));
        const match = controls.find(node => {
            const text = `${node.getAttribute('title') ?? ''} ${node.getAttribute('aria-label') ?? ''} ${node.textContent ?? ''}`.toLowerCase();
            return labels.some(label => text.includes(label.toLowerCase()));
        });
        match?.click();
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
