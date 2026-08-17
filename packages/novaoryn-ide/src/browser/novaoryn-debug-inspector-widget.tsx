import * as React from 'react';
import { inject, injectable, postConstruct } from 'inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { FileUri } from '@theia/core/lib/common/file-uri';
import { EditorManager } from '@theia/editor/lib/browser';
import { NovaOrynDebugState, NovaOrynExceptionBreakpointSettings, NovaOrynExpressionResult, NovaOrynProjectService } from '../common/novaoryn-protocol';

const WATCH_STORAGE_KEY = 'novaoryn.ide.watchExpressions';
const EXCEPTION_STORAGE_KEY = 'novaoryn.ide.exceptionBreakpoints';
const DEFAULT_EXCEPTION_VECTORS = [0, 2, 6, 8, 12, 13, 14, 18];

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
    protected exceptionSettings: NovaOrynExceptionBreakpointSettings = { vectors: [...DEFAULT_EXCEPTION_VECTORS], breakOnPanic: true };


    @postConstruct()
    protected init(): void {
        this.id = NovaOrynDebugInspectorWidget.ID;
        this.title.label = NovaOrynDebugInspectorWidget.LABEL;
        this.title.caption = 'NovaOryn watches, call stack, locals/frame values and registers';
        this.title.closable = true;
        this.addClass('novaoryn-debug-inspector-widget');
        this.loadWatches();
        this.loadExceptionSettings();
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

    protected loadExceptionSettings(): void {
        try {
            const raw = window.localStorage.getItem(EXCEPTION_STORAGE_KEY);
            if (!raw) { return; }
            const parsed = JSON.parse(raw) as Partial<NovaOrynExceptionBreakpointSettings>;
            const vectors = Array.isArray(parsed.vectors) ? parsed.vectors.filter(value => Number.isInteger(value) && value >= 0 && value < 32) : DEFAULT_EXCEPTION_VECTORS;
            this.exceptionSettings = { vectors: Array.from(new Set(vectors)), breakOnPanic: parsed.breakOnPanic !== false };
        } catch { }
    }

    protected saveExceptionSettings(): void {
        try { window.localStorage.setItem(EXCEPTION_STORAGE_KEY, JSON.stringify(this.exceptionSettings)); } catch { }
    }

    getExceptionBreakpoints(): NovaOrynExceptionBreakpointSettings {
        return { vectors: [...this.exceptionSettings.vectors], breakOnPanic: this.exceptionSettings.breakOnPanic };
    }

    protected async toggleExceptionVector(vector: number): Promise<void> {
        const enabled = this.exceptionSettings.vectors.includes(vector);
        this.exceptionSettings = {
            ...this.exceptionSettings,
            vectors: enabled ? this.exceptionSettings.vectors.filter(value => value !== vector) : [...this.exceptionSettings.vectors, vector].sort((a, b) => a - b)
        };
        this.saveExceptionSettings();
        await this.applyExceptionSettings();
    }

    protected async togglePanic(): Promise<void> {
        this.exceptionSettings = { ...this.exceptionSettings, breakOnPanic: !this.exceptionSettings.breakOnPanic };
        this.saveExceptionSettings();
        await this.applyExceptionSettings();
    }

    protected async applyExceptionSettings(): Promise<void> {
        this.update();
        if (!this.sessionId || !this.state.paused) { return; }
        this.state = await this.projectService.configureExceptionBreakpoints(this.sessionId, this.getExceptionBreakpoints());
        this.update();
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

    protected renderExceptionSection(): React.ReactNode {
        const items = [
            [0, 'Divide by zero'], [2, 'NMI'], [6, 'Invalid opcode'], [8, 'Double fault'],
            [12, 'Stack fault'], [13, 'General protection'], [14, 'Page fault'], [18, 'Machine check']
        ] as const;
        return <section>
            <h3>Exception / Panic Breakpoints</h3>
            <p className='novaoryn-debug-note'>Enabled breakpoints are armed before KMain. While a session is active, pause the kernel before changing them.</p>
            <div className='novaoryn-exception-grid'>
                {items.map(([vector, label]) => <label className='novaoryn-debug-check' key={vector}>
                    <input type='checkbox' checked={this.exceptionSettings.vectors.includes(vector)} disabled={!!this.sessionId && !this.state.paused} onChange={() => { void this.toggleExceptionVector(vector); }} />
                    <span>{label} <code>#{vector}</code></span>
                </label>)}
                <label className='novaoryn-debug-check'>
                    <input type='checkbox' checked={this.exceptionSettings.breakOnPanic} disabled={!!this.sessionId && !this.state.paused} onChange={() => { void this.togglePanic(); }} />
                    <span>Fatal / panic halt</span>
                </label>
            </div>
            {this.state.exceptionName && <div className='novaoryn-exception-hit'><strong>Stopped:</strong> {this.state.exceptionName}{this.state.exceptionVector !== undefined ? ` (vector ${this.state.exceptionVector})` : ''}</div>}
        </section>;
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
            return <div className='novaoryn-debug-inspector'><div className='novaoryn-debug-inspector-empty'>Start a NovaOryn Debug session to inspect the kernel.</div>{this.renderExceptionSection()}</div>;
        }
        if (!this.state.paused) {
            return <div className='novaoryn-debug-inspector'>
                <div className='novaoryn-debug-inspector-empty'>Kernel running. Pause or stop at a breakpoint to inspect state.</div>
                {this.renderExceptionSection()}
                {this.renderWatchSection()}
            </div>;
        }

        return <div className='novaoryn-debug-inspector'>
            {this.renderExceptionSection()}
            {this.renderWatchSection()}
            <section>
                <h3>Mixed C# / x64 Disassembly</h3>
                <p className='novaoryn-debug-note'>Runtime addresses include the EFI relocation. Source labels come from the exact NativeAOT sequence-point map.</p>
                <div className='novaoryn-disassembly'>
                    {(this.state.disassembly ?? []).map((instruction, index) => <div className={`novaoryn-disassembly-row ${instruction.current ? 'current' : ''}`} key={`${instruction.runtimeAddress}:${index}`}>
                        <code className='novaoryn-disassembly-address'>{instruction.runtimeAddress}</code>
                        <code className='novaoryn-disassembly-instruction'>{instruction.instruction}</code>
                        <span className='novaoryn-disassembly-source'>{instruction.sourcePath && instruction.line ? `${instruction.sourcePath.split(/[\\/]/).pop()}:${instruction.line}` : ''}</span>
                    </div>)}
                    {(this.state.disassembly ?? []).length === 0 && <div className='novaoryn-debug-note'>Disassembly is unavailable for this stop.</div>}
                </div>
            </section>
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
