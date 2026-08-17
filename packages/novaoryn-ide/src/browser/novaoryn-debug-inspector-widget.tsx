import * as React from 'react';
import { inject, injectable, postConstruct } from 'inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { FileUri } from '@theia/core/lib/common/file-uri';
import { EditorManager } from '@theia/editor/lib/browser';
import { NovaOrynCrashDumpResult, NovaOrynDebugState, NovaOrynExceptionBreakpointSettings, NovaOrynExpressionResult, NovaOrynHeapSnapshot, NovaOrynMemoryReadResult, NovaOrynPageTableInspection, NovaOrynProjectService } from '../common/novaoryn-protocol';

const WATCH_STORAGE_KEY = 'novaoryn.ide.watchExpressions';
const EXCEPTION_STORAGE_KEY = 'novaoryn.ide.exceptionBreakpoints';
const MEMORY_STORAGE_KEY = 'novaoryn.ide.memoryViewer';
const PAGE_TABLE_STORAGE_KEY = 'novaoryn.ide.pageTableViewer';
const CRASH_DUMP_STORAGE_KEY = 'novaoryn.ide.lastCrashDump';
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
    protected memoryAddressDraft = 'rsp';
    protected memoryLength = 128;
    protected memoryResult: NovaOrynMemoryReadResult | undefined;
    protected pageTableAddressDraft = 'rip';
    protected pageTableResult: NovaOrynPageTableInspection | undefined;
    protected heapResult: NovaOrynHeapSnapshot | undefined;
    protected crashDumpResult: NovaOrynCrashDumpResult | undefined;
    protected crashDumpPathDraft = '';
    protected offlineDump = false;


    @postConstruct()
    protected init(): void {
        this.id = NovaOrynDebugInspectorWidget.ID;
        this.title.label = NovaOrynDebugInspectorWidget.LABEL;
        this.title.caption = 'NovaOryn breakpoints, watches, memory, page tables, heap, crash dumps, CPU/thread/process contexts, x64 unwind call stack, named locals, disassembly and registers';
        this.title.closable = true;
        this.addClass('novaoryn-debug-inspector-widget');
        this.loadWatches();
        this.loadExceptionSettings();
        this.loadMemorySettings();
        this.loadAdvancedDebugSettings();
        this.update();
    }

    setSession(sessionId: string | undefined): void {
        this.sessionId = sessionId;
        if (sessionId) { this.offlineDump = false; }
        if (!sessionId && !this.offlineDump) {
            this.watchResults.clear();
            this.memoryResult = undefined;
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
            void this.refreshWatches().then(() => this.refreshMemory());
        } else if (!state.paused) {
            this.lastPauseKey = '';
        }
    }

    protected async selectExecutionContext(threadId: string): Promise<void> {
        if (!this.sessionId || !this.state.paused) { return; }
        this.state = await this.projectService.selectExecutionContext(this.sessionId, threadId);
        this.update();
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


    protected loadAdvancedDebugSettings(): void {
        try {
            const page = window.localStorage.getItem(PAGE_TABLE_STORAGE_KEY);
            if (page?.trim()) this.pageTableAddressDraft = page.trim();
            const dump = window.localStorage.getItem(CRASH_DUMP_STORAGE_KEY);
            if (dump?.trim()) this.crashDumpPathDraft = dump.trim();
        } catch { }
    }

    protected saveAdvancedDebugSettings(): void {
        try {
            window.localStorage.setItem(PAGE_TABLE_STORAGE_KEY, this.pageTableAddressDraft);
            if (this.crashDumpPathDraft.trim()) window.localStorage.setItem(CRASH_DUMP_STORAGE_KEY, this.crashDumpPathDraft.trim());
        } catch { }
    }

    protected async refreshPageTable(): Promise<void> {
        if (!this.sessionId || !this.state.paused || !this.pageTableAddressDraft.trim()) return;
        this.saveAdvancedDebugSettings();
        this.pageTableResult = await this.projectService.inspectPageTable(this.sessionId, this.pageTableAddressDraft.trim());
        this.update();
    }

    protected async refreshHeap(): Promise<void> {
        if (!this.sessionId || !this.state.paused) return;
        this.heapResult = await this.projectService.inspectHeap(this.sessionId);
        this.update();
    }

    protected async captureCrashDump(): Promise<void> {
        if (!this.sessionId || !this.state.paused) return;
        this.crashDumpResult = await this.projectService.captureCrashDump(this.sessionId, this.state.exceptionName ? `exception: ${this.state.exceptionName}` : 'manual debugger capture');
        if (this.crashDumpResult.dump?.path) {
            this.crashDumpPathDraft = this.crashDumpResult.dump.path;
            this.saveAdvancedDebugSettings();
        }
        this.update();
    }

    protected async loadCrashDump(): Promise<void> {
        const path = this.crashDumpPathDraft.trim();
        if (!path) return;
        const result = await this.projectService.loadCrashDump(path);
        this.crashDumpResult = result;
        if (result.success && result.state) {
            this.offlineDump = true;
            this.sessionId = undefined;
            this.state = result.state;
            this.pageTableResult = result.pageTable;
            this.heapResult = result.heap;
            this.memoryResult = result.memory?.stack;
            this.saveAdvancedDebugSettings();
        }
        this.update();
    }

    protected renderPageTableSection(): React.ReactNode {
        const result = this.pageTableResult;
        return <section>
            <h3>Page Tables — x64 Translation</h3>
            <div className='novaoryn-memory-controls'>
                <input className='theia-input' value={this.pageTableAddressDraft} disabled={!this.state.paused || this.offlineDump}
                    placeholder='rip, rsp, 0xffff800000001000'
                    onChange={event => { this.pageTableAddressDraft = event.target.value; this.update(); }}
                    onKeyDown={event => { if (event.key === 'Enter') void this.refreshPageTable(); }} />
                <button className='theia-button' disabled={!this.sessionId || !this.state.paused || !this.pageTableAddressDraft.trim()} onClick={() => void this.refreshPageTable()}>Translate</button>
            </div>
            <p className='novaoryn-debug-note'>Walks the active CR3 → PML4 → PDPT → PD → PT using QEMU physical-memory reads. Large 1 GiB and 2 MiB pages are recognized.</p>
            {result && <div className={`novaoryn-debug-value ${result.success ? '' : 'error'}`}>
                {result.success ? `${result.virtualAddress} → ${result.physicalAddress} (${result.pageSize}), CR3 ${result.cr3}` : result.error}
            </div>}
            <div className='novaoryn-page-table'>
                {(result?.entries ?? []).map(entry => <div className={`novaoryn-page-table-row ${entry.present ? '' : 'not-present'}`} key={`${entry.level}:${entry.index}`}>
                    <strong>{entry.level}[{entry.index}]</strong>
                    <code>{entry.entryPhysicalAddress}</code>
                    <code>{entry.entryValue}</code>
                    <span>{entry.present ? 'P' : 'NP'} {entry.writable ? 'W' : 'R'} {entry.user ? 'U' : 'S'} {entry.noExecute ? 'NX' : 'X'}{entry.largePage ? ' LARGE' : ''}{entry.global ? ' G' : ''}</span>
                    <code>{entry.targetPhysicalAddress ?? '—'}</code>
                </div>)}
            </div>
        </section>;
    }

    protected renderHeapSection(): React.ReactNode {
        const heap = this.heapResult;
        return <section>
            <h3>Kernel Heap</h3>
            <div className='novaoryn-debug-actions'>
                <button className='theia-button' disabled={!this.sessionId || !this.state.paused} onClick={() => void this.refreshHeap()}>Refresh Heap</button>
            </div>
            <p className='novaoryn-debug-note'>Reads KernelHeap's stable NovaOryn diagnostic ABI directly, including committed/allocated/free bytes and the authoritative live/free first-fit block table.</p>
            {heap && !heap.success && <div className='novaoryn-debug-value error'>{heap.error}</div>}
            {heap?.success && <>
                <div className='novaoryn-heap-summary'>
                    <span>Initialized <strong>{heap.initialized ? 'yes' : 'no'}</strong></span>
                    <span>Committed <strong>{heap.committedBytes?.toLocaleString()} B</strong></span>
                    <span>Allocated <strong>{heap.allocatedBytes?.toLocaleString()} B</strong></span>
                    <span>Free <strong>{heap.freeBytes?.toLocaleString()} B</strong></span>
                    <span>Peak <strong>{heap.peakAllocatedBytes?.toLocaleString()} B</strong></span>
                    <span>Live <strong>{heap.liveAllocations}</strong></span>
                </div>
                <div className='novaoryn-heap-blocks'>
                    {(heap.blocks ?? []).map(block => <div className={`novaoryn-heap-block ${block.state}`} key={block.index}>
                        <span>#{block.index} {block.state}</span><code>{block.address}</code><span>{block.byteCount.toLocaleString()} B</span><code>{block.token ?? ''}</code>
                    </div>)}
                </div>
            </>}
        </section>;
    }

    protected renderCrashDumpSection(): React.ReactNode {
        const result = this.crashDumpResult;
        return <section>
            <h3>Crash Dump Debugging</h3>
            <div className='novaoryn-crash-dump-actions'>
                <button className='theia-button' disabled={!this.sessionId || !this.state.paused} onClick={() => void this.captureCrashDump()}>Capture Dump</button>
                <input className='theia-input' value={this.crashDumpPathDraft} placeholder='C:\\NovaOrynOSes\\...\\.novaoryn\\crash-dumps\\*.nodump.json'
                    onChange={event => { this.crashDumpPathDraft = event.target.value; this.update(); }} />
                <button className='theia-button' disabled={!this.crashDumpPathDraft.trim()} onClick={() => void this.loadCrashDump()}>Open Dump</button>
            </div>
            <p className='novaoryn-debug-note'>Captures registers, x64 unwind stack, locals, disassembly, page-table translation, heap metadata, and code/stack memory into the OS project's <code>.novaoryn/crash-dumps</code> folder. Dumps can be reopened with QEMU stopped.</p>
            {this.offlineDump && result?.dump && <div className='novaoryn-crash-dump-open'><strong>Offline dump:</strong> {result.dump.createdUtc} — {result.dump.reason}<br/><code>{result.dump.path}</code></div>}
            {result && !result.success && <div className='novaoryn-debug-value error'>{result.error}</div>}
            {!this.offlineDump && result?.success && result.dump && <div className='novaoryn-crash-dump-open'><strong>Captured:</strong> <code>{result.dump.path}</code></div>}
        </section>;
    }

    protected loadMemorySettings(): void {
        try {
            const raw = window.localStorage.getItem(MEMORY_STORAGE_KEY);
            if (!raw) { return; }
            const parsed = JSON.parse(raw) as { address?: string; length?: number };
            if (typeof parsed.address === 'string' && parsed.address.trim()) { this.memoryAddressDraft = parsed.address.trim(); }
            if (typeof parsed.length === 'number' && Number.isFinite(parsed.length)) { this.memoryLength = Math.max(16, Math.min(1024, Math.trunc(parsed.length))); }
        } catch { }
    }

    protected saveMemorySettings(): void {
        try { window.localStorage.setItem(MEMORY_STORAGE_KEY, JSON.stringify({ address: this.memoryAddressDraft, length: this.memoryLength })); } catch { }
    }

    protected async refreshMemory(): Promise<void> {
        if (!this.sessionId || !this.state.active || !this.state.paused || !this.memoryAddressDraft.trim()) { return; }
        this.saveMemorySettings();
        this.memoryResult = await this.projectService.readMemoryRange(this.sessionId, this.memoryAddressDraft.trim(), this.memoryLength);
        this.update();
    }

    protected renderMemorySection(): React.ReactNode {
        const rows: React.ReactNode[] = [];
        const result = this.memoryResult;
        if (result?.success && result.bytes && result.address) {
            const bytes = result.bytes.match(/../g) ?? [];
            const base = BigInt(result.address);
            for (let offset = 0; offset < bytes.length; offset += 16) {
                const slice = bytes.slice(offset, offset + 16);
                const ascii = slice.map(value => {
                    const n = Number.parseInt(value, 16);
                    return n >= 32 && n <= 126 ? String.fromCharCode(n) : '.';
                }).join('');
                rows.push(<div className='novaoryn-memory-row' key={offset}>
                    <code className='novaoryn-memory-address'>0x{(base + BigInt(offset)).toString(16).padStart(16, '0')}</code>
                    <code className='novaoryn-memory-hex'>{slice.join(' ')}</code>
                    <code className='novaoryn-memory-ascii'>{ascii}</code>
                </div>);
            }
        }
        return <section>
            <h3>Memory</h3>
            <div className='novaoryn-memory-controls'>
                <input className='theia-input' value={this.memoryAddressDraft} disabled={!this.state.paused}
                    placeholder='rsp, rbp-0x40, 0x100000'
                    title='Address expressions use the same register/arithmetic syntax as Watch.'
                    onChange={event => { this.memoryAddressDraft = event.target.value; this.update(); }}
                    onKeyDown={event => { if (event.key === 'Enter') { void this.refreshMemory(); } }} />
                <select className='theia-select' value={this.memoryLength} disabled={!this.state.paused}
                    onChange={event => { this.memoryLength = Number(event.target.value); this.saveMemorySettings(); this.update(); }}>
                    {[64, 128, 256, 512, 1024].map(length => <option value={length} key={length}>{length} bytes</option>)}
                </select>
                <button className='theia-button' disabled={!this.state.paused || !this.memoryAddressDraft.trim()} onClick={() => { void this.refreshMemory(); }}>Read</button>
            </div>
            <p className='novaoryn-debug-note'>Reads guest virtual memory through QEMU's GDB stub. Addresses may be literals or expressions such as <code>rsp+0x20</code>.</p>
            {result && !result.success && <div className='novaoryn-debug-value error'>{result.error}</div>}
            {result?.success && <div className='novaoryn-memory-view'>{rows}</div>}
        </section>;
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
            return <div className='novaoryn-debug-inspector'><div className='novaoryn-debug-inspector-empty'>Start a NovaOryn Debug session to inspect the kernel, or open a captured dump below.</div>{this.renderExceptionSection()}{this.renderCrashDumpSection()}</div>;
        }
        if (!this.state.paused) {
            return <div className='novaoryn-debug-inspector'>
                <div className='novaoryn-debug-inspector-empty'>Kernel running. Pause or stop at a breakpoint to inspect state.</div>
                {this.renderExceptionSection()}
                {this.renderWatchSection()}
                {this.renderCrashDumpSection()}
            </div>;
        }

        return <div className='novaoryn-debug-inspector'>
            {this.renderExceptionSection()}
            {this.renderWatchSection()}
            {this.renderMemorySection()}
            {this.renderPageTableSection()}
            {this.renderHeapSection()}
            {this.renderCrashDumpSection()}
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
                <h3>CPUs / Threads / Process Contexts</h3>
                <p className='novaoryn-debug-note'>QEMU exposes each virtual CPU as a GDB execution thread. Selecting one switches register, locals, memory-expression and call-stack inspection to that CPU. A process/inferior id is shown when QEMU uses multiprocess thread ids.</p>
                <div className='novaoryn-debug-table'>
                    {(this.state.executionContexts ?? []).map(context => <div
                        className={`novaoryn-debug-row novaoryn-execution-context ${context.current ? 'current' : ''}`}
                        key={context.id}
                        title='Inspect this CPU/thread context'
                        onClick={() => this.selectExecutionContext(context.id)}
                    >
                        <span className='novaoryn-debug-name'>{context.cpuIndex !== undefined ? `CPU ${context.cpuIndex}` : 'CPU'}{context.current ? ' • selected' : ''}</span>
                        <span className='novaoryn-debug-value'>{context.name}</span>
                        <code className='novaoryn-debug-address'>thread {context.threadId}{context.processId ? ` / process ${context.processId}` : ''}</code>
                    </div>)}
                    {(this.state.executionContexts ?? []).length === 0 && <div className='novaoryn-debug-note'>QEMU did not report execution-thread metadata for this stop.</div>}
                </div>
            </section>
            <section>
                <h3>Call Stack — x64 Unwind</h3>
                <p className='novaoryn-debug-note'>Frames are reconstructed from the PE/COFF x64 exception/unwind table. Arbitrary stack-word scanning is no longer used.</p>
                <div className='novaoryn-debug-table'>
                    {(this.state.callStack ?? []).map(frame => <div
                        className={`novaoryn-debug-row ${frame.sourcePath && frame.line ? 'clickable' : ''}`}
                        key={`${frame.index}:${frame.address}`}
                        title={frame.sourcePath && frame.line ? 'Open this stack frame source location' : undefined}
                        onClick={() => this.openFrame(frame.sourcePath, frame.line)}
                    >
                        <span className='novaoryn-debug-name'>#{frame.index} <small>{frame.kind ?? 'native'} / {frame.unwoundBy ?? 'x64-unwind'}</small></span>
                        <span className='novaoryn-debug-value'>{frame.label}</span>
                        <span className='novaoryn-debug-address'>{frame.address}</span>
                    </div>)}
                </div>
            </section>
            <section>
                <h3>Locals / Native Frame</h3>
                {this.state.localsMessage && <p className='novaoryn-debug-note'>{this.state.localsMessage}</p>}
                <div className='novaoryn-debug-table'>
                    {(this.state.locals ?? []).map(variable => <div className='novaoryn-debug-row novaoryn-local-row' key={`${variable.kind}:${variable.name}`}>
                        <span className='novaoryn-debug-name'><strong>{variable.kind === 'argument' ? 'arg' : variable.kind}</strong> {variable.name}{variable.typeName ? <small> : {variable.typeName}</small> : null}</span>
                        <span className='novaoryn-debug-value'>{variable.value}</span>
                        <code className='novaoryn-debug-address'>{variable.location ?? ''}</code>
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
