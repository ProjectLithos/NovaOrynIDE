import { inject, injectable, postConstruct } from 'inversify';
import { Emitter, Event, MessageService, URI } from '@theia/core/lib/common';
import { OutputChannelManager } from '@theia/output/lib/browser/output-channel';
import { BreakpointManager as TheiaBreakpointManager } from '@theia/debug/lib/browser/breakpoint/breakpoint-manager';
import { SourceBreakpoint } from '@theia/debug/lib/browser/breakpoint/breakpoint-marker';
import { NovaOrynBreakpointResult, NovaOrynProjectService } from '../common/novaoryn-protocol';

const OUTPUT_CHANNEL_NAME = 'NovaOryn Build';

export interface NovaOrynSourceBreakpoint {
    sourcePath: string;
    line: number;
    verified: boolean;
}

/**
 * Bridges Theia's authoritative source-breakpoint UI to the NovaOryn/QEMU
 * debugger backend. Theia owns gutter dots, F9, persistence and breakpoint
 * editor behaviour; NovaOryn owns native-address resolution and GDB arming.
 */
@injectable()
export class NovaOrynBreakpointManager {
    @inject(NovaOrynProjectService)
    protected readonly projectService!: NovaOrynProjectService;

    @inject(MessageService)
    protected readonly messageService!: MessageService;

    @inject(OutputChannelManager)
    protected readonly outputChannelManager!: OutputChannelManager;

    @inject(TheiaBreakpointManager)
    protected readonly theiaBreakpoints!: TheiaBreakpointManager;

    protected readonly verification = new Map<string, boolean>();
    protected readonly changeEmitter = new Emitter<void>();
    readonly onDidChange: Event<void> = this.changeEmitter.event;
    protected sessionId: string | undefined;
    protected contextLocation: { sourcePath: string; line: number } | undefined;
    protected suppressNativeMirror = false;

    @postConstruct()
    protected init(): void {
        this.theiaBreakpoints.onDidChangeBreakpoints(event => {
            this.changeEmitter.fire(undefined);
            if (!this.sessionId || this.suppressNativeMirror) {
                return;
            }
            // The native editor UI can add/remove breakpoints directly (gutter/F9).
            // Mirror those changes into the live NovaOryn debugger session.
            for (const breakpoint of event.added) {
                void this.armRuntimeBreakpoint(breakpoint.uri.path.fsPath(), breakpoint.line);
            }
            for (const breakpoint of event.removed) {
                void this.armRuntimeBreakpoint(breakpoint.uri.path.fsPath(), breakpoint.line);
            }
        });
    }

    setSession(sessionId: string | undefined): void {
        this.sessionId = sessionId;
        if (!sessionId) {
            this.verification.clear();
            this.changeEmitter.fire(undefined);
        }
    }

    getSession(): string | undefined {
        return this.sessionId;
    }

    setContextLocation(sourcePath: string, line: number): void {
        if (line > 0) {
            this.contextLocation = { sourcePath, line };
        }
    }

    consumeContextLocation(): { sourcePath: string; line: number } | undefined {
        const location = this.contextLocation;
        this.contextLocation = undefined;
        return location;
    }

    all(): NovaOrynSourceBreakpoint[] {
        return this.theiaBreakpoints.getBreakpoints().map(breakpoint => ({
            sourcePath: breakpoint.uri.path.fsPath(),
            line: breakpoint.line,
            verified: this.verification.get(this.key(breakpoint.uri.path.fsPath(), breakpoint.line)) ?? false
        }));
    }

    has(sourcePath: string, line: number): boolean {
        const uri = new URI(sourcePath);
        return this.theiaBreakpoints.getLineBreakpoints(uri, line).length > 0;
    }

    forSource(sourcePath: string): NovaOrynSourceBreakpoint[] {
        const normalized = this.normalize(sourcePath);
        return this.all().filter(item => this.normalize(item.sourcePath) === normalized);
    }

    async toggle(sourcePath: string, line: number): Promise<NovaOrynBreakpointResult> {
        const uri = new URI(sourcePath);
        const existing = this.theiaBreakpoints.getLineBreakpoints(uri, line);

        if (existing.length) {
            this.suppressNativeMirror = true;
            try {
                for (const breakpoint of existing) {
                    this.theiaBreakpoints.removeBreakpoint(breakpoint);
                }
            } finally {
                this.suppressNativeMirror = false;
            }
            this.verification.delete(this.key(sourcePath, line));
            if (!this.sessionId) {
                return { success: true, verified: false, sourcePath, line, message: 'Breakpoint removed.' };
            }
            return this.armRuntimeBreakpoint(sourcePath, line);
        }

        this.suppressNativeMirror = true;
        try {
            this.theiaBreakpoints.addBreakpoint(SourceBreakpoint.create(uri, { line }));
        } finally {
            this.suppressNativeMirror = false;
        }
        if (!this.sessionId) {
            return { success: true, verified: false, sourcePath, line, message: 'Breakpoint set. It will be armed when Debug starts.' };
        }
        return this.armRuntimeBreakpoint(sourcePath, line);
    }

    applyRuntimeBreakpoints(results: NovaOrynBreakpointResult[] | undefined): void {
        if (!results) {
            return;
        }
        for (const result of results) {
            this.verification.set(this.key(result.sourcePath, result.line), result.verified);
        }
        this.changeEmitter.fire(undefined);
    }

    async armAll(sessionId: string): Promise<void> {
        this.setSession(sessionId);
        this.suppressNativeMirror = true;
        try {
            for (const breakpoint of this.theiaBreakpoints.getBreakpoints()) {
                await this.armRuntimeBreakpoint(breakpoint.uri.path.fsPath(), breakpoint.line);
            }
        } finally {
            this.suppressNativeMirror = false;
        }
        this.changeEmitter.fire(undefined);
    }

    protected async armRuntimeBreakpoint(sourcePath: string, line: number): Promise<NovaOrynBreakpointResult> {
        if (!this.sessionId) {
            return { success: true, verified: false, sourcePath, line, message: 'Breakpoint stored.' };
        }
        const result = await this.projectService.toggleBreakpoint(this.sessionId, sourcePath, line);
        this.verification.set(this.key(sourcePath, line), result.verified);
        this.log(result);
        this.changeEmitter.fire(undefined);
        return result;
    }

    protected log(result: NovaOrynBreakpointResult): void {
        const channel = this.outputChannelManager.getChannel(OUTPUT_CHANNEL_NAME);
        channel.appendLine(`[DEBUG] ${result.sourcePath}:${result.line}: ${result.message ?? (result.verified ? 'Breakpoint verified.' : 'Breakpoint pending.')}`);
    }

    protected key(sourcePath: string, line: number): string {
        return `${this.normalize(sourcePath)}:${line}`;
    }

    protected normalize(sourcePath: string): string {
        return sourcePath.replace(/\\/g, '/').toLowerCase();
    }
}
