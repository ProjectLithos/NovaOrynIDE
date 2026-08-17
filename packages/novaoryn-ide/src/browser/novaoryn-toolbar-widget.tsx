import * as React from 'react';
import { inject, injectable, postConstruct } from 'inversify';
import { MessageService } from '@theia/core/lib/common';
import { FileUri } from '@theia/core/lib/common/file-uri';
import { ApplicationShell } from '@theia/core/lib/browser/shell/application-shell';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { EditorManager } from '@theia/editor/lib/browser';
import { OutputChannelManager } from '@theia/output/lib/browser/output-channel';
import { WorkspaceService } from '@theia/workspace/lib/browser/workspace-service';
import { NovaOrynDebugCommand, NovaOrynDebugState, NovaOrynProjectService, NovaOrynRunMode } from '../common/novaoryn-protocol';
import { NovaOrynBreakpointManager } from './novaoryn-breakpoint-manager';

const RUN_MODE_STORAGE_KEY = 'novaoryn.ide.runMode';
const OUTPUT_CHANNEL_NAME = 'NovaOryn Build';

@injectable()
export class NovaOrynToolbarWidget extends ReactWidget {
    static readonly ID = 'novaoryn.run.toolbar';

    @inject(WorkspaceService)
    protected readonly workspaceService!: WorkspaceService;

    @inject(ApplicationShell)
    protected readonly shell!: ApplicationShell;

    @inject(EditorManager)
    protected readonly editorManager!: EditorManager;

    @inject(NovaOrynProjectService)
    protected readonly projectService!: NovaOrynProjectService;

    @inject(MessageService)
    protected readonly messageService!: MessageService;

    @inject(OutputChannelManager)
    protected readonly outputChannelManager!: OutputChannelManager;

    @inject(NovaOrynBreakpointManager)
    protected readonly breakpointManager!: NovaOrynBreakpointManager;

    protected runMode: NovaOrynRunMode = 'run';
    protected launching = false;
    protected sessionId: string | undefined;
    protected debugState: NovaOrynDebugState = { active: false, paused: false, sourceSymbols: false };
    protected lastRevealedStop = '';
    protected breakpointsArmedForSession: string | undefined;

    @postConstruct()
    protected init(): void {
        this.id = NovaOrynToolbarWidget.ID;
        this.addClass('novaoryn-run-toolbar-widget');

        const stored = window.localStorage.getItem(RUN_MODE_STORAGE_KEY);
        this.runMode = stored === 'debug' ? 'debug' : 'run';
        this.toDispose.push(this.workspaceService.onWorkspaceLocationChanged(() => this.update()));
        this.toDispose.push(this.editorManager.onCurrentEditorChanged(() => this.update()));
        this.toDispose.push(this.breakpointManager.onDidChange(() => this.update()));
        this.update();
    }

    refresh(): void {
        this.update();
    }

