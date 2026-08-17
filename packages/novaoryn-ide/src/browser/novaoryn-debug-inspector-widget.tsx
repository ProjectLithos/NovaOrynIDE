import * as React from 'react';
import { inject, injectable, postConstruct } from 'inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { FileUri } from '@theia/core/lib/common/file-uri';
import { EditorManager } from '@theia/editor/lib/browser';
import { NovaOrynDebugState, NovaOrynExpressionResult, NovaOrynProjectService } from '../common/novaoryn-protocol';

const WATCH_STORAGE_KEY = 'novaoryn.ide.watchExpressions';

@injectable()
export class NovaOrynDebugInspectorWidget extends ReactWidget {
    static readonly ID = 'novaoryn.debug.inspector';
    static readonly LABEL = 'NovaOryn Debug';

    @inject(EditorManager)
    protected readonly editorManager!: EditorManager;

    @inject(NovaOrynProjectService)
    protected readonly projectService!: NovaOrynProjectService;

    protected state: NovaOrynDebugState = { active: false, paused: false, sourceSymbols: false };
    protected sessionId: string | undefined;
    protected watches: string[] = [];
    protected watchDraft = '';
    protected readonly watchResults = new Map<string, NovaOrynExpressionResult>();
    protected lastPauseKey = '';
    protected refreshGeneration = 0;

    @postConstruct()
    protected init(): void {
        this.id = NovaOrynDebugInspectorWidget.ID;
        this.title.label = NovaOrynDebugInspectorWidget.LABEL;
        this.title.caption = 'NovaOryn watches, call stack, locals/frame values and registers';
        this.title.closable = true;
        this.addClass('novaoryn-debug-inspector-widget');
        this.loadWatches();
        this.update();
    }

    setSession(sessionId: string | undefined): void {
        this.sessionId = sessionId;
        if (!sessionId) {
            this.watchResults.clear();
            this.lastPauseKey = '';
        }
        this.update();
    }

    setState(state: NovaOrynDebugState): void {
        this.state = state;
        this.update();
        const rip = state.registers?.find(item => item.name === 'rip')?.value ?? '';
        const pauseKey = state.paused ? `${state.sourcePath ?? ''}:${state.line ?? 0}:${rip}:${state.message ?? ''}` : '';
        if (pauseKey && pauseKey !== this.lastPauseKey) {
            this.lastPauseKey = pauseKey;
            void this.refreshWatches();
        } else if (!state.paused) {
            this.lastPauseKey = '';
        }
    }

    protected async openFrame(sourcePath: string | undefined, line: number | undefined): Promise<void> {
        if (!sourcePath || !line) { return; }
        const editor = await this.editorManager.open(FileUri.create(sourcePath));
        const position = { line: Math.max(0, line - 1), character: 0 };
        editor.editor.cursor = position;
        editor.editor.revealPosition(position);
    }

    protected loadWatches(): void {
        try {
            const raw = window.localStorage.getItem(WATCH_STORAGE_KEY);
            const parsed = raw ? JSON.parse(raw) : [];
            if (Array.isArray(parsed)) {
                this.watches = parsed.filter(item => typeof item === 'string' && item.trim()).map(item => item.trim()).slice(0, 64);
            }
        } catch { this.watches = []; }
    }

    protected saveWatches(): void {
        try { window.localStorage.setItem(WATCH_STORAGE_KEY, JSON.stringify(this.watches)); } catch { }
    }

    protected addWatch(): void {
        const expression = this.watchDraft.trim();
        if (!expression) { return; }
        if (!this.watches.includes(expression)) { this.watches.push(expression); }
        this.watchDraft = '';
        this.saveWatches();
        this.update();
        void this.refreshWatches();
    }

    protected removeWatch(expression: string): void {
        this.watches = this.watches.filter(item => item !== expression);
        this.watchResults.delete(expression);
        this.saveWatches();
        this.update();
    }

    protected async refreshWatches(): Promise<void> {
        if (!this.sessionId || !this.state.active || !this.state.paused) { return; }
        const generation = ++this.refreshGeneration;
        const results: NovaOrynExpressionResult[] = [];
        // GDB RSP is deliberately serialized: QEMU's stub and NovaOryn's client
        // have one outstanding request slot, so watch reads must never overlap.
        for (const expression of this.watches) {
            if (generation !== this.refreshGeneration) { return; }
            results.push(await this.projectService.evaluateExpression(this.sessionId!, expression));
        }
        if (generation !== this.refreshGeneration) { return; }
        for (const result of results) { this.watchResults.set(result.expression, result); }
        this.update();
    }