    protected render(): React.ReactNode {
        const hasWorkspace = !!this.currentProjectPath();
        const debug = this.runMode === 'debug';
        const active = debug && this.debugState.active && !!this.sessionId;
        const paused = active && this.debugState.paused;
        const canSetBreakpoint = debug && hasWorkspace;
        return <div className='novaoryn-run-toolbar'>
            <button
                className='theia-button novaoryn-run-button'
                disabled={!hasWorkspace || this.launching || !!this.sessionId}
                title={hasWorkspace ? `Run current NovaOryn OS (${debug ? 'Debug' : 'No Debug'})` : 'Open a NovaOryn OS first'}
                onClick={() => this.runCurrentOperatingSystem()}
            >
                <span className='codicon codicon-play' aria-hidden='true'></span>
                <span>{this.launching ? 'Starting…' : 'Run'}</span>
            </button>
            <select
                className='theia-select novaoryn-run-mode'
                value={this.runMode}
                disabled={!!this.sessionId}
                aria-label='NovaOryn run mode'
                title='NovaOryn run mode'
                onChange={event => this.setRunMode(event.target.value as NovaOrynRunMode)}
            >
                <option value='run'>No Debug</option>
                <option value='debug'>Debug</option>
            </select>
            {debug && <div className='novaoryn-debug-controls' aria-label='NovaOryn debugger controls'>
                <button className='novaoryn-debug-button novaoryn-breakpoint-button' disabled={!canSetBreakpoint} title='Toggle breakpoint on the current source line (also click the far-left editor gutter)' onClick={() => this.toggleBreakpoint()}>
                    <span className='codicon codicon-debug-breakpoint' aria-hidden='true'></span>
                </button>
                <span className='novaoryn-debug-separator'></span>
                <button className='novaoryn-debug-button' disabled={!paused} title='Continue (F5)' onClick={() => this.sendDebugCommand('continue')}>
                    <span className='codicon codicon-debug-continue' aria-hidden='true'></span>
                </button>
                <button className='novaoryn-debug-button' disabled={!active || paused} title='Pause' onClick={() => this.sendDebugCommand('pause')}>
                    <span className='codicon codicon-debug-pause' aria-hidden='true'></span>
                </button>
                <button className='novaoryn-debug-button' disabled={!paused} title='Step Over (F10)' onClick={() => this.sendDebugCommand('step-over')}>
                    <span className='codicon codicon-debug-step-over' aria-hidden='true'></span>
                </button>
                <button className='novaoryn-debug-button' disabled={!paused} title='Step Into (F11)' onClick={() => this.sendDebugCommand('step-into')}>
                    <span className='codicon codicon-debug-step-into' aria-hidden='true'></span>
                </button>
                <button className='novaoryn-debug-button' disabled={!paused} title='Step Out (Shift+F11)' onClick={() => this.sendDebugCommand('step-out')}>
                    <span className='codicon codicon-debug-step-out' aria-hidden='true'></span>
                </button>
                <button className='novaoryn-debug-button' disabled={!active} title='Restart' onClick={() => this.sendDebugCommand('restart')}>
                    <span className='codicon codicon-debug-restart' aria-hidden='true'></span>
                </button>
                <button className='novaoryn-debug-button' disabled={!active} title='Stop (Shift+F5)' onClick={() => this.sendDebugCommand('stop')}>
                    <span className='codicon codicon-debug-stop' aria-hidden='true'></span>
                </button>
                <span className={`novaoryn-debug-state ${paused ? 'paused' : active ? 'running' : ''}`} title={this.debugState.message ?? 'Debugger not attached'}>
                    {paused ? 'Paused' : active ? 'Running' : this.launching ? 'Attaching…' : debug && hasWorkspace ? 'Breakpoints Ready' : 'Debugger'}
                </span>
            </div>}
        </div>;
    }

    protected setRunMode(mode: NovaOrynRunMode): void {
        if (this.sessionId) {
            return;
        }
        this.runMode = mode === 'debug' ? 'debug' : 'run';
        window.localStorage.setItem(RUN_MODE_STORAGE_KEY, this.runMode);
        this.update();
    }

    protected currentProjectPath(): string | undefined {
        const workspace = this.workspaceService.workspace;
        if (!workspace) {
            return undefined;
        }
        return workspace.resource.path.fsPath();
    }

    protected async toggleBreakpoint(): Promise<void> {
        const widget = this.editorManager.currentEditor;
        if (!widget) {
            await this.messageService.warn('Open the C# source file and place the caret on the line where you want a breakpoint.');
            return;
        }
        const editor = widget.editor;
        const sourcePath = editor.uri.path.fsPath();
        const line = editor.cursor.line + 1;
        const result = await this.breakpointManager.toggle(sourcePath, line);
        if (!result.success && this.sessionId) {
            // The source breakpoint remains visible/pending and will be retried on the
            // next Debug launch when fresh native symbols have been generated.
            await this.messageService.warn(result.message ?? 'The breakpoint is pending because NovaOryn could not arm it in the current native image.');
        }
    }

    protected async sendDebugCommand(command: NovaOrynDebugCommand): Promise<void> {
        if (!this.sessionId) {
            return;
        }
        try {
            this.debugState = await this.projectService.debugCommand(this.sessionId, command);
            this.update();
            await this.revealStoppedSource();
        } catch (error) {
            await this.messageService.error(error instanceof Error ? error.message : String(error));
        }
    }

    protected async revealStoppedSource(): Promise<void> {
        if (!this.debugState.paused || !this.debugState.sourcePath || !this.debugState.line) {
            return;
        }
        const key = `${this.debugState.sourcePath}:${this.debugState.line}`;
        if (key === this.lastRevealedStop) {
            return;
        }
        this.lastRevealedStop = key;
        try {
            const editor = await this.editorManager.open(FileUri.create(this.debugState.sourcePath));
            const position = { line: Math.max(0, this.debugState.line - 1), character: 0 };
            editor.editor.cursor = position;
            editor.editor.revealPosition(position);
        } catch {
            // The debugger remains usable even if an editor cannot be opened automatically.
        }
    }

    protected async runCurrentOperatingSystem(): Promise<void> {
        const projectPath = this.currentProjectPath();
        if (!projectPath || this.launching || this.sessionId) {
            return;
        }

        this.launching = true;
        this.debugState = { active: false, paused: false, sourceSymbols: false };
        this.update();
        const channel = this.outputChannelManager.getChannel(OUTPUT_CHANNEL_NAME);
        channel.clear();
        channel.show({ preserveFocus: false });
        channel.appendLine(`[INFO] NovaOryn ${this.runMode === 'debug' ? 'Debug' : 'No Debug'}: ${projectPath}`);
        channel.appendLine('[INFO] Build and launch output follows.');
        channel.appendLine('');

        try {
            channel.appendLine('[INFO] Saving all modified files before build.');
            await this.shell.saveAll();
            channel.appendLine('[ OK ] All modified files saved.');
            channel.appendLine('');

            const requestedBreakpoints = this.runMode === 'debug'
                ? this.breakpointManager.all().map(({ sourcePath, line }) => ({ sourcePath, line }))
                : undefined;
            const result = await this.projectService.runOperatingSystem(projectPath, this.runMode, requestedBreakpoints);
            if (!result.success || !result.sessionId) {
                const message = result.error ?? 'NovaOryn could not start the selected operating system.';
                channel.appendLine(`[FAIL] ${message}`);
                await this.messageService.error(message);
                return;
            }

            this.sessionId = result.sessionId;
            this.launching = false;
            this.update();

            let offset = 0;
            while (this.sessionId === result.sessionId) {
                const output = await this.projectService.readRunOutput(result.sessionId, offset);
                if (output.text) {
                    channel.append(output.text);
                }
                offset = output.nextOffset;

                if (this.runMode === 'debug') {
                    this.debugState = await this.projectService.debugState(result.sessionId);
                    this.breakpointManager.applyRuntimeBreakpoints(this.debugState.breakpoints);
                    if (this.debugState.active && this.breakpointsArmedForSession !== result.sessionId) {
                        this.breakpointsArmedForSession = result.sessionId;
                        this.breakpointManager.setSession(result.sessionId);
                    }
                    this.update();
                    await this.revealStoppedSource();
                }

                if (output.complete) {
                    if (output.error) {
                        channel.appendLine(`\n[FAIL] ${output.error}`);
                    }
                    if (output.exitCode === 0) {
                        channel.appendLine(this.runMode === 'debug' ? '\n[ OK ] NovaOryn debug session ended.' : '\n[ OK ] NovaOryn build/run command completed successfully.');
                    } else {
                        channel.appendLine(`\n[FAIL] NovaOryn build/run command exited with code ${output.exitCode ?? -1}.`);
                        await this.messageService.error(`NovaOryn Run failed with exit code ${output.exitCode ?? -1}.`);
                    }
                    break;
                }

                await new Promise(resolve => window.setTimeout(resolve, 100));
            }
        } catch (error) {
            const message = error instanceof Error ? error.message : String(error);
            channel.appendLine(`\n[FAIL] ${message}`);
            await this.messageService.error(`NovaOryn Run failed: ${message}`);
        } finally {
            this.launching = false;
            this.sessionId = undefined;
            this.breakpointManager.setSession(undefined);
            this.debugState = { active: false, paused: false, sourceSymbols: false };
            this.lastRevealedStop = '';
            this.breakpointsArmedForSession = undefined;
            this.update();
        }
    }
}