    protected renderWatchSection(): React.ReactNode {
        return <section>
            <h3>Watch</h3>
            <div className='novaoryn-watch-entry'>
                <input
                    className='theia-input'
                    type='text'
                    value={this.watchDraft}
                    disabled={!this.state.paused}
                    placeholder='rax, rsp + 8, [rsp+8], (rflags & 1) != 0'
                    title='Expressions support x64 registers, integer arithmetic/bitwise/comparison operators and [address] 64-bit memory reads.'
                    onChange={event => { this.watchDraft = event.target.value; this.update(); }}
                    onKeyDown={event => { if (event.key === 'Enter') { this.addWatch(); } }}
                />
                <button className='theia-button' disabled={!this.state.paused || !this.watchDraft.trim()} onClick={() => this.addWatch()}>Add</button>
                <button className='novaoryn-debug-small-button' disabled={!this.state.paused || this.watches.length === 0} title='Refresh watches' onClick={() => this.refreshWatches()}>
                    <span className='codicon codicon-refresh' aria-hidden='true'></span>
                </button>
            </div>
            <p className='novaoryn-debug-note'>Values refresh whenever the kernel pauses. Use register names such as <code>rax</code>/<code>rsp</code>; <code>[address]</code> reads one 64-bit value from guest memory.</p>
            <div className='novaoryn-debug-table'>
                {this.watches.map(expression => {
                    const result = this.watchResults.get(expression);
                    return <div className='novaoryn-debug-row novaoryn-watch-row' key={expression}>
                        <span className='novaoryn-debug-name'>{expression}</span>
                        <span className={`novaoryn-debug-value ${result && !result.success ? 'error' : ''}`} title={result?.error}>
                            {!this.state.paused ? 'not available while running'
                                : !result ? 'evaluating…'
                                : result.success ? `${result.hexValue}  (${result.value})` : result.error}
                        </span>
                        <button className='novaoryn-debug-small-button' title='Remove watch' onClick={() => this.removeWatch(expression)}>
                            <span className='codicon codicon-close' aria-hidden='true'></span>
                        </button>
                    </div>;
                })}
            </div>
        </section>;
    }

    protected render(): React.ReactNode {
        if (!this.state.active) {
            return <div className='novaoryn-debug-inspector-empty'>Start a NovaOryn Debug session to inspect the kernel.</div>;
        }
        if (!this.state.paused) {
            return <div className='novaoryn-debug-inspector'>
                <div className='novaoryn-debug-inspector-empty'>Kernel running. Pause or stop at a breakpoint to inspect state.</div>
                {this.renderWatchSection()}
            </div>;
        }

        return <div className='novaoryn-debug-inspector'>
            {this.renderWatchSection()}
            <section>
                <h3>Call Stack</h3>
                <div className='novaoryn-debug-table'>
                    {(this.state.callStack ?? []).map(frame => <div
                        className={`novaoryn-debug-row ${frame.sourcePath && frame.line ? 'clickable' : ''}`}
                        key={`${frame.index}:${frame.address}`}
                        title={frame.sourcePath && frame.line ? 'Open this stack frame source location' : undefined}
                        onClick={() => this.openFrame(frame.sourcePath, frame.line)}
                    >
                        <span className='novaoryn-debug-name'>#{frame.index}</span>
                        <span className='novaoryn-debug-value'>{frame.label}</span>
                        <span className='novaoryn-debug-address'>{frame.address}</span>
                    </div>)}
                </div>
            </section>
            <section>
                <h3>Locals / Native Frame</h3>
                {this.state.localsMessage && <p className='novaoryn-debug-note'>{this.state.localsMessage}</p>}
                <div className='novaoryn-debug-table'>
                    {(this.state.locals ?? []).map(variable => <div className='novaoryn-debug-row' key={`${variable.kind}:${variable.name}`}>
                        <span className='novaoryn-debug-name'>{variable.name}</span>
                        <span className='novaoryn-debug-value'>{variable.value}</span>
                    </div>)}
                </div>
            </section>
            <section>
                <h3>Registers</h3>
                <div className='novaoryn-debug-register-grid'>
                    {(this.state.registers ?? []).map(register => <div className='novaoryn-debug-register' key={register.name}>
                        <span>{register.name}</span><code>{register.value}</code>
                    </div>)}
                </div>
            </section>
        </div>;
    }
}
