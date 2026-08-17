import { injectable } from 'inversify';
import * as fs from 'fs/promises';
import * as path from 'path';
import { ChildProcess, spawn } from 'child_process';
import * as net from 'net';
import * as os from 'os';
import { pathToFileURL } from 'url';
import {
    NOVAORYN_OS_ROOT,
    NovaOrynOperatingSystem,
    NovaOrynConfigurationResult,
    NovaOrynProjectConfiguration,
    NovaOrynProjectResult,
    NovaOrynRunMode,
    NovaOrynDebugCommand,
    NovaOrynDebugState,
    NovaOrynDebugFrame,
    NovaOrynDebugRegister,
    NovaOrynDebugVariable,
    NovaOrynBreakpointResult,
    NovaOrynRunOutput,
    NovaOrynRunResult,
    NovaOrynProjectService
} from '../common/novaoryn-protocol';

const NOVAORYN_IDE_ROOT = process.env.NOVAORYN_IDE_ROOT
    ? path.resolve(process.env.NOVAORYN_IDE_ROOT)
    : path.resolve(__dirname, '..', '..', '..', '..');
const NOVAORYN_SDK_ROOT = path.join(NOVAORYN_IDE_ROOT, 'SDK');
const NOVAORYN_IDE_VERSION = '0.1.40';

class GdbRspClient {
    protected socket: net.Socket | undefined;
    protected buffer = Buffer.alloc(0);
    protected pending: { resolve: (value: string) => void; reject: (error: Error) => void; timer: NodeJS.Timeout } | undefined;

    constructor(protected readonly onStop: (packet: string) => void) {}

    async connect(port: number, timeoutMs = 15000): Promise<void> {
        const until = Date.now() + timeoutMs;
        let lastError: Error | undefined;
        while (Date.now() < until) {
            try {
                const socket = await new Promise<net.Socket>((resolve, reject) => {
                    const candidate = net.createConnection({ host: '127.0.0.1', port });
                    const fail = (error: Error) => {
                        candidate.destroy();
                        reject(error);
                    };
                    candidate.once('error', fail);
                    candidate.once('connect', () => {
                        candidate.removeListener('error', fail);
                        resolve(candidate);
                    });
                });
                socket.setNoDelay(true);
                socket.on('data', data => this.onData(data));
                socket.on('error', error => {
                    if (this.pending) {
                        const pending = this.pending;
                        this.pending = undefined;
                        clearTimeout(pending.timer);
                        pending.reject(error);
                    }
                });
                this.socket = socket;
                return;
            } catch (error) {
                lastError = error instanceof Error ? error : new Error(String(error));
                await new Promise(resolve => setTimeout(resolve, 100));
            }
        }
        throw new Error(`QEMU GDB endpoint 127.0.0.1:${port} did not accept the debugger connection within ${timeoutMs}ms${lastError ? `: ${lastError.message}` : '.'}`);
    }

    close(): void {
        this.socket?.destroy();
        this.socket = undefined;
    }

    async command(command: string): Promise<string> {
        if (!this.socket) {
            throw new Error('NovaOryn debugger is not connected to QEMU.');
        }
        if (this.pending) {
            throw new Error('NovaOryn debugger already has a pending GDB command.');
        }
        return new Promise<string>((resolve, reject) => {
            const timer = setTimeout(() => {
                if (this.pending) {
                    this.pending = undefined;
                    reject(new Error(`GDB command timed out: ${command}`));
                }
            }, 2500);
            this.pending = { resolve, reject, timer };
            this.socket!.write(this.packet(command));
        });
    }

    run(command: string): void {
        if (!this.socket) {
            throw new Error('NovaOryn debugger is not connected to QEMU.');
        }
        this.socket.write(this.packet(command));
    }

    interrupt(): void {
        if (!this.socket) {
            throw new Error('NovaOryn debugger is not connected to QEMU.');
        }
        this.socket.write(Buffer.from([0x03]));
    }

    protected packet(payload: string): string {
        let checksum = 0;
        for (const byte of Buffer.from(payload, 'ascii')) {
            checksum = (checksum + byte) & 0xff;
        }
        return `$${payload}#${checksum.toString(16).padStart(2, '0')}`;
    }

    protected onData(data: Buffer): void {
        this.buffer = Buffer.concat([this.buffer, data]);
        while (this.buffer.length > 0) {
            if (this.buffer[0] === 0x2b || this.buffer[0] === 0x2d) {
                this.buffer = this.buffer.subarray(1);
                continue;
            }
            const start = this.buffer.indexOf(0x24);
            if (start < 0) {
                this.buffer = Buffer.alloc(0);
                return;
            }
            const hash = this.buffer.indexOf(0x23, start + 1);
            if (hash < 0 || this.buffer.length < hash + 3) {
                return;
            }
            const payload = this.buffer.subarray(start + 1, hash).toString('ascii');
            this.buffer = this.buffer.subarray(hash + 3);
            this.socket?.write('+');

            if (/^[TSWX]/.test(payload)) {
                this.onStop(payload);
                continue;
            }
            if (this.pending) {
                const pending = this.pending;
                this.pending = undefined;
                clearTimeout(pending.timer);
                pending.resolve(payload);
            }
        }
    }
}

interface SourceBreakpoint {
    sourcePath: string;
    line: number;
    resolvedLine: number;
    address: string;
}

interface ResolvedSourceAddress {
    linkedAddress: bigint;
    resolvedLine: number;
    exactLine: boolean;
}

interface NativeSourceLine {
    sourcePath: string;
    line: number;
    linkedAddress: string;
}

interface NativeDebugMap {
    anchor: { symbol: string; linkedAddress: string; resumeSymbol?: string; resumeLinkedAddress?: string; transport?: string };
    entries: NativeSourceLine[];
}

interface StepPlan {
    kind: 'step-into' | 'step-over' | 'step-out';
    sourcePath?: string;
    line?: number;
    machineSteps: number;
    temporaryAddress?: bigint;
}

interface RunSession {
    output: string;
    complete: boolean;
    exitCode?: number;
    error?: string;
    mode: NovaOrynRunMode;
    projectRoot: string;
    qemu?: ChildProcess;
    gdb?: GdbRspClient;
    debug?: NovaOrynDebugState;
    breakpoints: Map<string, SourceBreakpoint>;
    requestedBreakpoints: Array<{ sourcePath: string; line: number }>;
    breakpointResults: NovaOrynBreakpointResult[];
    nativeDebugMap?: NativeDebugMap;
    relocationDelta?: bigint;
    preparingAnchor?: boolean;
    anchorStopResolve?: () => void;
    internalPause?: boolean;
    lastBreakpoint?: SourceBreakpoint;
    stepPlan?: StepPlan;
}

interface GeneratedProject {
    id: string;
    relativePath: string;
    kind: 'kernel' | 'kernel-module' | 'service' | 'driver' | 'userland' | 'test';
    description: string;
}

@injectable()
export class NovaOrynProjectServiceImpl implements NovaOrynProjectService {
    protected readonly runSessions = new Map<string, RunSession>();

    async listOperatingSystems(): Promise<NovaOrynOperatingSystem[]> {
        await fs.mkdir(NOVAORYN_OS_ROOT, { recursive: true });
        const entries = await fs.readdir(NOVAORYN_OS_ROOT, { withFileTypes: true });
        const systems: NovaOrynOperatingSystem[] = [];
        for (const entry of entries) {
            if (!entry.isDirectory()) {
                continue;
            }
            const osPath = path.join(NOVAORYN_OS_ROOT, entry.name);
            try {
                await fs.access(path.join(osPath, 'NovaOryn.json'));
                await this.refreshSdkBridge(osPath);
                systems.push({ name: entry.name, path: osPath, uri: pathToFileURL(osPath).toString() });
            } catch {
                // Only folders containing a NovaOryn OS configuration are offered.
            }
        }
        return systems.sort((a, b) => a.name.localeCompare(b.name));
    }


    async runOperatingSystem(projectPath: string, mode: NovaOrynRunMode, breakpoints: Array<{ sourcePath: string; line: number }> = []): Promise<NovaOrynRunResult> {
        try {
            const projectRoot = path.resolve(projectPath);
            const osRoot = path.resolve(NOVAORYN_OS_ROOT);
            const relative = path.relative(osRoot, projectRoot);
            if (!relative || relative.startsWith('..') || path.isAbsolute(relative)) {
                return { success: false, error: `NovaOryn Run only accepts operating systems beneath ${NOVAORYN_OS_ROOT}.` };
            }

            await fs.access(path.join(projectRoot, 'NovaOryn.json'));
            await this.refreshSdkBridge(projectRoot);

            const runPath = path.join(projectRoot, 'Run.bat');
            await fs.access(runPath);

            const modeArgument = mode === 'debug' ? 'Debug' : 'Run';
            const sessionId = `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;
            const session: RunSession = {
                output: '', complete: false, mode, projectRoot,
                breakpoints: new Map<string, SourceBreakpoint>(),
                requestedBreakpoints: breakpoints.map(item => ({ sourcePath: path.resolve(item.sourcePath), line: item.line })),
                breakpointResults: [],
                debug: mode === 'debug' ? { active: false, paused: false, sourceSymbols: false, message: 'Building Debug kernel…' } : undefined
            };
            this.runSessions.set(sessionId, session);

            const child = spawn('cmd.exe', ['/d', '/c', 'call', runPath, modeArgument], {
                cwd: projectRoot,
                detached: false,
                windowsHide: true,
                stdio: ['ignore', 'pipe', 'pipe']
            });

            child.stdout?.setEncoding('utf8');
            child.stderr?.setEncoding('utf8');
            child.stdout?.on('data', data => { session.output += data; });
            child.stderr?.on('data', data => { session.output += data; });
            child.on('error', error => {
                session.error = error.message;
                session.output += `\r\n[FAIL] ${error.message}\r\n`;
                session.complete = true;
            });
            child.on('close', code => {
                if (session.complete) {
                    return;
                }
                if (mode === 'debug' && code === 0) {
                    void this.launchDebugQemu(session).catch(error => {
                        session.error = error instanceof Error ? error.message : String(error);
                        session.output += `\r\n[FAIL] ${session.error}\r\n`;
                        session.complete = true;
                        session.exitCode = 1;
                    });
                    return;
                }
                session.exitCode = code ?? -1;
                session.complete = true;
            });

            return { success: true, sessionId };
        } catch (error) {
            const message = error instanceof Error ? error.message : String(error);
            return { success: false, error: message };
        }
    }

    async readRunOutput(sessionId: string, offset: number): Promise<NovaOrynRunOutput> {
        const session = this.runSessions.get(sessionId);
        if (!session) {
            return {
                text: '',
                nextOffset: offset,
                complete: true,
                exitCode: -1,
                error: 'NovaOryn run session was not found.'
            };
        }

        const safeOffset = Math.max(0, Math.min(offset, session.output.length));
        const text = session.output.slice(safeOffset);
        const nextOffset = session.output.length;
        const result: NovaOrynRunOutput = {
            text,
            nextOffset,
            complete: session.complete,
            exitCode: session.exitCode,
            error: session.error
        };

        if (session.complete && nextOffset === session.output.length) {
            setTimeout(() => this.runSessions.delete(sessionId), 60_000);
        }
        return result;
    }

    async debugState(sessionId: string): Promise<NovaOrynDebugState> {
        const session = this.runSessions.get(sessionId);
        return session?.debug
            ? { ...session.debug, breakpoints: session.breakpointResults.map(item => ({ ...item })) }
            : { active: false, paused: false, sourceSymbols: false, message: 'No active NovaOryn debug session.' };
    }

    async debugCommand(sessionId: string, command: NovaOrynDebugCommand): Promise<NovaOrynDebugState> {
        const session = this.runSessions.get(sessionId);
        if (!session || session.mode !== 'debug' || !session.debug) {
            return { active: false, paused: false, sourceSymbols: false, message: 'No active NovaOryn debug session.' };
        }
        if (command === 'stop') {
            session.stepPlan = undefined;
            session.gdb?.close();
            session.qemu?.kill();
            session.debug = { ...session.debug, active: false, paused: false, message: 'Debug session stopped.' };
            session.complete = true;
            session.exitCode = 0;
            return session.debug;
        }
        if (command === 'restart') {
            session.gdb?.close();
            session.qemu?.kill();
            session.breakpoints.clear();
            session.lastBreakpoint = undefined;
            session.stepPlan = undefined;
            session.debug = { active: false, paused: false, sourceSymbols: session.debug.sourceSymbols, message: 'Restarting QEMU debugger…' };
            await this.launchDebugQemu(session);
            return session.debug!;
        }
        if (!session.gdb || !session.debug.active) {
            return { ...session.debug, message: 'QEMU debugger is not attached yet.' };
        }
        if (command === 'pause') {
            session.gdb.interrupt();
            session.debug = { ...session.debug, message: 'Pause requested…' };
            return session.debug;
        }
        if (command === 'continue') {
            await this.clearTemporaryStepBreakpoint(session);
            session.stepPlan = undefined;
            session.gdb.run('c');
            session.debug = this.runningDebugState(session, 'Kernel running. Waiting for breakpoint.');
            return session.debug;
        }
        if (!session.debug.paused) {
            return { ...session.debug, message: 'Step commands are available after a breakpoint or Pause.' };
        }

        if (command === 'step-out') {
            const rbp = await this.readRegister(session.gdb, 6);
            const returnAddress = rbp !== 0n ? await this.readU64(session.gdb, rbp + 8n) : 0n;
            if (returnAddress === 0n) {
                return { ...session.debug, message: 'Step Out could not determine the current frame return address.' };
            }
            const reply = await session.gdb.command(`Z0,${returnAddress.toString(16)},1`);
            if (reply !== 'OK') {
                return { ...session.debug, message: `Step Out temporary breakpoint was rejected by QEMU: ${reply}` };
            }
            session.stepPlan = { kind: 'step-out', sourcePath: session.debug.sourcePath, line: session.debug.line, machineSteps: 0, temporaryAddress: returnAddress };
            session.gdb.run('c');
            session.debug = this.runningDebugState(session, 'Step Out running to the caller…');
            return session.debug;
        }

        session.stepPlan = {
            kind: command,
            sourcePath: session.debug.sourcePath,
            line: session.debug.line,
            machineSteps: 0
        };
        await this.advanceStepPlan(session);
        return session.debug!;
    }

    async toggleBreakpoint(sessionId: string, sourcePath: string, line: number): Promise<NovaOrynBreakpointResult> {
        const session = this.runSessions.get(sessionId);
        if (!session || session.mode !== 'debug' || !session.gdb || !session.debug?.active) {
            return { success: false, verified: false, sourcePath, line, message: 'The debugger is still preparing the kernel image. Try again when the toolbar shows Running.' };
        }

        const normalizedSource = path.resolve(sourcePath);
        const key = `${normalizedSource.toLowerCase()}:${line}`;
        const existing = session.breakpoints.get(key);
        const pendingResult = session.breakpointResults.find(item =>
            path.resolve(item.sourcePath).toLowerCase() === normalizedSource.toLowerCase() && item.line === line);
        const wasPaused = session.debug.paused;
        if (!wasPaused) {
            session.internalPause = true;
            session.gdb.interrupt();
            await this.waitForPause(session, 1200);
        }

        try {
            // A stored breakpoint that failed to bind is not present in session.breakpoints.
            // When Theia removes that pending breakpoint, treat this call as removal rather
            // than accidentally attempting to arm it again.
            if (!existing && pendingResult && !pendingResult.verified) {
                session.breakpointResults = session.breakpointResults.filter(item =>
                    !(path.resolve(item.sourcePath).toLowerCase() === normalizedSource.toLowerCase() && item.line === line));
                if (!wasPaused) {
                    session.gdb.run('c');
                    session.debug = { ...session.debug!, paused: false, sourcePath: undefined, line: undefined, message: 'Kernel running. Waiting for breakpoint.' };
                }
                return { success: true, verified: false, sourcePath, line, message: 'Unverified breakpoint removed.' };
            }

            if (existing) {
                const reply = await session.gdb.command(`z0,${existing.address},1`);
                session.breakpoints.delete(key);
                session.breakpointResults = session.breakpointResults.filter(item =>
                    !(path.resolve(item.sourcePath).toLowerCase() === normalizedSource.toLowerCase() && item.line === line));
                if (!wasPaused) {
                    session.gdb.run('c');
                    session.debug = { ...session.debug!, paused: false, sourcePath: undefined, line: undefined, message: 'Kernel running. Waiting for breakpoint.' };
                }
                return { success: reply === 'OK', verified: false, sourcePath, line, address: existing.address, message: 'Breakpoint removed.' };
            }

            const result = await this.armSourceBreakpoint(session, normalizedSource, line);
            session.breakpointResults = session.breakpointResults.filter(item =>
                !(path.resolve(item.sourcePath).toLowerCase() === normalizedSource.toLowerCase() && item.line === line));
            session.breakpointResults.push(result);
            if (!wasPaused) {
                session.gdb.run('c');
                session.debug = { ...session.debug!, paused: false, sourcePath: undefined, line: undefined, message: 'Kernel running. Waiting for breakpoint.' };
            }
            return result;
        } catch (error) {
            if (!wasPaused && session.debug?.active) {
                try { session.gdb.run('c'); } catch { }
            }
            return { success: false, verified: false, sourcePath, line, message: error instanceof Error ? error.message : String(error) };
        } finally {
            session.internalPause = false;
        }
    }

    protected async launchDebugQemu(session: RunSession): Promise<void> {
        const artifactRoot = path.join(NOVAORYN_SDK_ROOT, 'Artifacts', 'MinimalKernel');
        const imagePath = path.join(artifactRoot, 'MinimalKernel.img');
        const debugMapPath = path.join(artifactRoot, 'NovaOryn.DebugSymbols.json');
        const qemuPath = 'C:\\Program Files\\qemu\\qemu-system-x86_64.exe';
        const ovmfCode = 'C:\\Program Files\\qemu\\share\\edk2-x86_64-code.fd';
        const ovmfVars = 'C:\\Program Files\\qemu\\share\\edk2-i386-vars.fd';
        await Promise.all([fs.access(imagePath), fs.access(debugMapPath), fs.access(qemuPath), fs.access(ovmfCode), fs.access(ovmfVars)]);

        const rawDebugMap = JSON.parse(await fs.readFile(debugMapPath, 'utf8')) as { anchor?: NativeDebugMap['anchor']; entries?: Array<Record<string, unknown>> };
        const normalizedEntries: NativeSourceLine[] = Array.isArray(rawDebugMap.entries)
            ? rawDebugMap.entries.flatMap(entry => {
                // System.Text.Json serializes the SDK's SourceLineEntry record using
                // PascalCase property names, while older IDE builds expected camelCase.
                // Accept both schemas and discard malformed rows before path handling.
                const sourcePath = typeof entry.sourcePath === 'string' ? entry.sourcePath
                    : typeof entry.SourcePath === 'string' ? entry.SourcePath : undefined;
                const lineValue = typeof entry.line === 'number' ? entry.line
                    : typeof entry.Line === 'number' ? entry.Line : undefined;
                const linkedAddress = typeof entry.linkedAddress === 'string' ? entry.linkedAddress
                    : typeof entry.LinkedAddress === 'string' ? entry.LinkedAddress : undefined;
                return sourcePath && sourcePath.trim() && Number.isInteger(lineValue) && (lineValue ?? 0) > 0 && linkedAddress
                    ? [{ sourcePath, line: lineValue!, linkedAddress }]
                    : [];
            })
            : [];
        session.nativeDebugMap = { anchor: rawDebugMap.anchor!, entries: normalizedEntries };
        if (session.nativeDebugMap.anchor?.symbol !== 'NovaOrynDebugImageAnchor' || !session.nativeDebugMap.anchor.linkedAddress || !session.nativeDebugMap.anchor.resumeLinkedAddress || session.nativeDebugMap.anchor.transport !== 'qemu-debugcon-0xe9-binary-v1' || session.nativeDebugMap.entries.length === 0) {
            throw new Error('NovaOryn.DebugSymbols.json is incomplete or does not contain the NovaOryn 0.37.4 debug rendezvous metadata. Rebuild the SDK/kernel in Debug mode.');
        }

        const stamp = new Date().toISOString().replace(/[-:TZ.]/g, '').slice(0, 17);
        const runDirectory = path.join(artifactRoot, 'Runs', `debug-${stamp}`);
        await fs.mkdir(runDirectory, { recursive: true });
        const varsCopy = path.join(runDirectory, 'OVMF_VARS.fd');
        const serialLog = path.join(runDirectory, 'serial.log');
        const debugConLog = path.join(runDirectory, 'debugcon.bin');
        await fs.copyFile(ovmfVars, varsCopy);
        const gdbPort = await this.findFreePort(1234, 1299);
        const qemuCpus = Math.max(1, Math.ceil(os.cpus().length / 2));
        const args = [
            '-machine', 'q35', '-accel', 'tcg,thread=multi', '-cpu', 'max', '-smp', String(qemuCpus), '-m', '512M',
            '-display', 'sdl',
            '-drive', `if=pflash,format=raw,unit=0,readonly=on,file=${ovmfCode}`,
            '-drive', `if=pflash,format=raw,unit=1,file=${varsCopy}`,
            '-drive', `if=none,format=raw,readonly=on,file=${imagePath},id=boot`,
            '-device', 'virtio-blk-pci,drive=boot,bootindex=0', '-device', 'virtio-gpu-pci',
            '-boot', 'menu=off,strict=on', '-serial', `file:${serialLog}`,
            '-debugcon', `file:${debugConLog}`, '-global', 'isa-debugcon.iobase=0xe9',
            '-monitor', 'none', '-no-reboot', '-no-shutdown',
            '-gdb', `tcp:127.0.0.1:${gdbPort}`,
            '-S'
        ];
        session.output += `\r\n[INFO] Debug launch: QEMU GDB endpoint 127.0.0.1:${gdbPort}.\r\n`;
        session.output += '[INFO] QEMU is held only while the IDE attaches; firmware then runs to the internal NovaOryn debug rendezvous.\r\n';
        session.output += '[INFO] The rendezvous publishes the relocated EFI address through QEMU debugcon; it is never shown as a user breakpoint.\r\n';
        session.qemu = spawn(qemuPath, args, { cwd: session.projectRoot, detached: false, windowsHide: false, stdio: 'ignore' });
        session.qemu.on('error', error => {
            session.error = error.message;
            session.output += `\r\n[FAIL] QEMU debugger launch failed: ${error.message}\r\n`;
            session.complete = true;
            session.exitCode = 1;
        });
        session.qemu.on('close', code => {
            session.gdb?.close();
            session.debug = { ...(session.debug ?? { sourceSymbols: false }), active: false, paused: false, sourceSymbols: session.debug?.sourceSymbols ?? false, message: 'QEMU debug session ended.' };
            session.complete = true;
            session.exitCode = code === 0 || code === null ? 0 : code;
        });

        const gdb = new GdbRspClient(packet => { void this.handleGdbStop(session, packet); });
        await gdb.connect(gdbPort, 15000);
        session.gdb = gdb;
        session.debug = { active: false, paused: false, sourceSymbols: true, gdbPort, message: 'Debugger attached. Preparing source breakpoints…' };
        session.output += `[ OK ] NovaOryn debugger attached to QEMU on port ${gdbPort}.\r\n`;
        session.output += `[ OK ] Exact source-line map loaded: ${session.nativeDebugMap.entries.length} line mapping(s).\r\n`;

        gdb.run('c');
        const runtimeAnchor = await this.waitForDebugRendezvous(debugConLog, 30000);
        session.output += `[ OK ] NovaOryn debug rendezvous reached at runtime address 0x${runtimeAnchor.toString(16)}.\r\n`;

        session.internalPause = true;
        gdb.interrupt();
        await this.waitForPause(session, 3000);

        const linkedAnchor = this.parseAddress(session.nativeDebugMap.anchor.linkedAddress);
        session.relocationDelta = runtimeAnchor - linkedAnchor;
        session.output += `[ OK ] EFI runtime relocation resolved: linked anchor ${session.nativeDebugMap.anchor.linkedAddress}, runtime anchor 0x${runtimeAnchor.toString(16)}, delta ${this.formatSignedHex(session.relocationDelta)}.\r\n`;

        session.breakpointResults = [];
        for (const requested of session.requestedBreakpoints) {
            const result = await this.armSourceBreakpoint(session, requested.sourcePath, requested.line);
            session.breakpointResults.push(result);
            session.output += `[DEBUG] ${result.sourcePath}:${result.line}: ${result.message}\r\n`;
        }

        const linkedResume = this.parseAddress(session.nativeDebugMap.anchor.resumeLinkedAddress!);
        const runtimeResume = linkedResume + session.relocationDelta;
        await this.writeRip(gdb, runtimeResume);
        session.internalPause = false;

        const unresolved = session.breakpointResults.filter(item => !item.verified);
        if (unresolved.length > 0) {
            session.debug = {
                active: true, paused: true, sourceSymbols: true, gdbPort,
                breakpoints: session.breakpointResults.map(item => ({ ...item })),
                message: `${unresolved.length} requested breakpoint(s) could not be verified. Kernel held before KMain; fix/remove them, then Continue.`
            };
            session.output += `[WARN] ${session.breakpointResults.filter(item => item.verified).length}/${session.breakpointResults.length} requested source breakpoint(s) armed before KMain.\r\n`;
            session.output += `[WARN] Kernel is held before KMain because ${unresolved.length} requested breakpoint(s) are unresolved. Remove or move the unverified breakpoint(s), then press Continue.\r\n`;
            return;
        }

        session.debug = {
            active: true, paused: false, sourceSymbols: true, gdbPort,
            breakpoints: session.breakpointResults.map(item => ({ ...item })),
            message: 'Kernel running. Waiting for breakpoint.'
        };
        gdb.run('c');
        session.output += `[ OK ] ${session.breakpointResults.filter(item => item.verified).length}/${session.breakpointResults.length} requested source breakpoint(s) armed before KMain.\r\n`;
        session.output += '[INFO] Kernel released. It will stop only at a verified breakpoint, Pause, exception or panic.\r\n';
    }

    protected async armSourceBreakpoint(session: RunSession, sourcePath: string, line: number): Promise<NovaOrynBreakpointResult> {
        if (!session.gdb || session.relocationDelta === undefined || !session.nativeDebugMap) {
            return { success: false, verified: false, sourcePath, line, message: 'The native source map or EFI relocation is not ready.' };
        }
        const resolved = this.resolveLinkedSourceAddress(session.nativeDebugMap, sourcePath, line);
        if (resolved === undefined) {
            return { success: false, verified: false, sourcePath, line, message: 'No executable NativeAOT sequence point exists on this C# line or a nearby executable line in the same source file.' };
        }
        const runtimeAddress = resolved.linkedAddress + session.relocationDelta;
        const address = runtimeAddress.toString(16);
        const reply = await session.gdb.command(`Z0,${address},1`);
        const breakpoint = { sourcePath: path.resolve(sourcePath), line, resolvedLine: resolved.resolvedLine, address };
        if (reply === 'OK') {
            session.breakpoints.set(`${path.resolve(sourcePath).toLowerCase()}:${line}`, breakpoint);
        }
        const binding = resolved.exactLine
            ? `line ${line}`
            : `requested line ${line} -> executable line ${resolved.resolvedLine}`;
        return {
            success: reply === 'OK',
            verified: reply === 'OK',
            sourcePath,
            line,
            resolvedLine: resolved.resolvedLine,
            address,
            message: reply === 'OK'
                ? `Breakpoint verified (${binding}) at runtime address 0x${address}.`
                : `QEMU rejected the source breakpoint (${binding}): ${reply}`
        };
    }

    protected resolveLinkedSourceAddress(debugMap: NativeDebugMap, sourcePath: string, line: number): ResolvedSourceAddress | undefined {
        const normalize = (value: string | undefined) => value ? path.resolve(value).replace(/\//g, '\\').toLowerCase() : '';
        const normalized = normalize(sourcePath);
        const basename = path.basename(normalized).toLowerCase();

        let entries = debugMap.entries.filter(entry => normalize(entry.sourcePath) === normalized);
        if (entries.length === 0) {
            const basenameMatches = debugMap.entries.filter(entry => path.basename(entry.sourcePath).toLowerCase() === basename);
            const distinctSources = new Set(basenameMatches.map(entry => normalize(entry.sourcePath)));
            if (distinctSources.size === 1) {
                entries = basenameMatches;
            }
        }
        if (entries.length === 0) {
            return undefined;
        }

        const exact = entries.find(entry => entry.line === line);
        if (exact) {
            return { linkedAddress: this.parseAddress(exact.linkedAddress), resolvedLine: exact.line, exactLine: true };
        }

        // C# debuggers bind non-executable lines (braces, declarations, comments, blank
        // lines) to the nearest useful sequence point. Prefer the next executable line,
        // which matches normal breakpoint behaviour, and only fall back a few lines.
        const forward = entries
            .filter(entry => entry.line > line && entry.line - line <= 8)
            .sort((a, b) => a.line - b.line || Number(this.parseAddress(a.linkedAddress) - this.parseAddress(b.linkedAddress)))[0];
        if (forward) {
            return { linkedAddress: this.parseAddress(forward.linkedAddress), resolvedLine: forward.line, exactLine: false };
        }

        const backward = entries
            .filter(entry => entry.line < line && line - entry.line <= 3)
            .sort((a, b) => b.line - a.line || Number(this.parseAddress(a.linkedAddress) - this.parseAddress(b.linkedAddress)))[0];
        return backward
            ? { linkedAddress: this.parseAddress(backward.linkedAddress), resolvedLine: backward.line, exactLine: false }
            : undefined;
    }

    protected async handleGdbStop(session: RunSession, packet: string): Promise<void> {
        if (session.preparingAnchor) {
            session.anchorStopResolve?.();
            return;
        }
        if (session.internalPause) {
            session.debug = { ...(session.debug ?? { active: false, sourceSymbols: true }), active: false, paused: true, sourceSymbols: true, message: 'Debugger preparation pause.' };
            return;
        }
        if (!session.debug?.active) {
            return;
        }

        try {
            if (!session.gdb || session.relocationDelta === undefined || !session.nativeDebugMap) {
                session.debug = { ...session.debug, paused: true, message: 'Kernel stopped.' };
                return;
            }

            const rip = await this.readRip(session.gdb);
            const candidates = [rip, rip > 0n ? rip - 1n : rip];
            const hit = Array.from(session.breakpoints.values()).find(bp =>
                candidates.some(candidate => candidate === this.parseAddress(bp.address)));

            if (hit) {
                await this.clearTemporaryStepBreakpoint(session);
                session.stepPlan = undefined;
                session.lastBreakpoint = hit;
                session.debug = {
                    ...session.debug,
                    paused: true,
                    sourcePath: hit.sourcePath,
                    line: hit.resolvedLine,
                    message: hit.resolvedLine === hit.line
                        ? `Breakpoint reached at ${path.basename(hit.sourcePath)}:${hit.line}.`
                        : `Breakpoint reached at ${path.basename(hit.sourcePath)}:${hit.resolvedLine} (requested line ${hit.line}).`
                };
                await this.populatePausedDebugData(session, rip);
                return;
            }

            if (session.stepPlan) {
                const plan = session.stepPlan;
                if (plan.temporaryAddress && candidates.some(candidate => candidate === plan.temporaryAddress)) {
                    await this.clearTemporaryStepBreakpoint(session);
                    if (plan.kind === 'step-out') {
                        session.stepPlan = undefined;
                        await this.publishPausedLocation(session, rip, 'Step Out completed.');
                        return;
                    }
                }

                const linkedRip = rip - session.relocationDelta;
                const location = this.resolveSourceLocation(session.nativeDebugMap, linkedRip);
                const changedSourceLine = !!location &&
                    (!plan.sourcePath || this.normalizeSourcePath(location.sourcePath) !== this.normalizeSourcePath(plan.sourcePath) || location.line !== plan.line);
                if (changedSourceLine) {
                    session.stepPlan = undefined;
                    await this.publishPausedLocation(session, rip, `${this.stepLabel(plan.kind)} completed.`);
                    return;
                }

                if (plan.machineSteps >= 20000) {
                    session.stepPlan = undefined;
                    await this.publishPausedLocation(session, rip, `${this.stepLabel(plan.kind)} stopped after the safety limit of 20,000 machine instructions.`);
                    return;
                }

                await this.advanceStepPlan(session, rip);
                return;
            }

            await this.publishPausedLocation(
                session,
                rip,
                packet.startsWith('T05') || packet.startsWith('S05') ? 'Kernel stopped.' : `Kernel stopped: ${packet}`
            );
        } catch (error) {
            session.stepPlan = undefined;
            session.debug = { ...session.debug, paused: true, message: `Kernel stopped; source/debug-state lookup failed: ${error instanceof Error ? error.message : String(error)}` };
        }
    }

    protected async publishPausedLocation(session: RunSession, rip: bigint, message: string): Promise<void> {
        if (!session.debug || session.relocationDelta === undefined || !session.nativeDebugMap) {
            return;
        }
        const linkedRip = rip - session.relocationDelta;
        const nearest = this.resolveSourceLocation(session.nativeDebugMap, linkedRip);
        session.debug = {
            ...session.debug,
            paused: true,
            sourcePath: nearest?.sourcePath,
            line: nearest?.line,
            message: nearest ? `${message} ${path.basename(nearest.sourcePath)}:${nearest.line}.` : message
        };
        await this.populatePausedDebugData(session, rip);
    }

    protected async advanceStepPlan(session: RunSession, knownRip?: bigint): Promise<void> {
        const plan = session.stepPlan;
        if (!plan || !session.gdb || !session.debug) {
            return;
        }
        plan.machineSteps++;

        if (plan.kind === 'step-over') {
            const rip = knownRip ?? await this.readRip(session.gdb);
            const instructionLength = await this.currentCallInstructionLength(session.gdb, rip);
            if (instructionLength > 0) {
                const afterCall = rip + BigInt(instructionLength);
                const reply = await session.gdb.command(`Z0,${afterCall.toString(16)},1`);
                if (reply === 'OK') {
                    plan.temporaryAddress = afterCall;
                    session.gdb.run('c');
                    session.debug = this.runningDebugState(session, 'Step Over running…');
                    return;
                }
            }
        }

        session.gdb.run('s');
        session.debug = this.runningDebugState(session, `${this.stepLabel(plan.kind)} running…`);
    }

    protected async clearTemporaryStepBreakpoint(session: RunSession): Promise<void> {
        const address = session.stepPlan?.temporaryAddress;
        if (!address || !session.gdb) {
            return;
        }
        try { await session.gdb.command(`z0,${address.toString(16)},1`); } catch { }
        if (session.stepPlan) {
            session.stepPlan.temporaryAddress = undefined;
        }
    }

    protected runningDebugState(session: RunSession, message: string): NovaOrynDebugState {
        return {
            ...(session.debug ?? { active: true, sourceSymbols: true }),
            active: true,
            paused: false,
            sourcePath: undefined,
            line: undefined,
            registers: undefined,
            callStack: undefined,
            locals: undefined,
            localsMessage: undefined,
            message
        };
    }

    protected stepLabel(kind: StepPlan['kind']): string {
        if (kind === 'step-into') { return 'Step Into'; }
        if (kind === 'step-over') { return 'Step Over'; }
        return 'Step Out';
    }

    protected normalizeSourcePath(sourcePath: string): string {
        return path.resolve(sourcePath).replace(/\\/g, '/').toLowerCase();
    }

    protected async currentCallInstructionLength(gdb: GdbRspClient, rip: bigint): Promise<number> {
        const bytes = await this.readMemory(gdb, rip, 15);
        if (bytes.length === 0) { return 0; }
        let i = 0;
        while (i < bytes.length && (bytes[i] === 0x66 || bytes[i] === 0x67 || bytes[i] === 0xf2 || bytes[i] === 0xf3 || (bytes[i] >= 0x40 && bytes[i] <= 0x4f))) { i++; }
        if (bytes[i] === 0xe8) { return i + 5; }
        if (bytes[i] !== 0xff || i + 1 >= bytes.length) { return 0; }
        const modrm = bytes[i + 1];
        const reg = (modrm >> 3) & 7;
        if (reg !== 2 && reg !== 3) { return 0; }
        let length = i + 2;
        const mod = (modrm >> 6) & 3;
        const rm = modrm & 7;
        if (mod !== 3 && rm === 4) {
            if (length >= bytes.length) { return 0; }
            const sib = bytes[length++];
            const base = sib & 7;
            if (mod === 0 && base === 5) { length += 4; }
        }
        if (mod === 0 && rm === 5) { length += 4; }
        else if (mod === 1) { length += 1; }
        else if (mod === 2) { length += 4; }
        return length <= bytes.length ? length : 0;
    }

    protected async populatePausedDebugData(session: RunSession, rip: bigint): Promise<void> {
        if (!session.gdb || !session.debug) { return; }
        const registers = await this.readRegisterSet(session.gdb);
        const registerMap = new Map(registers.map(item => [item.name, this.parseAddress(item.value)]));
        const rbp = registerMap.get('rbp') ?? 0n;
        const rsp = registerMap.get('rsp') ?? 0n;
        const callStack = await this.readCallStack(session, rip, rbp, rsp);
        const locals = await this.readFrameSlots(session.gdb, rbp, rsp);
        session.debug = {
            ...session.debug,
            registers,
            callStack,
            locals,
            localsMessage: 'NativeAOT C# local-variable names are not exported by the current NovaOryn source map yet; Locals shows the current native frame/stack slots while Registers shows the complete x64 integer register set.'
        };
    }

    protected async readRegisterSet(gdb: GdbRspClient): Promise<NovaOrynDebugRegister[]> {
        const names = ['rax','rbx','rcx','rdx','rsi','rdi','rbp','rsp','r8','r9','r10','r11','r12','r13','r14','r15','rip','rflags'];
        const values: NovaOrynDebugRegister[] = [];
        for (let i = 0; i < names.length; i++) {
            const value = await this.readRegister(gdb, i);
            values.push({ name: names[i], value: `0x${value.toString(16).padStart(16, '0')}` });
        }
        return values;
    }

    protected async readCallStack(session: RunSession, rip: bigint, initialRbp: bigint, rsp: bigint): Promise<NovaOrynDebugFrame[]> {
        const frames: NovaOrynDebugFrame[] = [];
        const addFrame = (address: bigint, index: number) => {
            let location: NativeSourceLine | undefined;
            if (session.relocationDelta !== undefined && session.nativeDebugMap) {
                const linked = address - session.relocationDelta;
                location = this.resolveSourceLocation(session.nativeDebugMap, linked);
            }
            frames.push({
                index,
                address: `0x${address.toString(16)}`,
                label: location ? `${path.basename(location.sourcePath)}:${location.line}` : `0x${address.toString(16)}`,
                sourcePath: location?.sourcePath,
                line: location?.line
            });
        };
        addFrame(rip, 0);
        if (!session.gdb) { return frames; }
        let rbp = initialRbp;
        const visited = new Set<string>();
        for (let index = 1; index < 24 && rbp !== 0n; index++) {
            const key = rbp.toString(16);
            if (visited.has(key)) { break; }
            visited.add(key);
            const previousRbp = await this.readU64(session.gdb, rbp);
            const returnAddress = await this.readU64(session.gdb, rbp + 8n);
            if (returnAddress === 0n) { break; }
            addFrame(returnAddress, index);
            if (previousRbp <= rbp || previousRbp - rbp > 0x1000000n) { break; }
            rbp = previousRbp;
        }

        // NativeAOT may use RBP as a general-purpose register even in Debug builds.
        // If a conventional frame chain was unavailable, conservatively scan a small
        // portion of the current stack for return addresses that map to known source.
        if (frames.length === 1 && rsp !== 0n && session.relocationDelta !== undefined && session.nativeDebugMap) {
            const stack = await this.readMemory(session.gdb, rsp, 0x100);
            for (let offset = 0; offset + 8 <= stack.length && frames.length < 16; offset += 8) {
                let candidate = 0n;
                for (let i = 7; i >= 0; i--) { candidate = (candidate << 8n) | BigInt(stack[offset + i]); }
                if (candidate === 0n) { continue; }
                const linkedCandidate = candidate - session.relocationDelta;
                if (!this.isWithinDebugMap(session.nativeDebugMap, linkedCandidate)) { continue; }
                const location = this.resolveSourceLocation(session.nativeDebugMap, linkedCandidate);
                if (!location) { continue; }
                const duplicate = frames.some(frame => frame.address.toLowerCase() === `0x${candidate.toString(16)}`);
                if (!duplicate) { addFrame(candidate, frames.length); }
            }
        }
        return frames;
    }

    protected async readFrameSlots(gdb: GdbRspClient, rbp: bigint, rsp: bigint): Promise<NovaOrynDebugVariable[]> {
        const result: NovaOrynDebugVariable[] = [];
        if (rbp !== 0n) {
            for (let offset = -0x40; offset <= -0x08; offset += 8) {
                const address = rbp + BigInt(offset);
                const value = await this.readU64(gdb, address);
                result.push({ name: `[rbp${offset.toString(16)}]`, value: `0x${value.toString(16).padStart(16, '0')}`, kind: 'local' });
            }
            for (let offset = 0x10; offset <= 0x30; offset += 8) {
                const value = await this.readU64(gdb, rbp + BigInt(offset));
                result.push({ name: `[rbp+0x${offset.toString(16)}]`, value: `0x${value.toString(16).padStart(16, '0')}`, kind: 'argument' });
            }
        } else if (rsp !== 0n) {
            for (let offset = 0; offset <= 0x40; offset += 8) {
                const value = await this.readU64(gdb, rsp + BigInt(offset));
                result.push({ name: `[rsp+0x${offset.toString(16)}]`, value: `0x${value.toString(16).padStart(16, '0')}`, kind: 'stack' });
            }
        }
        return result;
    }

    protected async readRegister(gdb: GdbRspClient, index: number): Promise<bigint> {
        const reply = await gdb.command(`p${index.toString(16)}`);
        if (!/^[0-9a-fA-F]+$/.test(reply) || reply.length < 2) {
            throw new Error(`QEMU returned an invalid register value for p${index.toString(16)}: ${reply}`);
        }
        const bytes = reply.match(/../g) ?? [];
        return BigInt(`0x${bytes.reverse().join('')}`);
    }

    protected async readMemory(gdb: GdbRspClient, address: bigint, length: number): Promise<Buffer> {
        const reply = await gdb.command(`m${address.toString(16)},${length.toString(16)}`);
        if (reply.startsWith('E') || !/^[0-9a-fA-F]*$/.test(reply) || reply.length % 2 !== 0) {
            return Buffer.alloc(0);
        }
        return Buffer.from(reply, 'hex');
    }

    protected async readU64(gdb: GdbRspClient, address: bigint): Promise<bigint> {
        const bytes = await this.readMemory(gdb, address, 8);
        if (bytes.length !== 8) { return 0n; }
        let value = 0n;
        for (let i = 7; i >= 0; i--) { value = (value << 8n) | BigInt(bytes[i]); }
        return value;
    }

    protected isWithinDebugMap(debugMap: NativeDebugMap, linkedAddress: bigint): boolean {
        if (debugMap.entries.length === 0) { return false; }
        let min = this.parseAddress(debugMap.entries[0].linkedAddress);
        let max = min;
        for (const entry of debugMap.entries) {
            const address = this.parseAddress(entry.linkedAddress);
            if (address < min) { min = address; }
            if (address > max) { max = address; }
        }
        return linkedAddress >= min && linkedAddress <= max + 0x10000n;
    }

    protected resolveSourceLocation(debugMap: NativeDebugMap, linkedAddress: bigint): NativeSourceLine | undefined {
        let nearest: NativeSourceLine | undefined;
        let nearestAddress = -1n;
        for (const entry of debugMap.entries) {
            const address = this.parseAddress(entry.linkedAddress);
            if (address <= linkedAddress && address > nearestAddress) {
                nearest = entry;
                nearestAddress = address;
            }
        }
        return nearest;
    }

    protected async waitForDebugRendezvous(debugConLog: string, timeoutMs: number): Promise<bigint> {
        const magic = Buffer.from('NODBG64!', 'ascii');
        const until = Date.now() + timeoutMs;
        while (Date.now() < until) {
            try {
                const data = await fs.readFile(debugConLog);
                const index = data.indexOf(magic);
                if (index >= 0 && data.length >= index + magic.length + 8) {
                    const bytes = data.subarray(index + magic.length, index + magic.length + 8);
                    let address = 0n;
                    for (let i = 7; i >= 0; i--) { address = (address << 8n) | BigInt(bytes[i]); }
                    if (address !== 0n) { return address; }
                }
            } catch { }
            await new Promise(resolve => setTimeout(resolve, 20));
        }
        throw new Error('NovaOryn debug rendezvous was not published within 30 seconds.');
    }

    protected async writeRip(gdb: GdbRspClient, value: bigint): Promise<void> {
        let hex = value.toString(16).padStart(16, '0');
        const bytes = hex.match(/../g) ?? [];
        hex = bytes.reverse().join('');
        const reply = await gdb.command(`P10=${hex}`);
        if (reply !== 'OK') { throw new Error(`QEMU rejected the debugger resume RIP: ${reply}`); }
    }

    protected async readRip(gdb: GdbRspClient): Promise<bigint> {
        return this.readRegister(gdb, 16);
    }

    protected parseAddress(value: string): bigint {
        const trimmed = value.trim().replace(/^0x/i, '');
        return BigInt(`0x${trimmed || '0'}`);
    }

    protected formatSignedHex(value: bigint): string {
        return value < 0n ? `-0x${(-value).toString(16)}` : `+0x${value.toString(16)}`;
    }

    protected async waitForPause(session: RunSession, timeoutMs: number): Promise<void> {
        const until = Date.now() + timeoutMs;
        while (Date.now() < until) {
            if (session.debug?.paused) { return; }
            await new Promise(resolve => setTimeout(resolve, 25));
        }
        throw new Error('QEMU did not acknowledge the debugger pause request.');
    }

    protected async findFreePort(start: number, end: number): Promise<number> {
        for (let port = start; port <= end; port++) {
            if (await new Promise<boolean>(resolve => {
                const server = net.createServer();
                server.once('error', () => resolve(false));
                server.listen(port, '127.0.0.1', () => server.close(() => resolve(true)));
            })) { return port; }
        }
        throw new Error(`No free QEMU GDB port was found between ${start} and ${end}.`);
    }

    protected async exists(filePath: string): Promise<boolean> {
        try { await fs.access(filePath); return true; } catch { return false; }
    }

    async readProjectConfiguration(projectPath: string): Promise<NovaOrynConfigurationResult> {
        try {
            const projectRoot = this.requireOperatingSystemRoot(projectPath);
            const configurationPath = path.join(projectRoot, 'NovaOryn.json');
            const configuration = JSON.parse(await fs.readFile(configurationPath, 'utf8')) as NovaOrynProjectConfiguration;
            const validationError = this.validate(configuration);
            if (validationError) {
                return { success: false, error: `Existing NovaOryn.json is invalid: ${validationError}` };
            }
            return { success: true, projectPath: projectRoot, configuration: this.copyConfiguration(configuration) };
        } catch (error) {
            const message = error instanceof Error ? error.message : String(error);
            return { success: false, error: message };
        }
    }

    async reconfigureProject(projectPath: string, configuration: NovaOrynProjectConfiguration): Promise<NovaOrynProjectResult> {
        try {
            const projectRoot = this.requireOperatingSystemRoot(projectPath);
            const existingName = path.basename(projectRoot);
            if (configuration.name.toLowerCase() !== existingName.toLowerCase()) {
                return { success: false, error: 'The operating-system name cannot be changed while reconfiguring. Create a new OS to use a different name.' };
            }

            const authoritativeConfiguration: NovaOrynProjectConfiguration = {
                ...this.copyConfiguration(configuration),
                name: existingName,
                location: NOVAORYN_OS_ROOT
            };
            const validationError = this.validate(authoritativeConfiguration);
            if (validationError) {
                return { success: false, error: validationError };
            }

            await fs.access(path.join(projectRoot, 'NovaOryn.json'));
            const previousProjects = await this.readGeneratedProjectGraph(projectRoot);
            const generatedProjects = this.buildProjectGraph(authoritativeConfiguration);

            await this.removeObsoleteGeneratedProjects(projectRoot, previousProjects, generatedProjects, authoritativeConfiguration.name);
            await this.createBaseDirectories(projectRoot, authoritativeConfiguration);
            for (const project of generatedProjects) {
                await this.writeGeneratedProject(projectRoot, authoritativeConfiguration, project);
            }

            await fs.mkdir(path.join(projectRoot, 'Configuration'), { recursive: true });
            await fs.writeFile(path.join(projectRoot, 'NovaOryn.json'), this.configurationJson(authoritativeConfiguration), 'utf8');
            await fs.writeFile(path.join(projectRoot, 'NovaOryn.ProjectGraph.json'), this.projectGraphJson(authoritativeConfiguration, generatedProjects), 'utf8');
            await fs.writeFile(path.join(projectRoot, 'NovaOrynProject.json'), this.sdkProjectManifest(authoritativeConfiguration), 'utf8');
            await fs.writeFile(path.join(projectRoot, 'Configuration', 'GeneratedConfiguration.cs'), this.generatedConfigurationSource(authoritativeConfiguration), 'utf8');
            // Kernel\Kernel.cs is deliberately user-owned after initial creation and is never replaced here.
            await fs.writeFile(path.join(projectRoot, 'NovaOryn.slnx'), this.solutionFile(authoritativeConfiguration, generatedProjects), 'utf8');
            await fs.writeFile(path.join(projectRoot, 'Build.bat'), this.buildBatch(), 'utf8');
            await fs.writeFile(path.join(projectRoot, 'Run.bat'), this.runBatch(authoritativeConfiguration), 'utf8');
            await fs.writeFile(path.join(projectRoot, 'README.md'), this.projectReadme(authoritativeConfiguration, generatedProjects), 'utf8');

            return {
                success: true,
                projectPath: projectRoot,
                generatedProjects: generatedProjects.map(project => project.id)
            };
        } catch (error) {
            const message = error instanceof Error ? error.message : String(error);
            return { success: false, error: message };
        }
    }

    async createProject(configuration: NovaOrynProjectConfiguration): Promise<NovaOrynProjectResult> {
        try {
            const validationError = this.validate(configuration);
            if (validationError) {
                return { success: false, error: validationError };
            }

            await fs.mkdir(NOVAORYN_OS_ROOT, { recursive: true });
            const authoritativeConfiguration: NovaOrynProjectConfiguration = { ...configuration, location: NOVAORYN_OS_ROOT };
            const projectRoot = path.join(NOVAORYN_OS_ROOT, authoritativeConfiguration.name);
            await fs.mkdir(projectRoot, { recursive: false });

            const generatedProjects = this.buildProjectGraph(authoritativeConfiguration);
            await this.createBaseDirectories(projectRoot, authoritativeConfiguration);

            for (const project of generatedProjects) {
                await this.writeGeneratedProject(projectRoot, authoritativeConfiguration, project);
            }

            await fs.mkdir(path.join(projectRoot, 'Configuration'), { recursive: true });
            await fs.writeFile(path.join(projectRoot, 'NovaOryn.json'), this.configurationJson(authoritativeConfiguration), 'utf8');
            await fs.writeFile(path.join(projectRoot, 'NovaOryn.ProjectGraph.json'), this.projectGraphJson(authoritativeConfiguration, generatedProjects), 'utf8');
            await fs.writeFile(path.join(projectRoot, 'NovaOrynProject.json'), this.sdkProjectManifest(authoritativeConfiguration), 'utf8');
            await fs.writeFile(path.join(projectRoot, 'Configuration', 'GeneratedConfiguration.cs'), this.generatedConfigurationSource(authoritativeConfiguration), 'utf8');
            await fs.writeFile(path.join(projectRoot, 'Kernel', 'Kernel.cs'), this.kernelSource(authoritativeConfiguration), 'utf8');
            await fs.writeFile(path.join(projectRoot, 'NovaOryn.slnx'), this.solutionFile(authoritativeConfiguration, generatedProjects), 'utf8');
            await fs.writeFile(path.join(projectRoot, 'Build.bat'), this.buildBatch(), 'utf8');
            await fs.writeFile(path.join(projectRoot, 'Run.bat'), this.runBatch(authoritativeConfiguration), 'utf8');
            await fs.writeFile(path.join(projectRoot, 'README.md'), this.projectReadme(authoritativeConfiguration, generatedProjects), 'utf8');

            return {
                success: true,
                projectPath: projectRoot,
                generatedProjects: generatedProjects.map(project => project.id)
            };
        } catch (error) {
            const message = error instanceof Error ? error.message : String(error);
            return { success: false, error: message };
        }
    }

    protected requireOperatingSystemRoot(projectPath: string): string {
        const projectRoot = path.resolve(projectPath);
        const osRoot = path.resolve(NOVAORYN_OS_ROOT);
        const relative = path.relative(osRoot, projectRoot);
        if (!relative || relative.startsWith('..') || path.isAbsolute(relative) || relative.includes(path.sep)) {
            throw new Error(`NovaOryn configuration only accepts an OS root directly beneath ${NOVAORYN_OS_ROOT}.`);
        }
        return projectRoot;
    }

    protected copyConfiguration(configuration: NovaOrynProjectConfiguration): NovaOrynProjectConfiguration {
        return {
            ...configuration,
            location: NOVAORYN_OS_ROOT,
            timers: [...configuration.timers],
            drivers: [...configuration.drivers],
            storageControllers: [...configuration.storageControllers],
            networkDrivers: [...configuration.networkDrivers],
            input: [...configuration.input],
            graphics: [...configuration.graphics],
            debugging: [...configuration.debugging],
            testing: [...configuration.testing],
            safetyOptions: [...configuration.safetyOptions]
        };
    }

    protected async readGeneratedProjectGraph(projectRoot: string): Promise<GeneratedProject[]> {
        try {
            const graph = JSON.parse(await fs.readFile(path.join(projectRoot, 'NovaOryn.ProjectGraph.json'), 'utf8')) as { projects?: GeneratedProject[] };
            return Array.isArray(graph.projects) ? graph.projects.filter(project =>
                !!project && typeof project.id === 'string' && typeof project.relativePath === 'string') : [];
        } catch {
            return [];
        }
    }

    protected generatedProjectDirectory(projectRoot: string, relativePath: string): string {
        const normalised = relativePath.replace(/\\/g, '/');
        const segments = normalised.split('/').filter(segment => segment.length > 0);
        if (segments.length === 0 || segments.some(segment => segment === '.' || segment === '..')) {
            throw new Error(`Invalid generated project path in NovaOryn.ProjectGraph.json: ${relativePath}`);
        }
        const directory = path.resolve(projectRoot, ...segments);
        const relative = path.relative(projectRoot, directory);
        if (!relative || relative.startsWith('..') || path.isAbsolute(relative)) {
            throw new Error(`Generated project path escapes the NovaOryn OS root: ${relativePath}`);
        }
        return directory;
    }

    protected async removeObsoleteGeneratedProjects(
        projectRoot: string,
        previousProjects: GeneratedProject[],
        nextProjects: GeneratedProject[],
        osName: string
    ): Promise<void> {
        const keep = new Set(nextProjects.map(project => `${project.id}\n${project.relativePath}`));
        for (const project of previousProjects) {
            if (keep.has(`${project.id}\n${project.relativePath}`) || project.id === 'Kernel.Core') {
                continue;
            }

            const projectDirectory = this.generatedProjectDirectory(projectRoot, project.relativePath);
            const projectFile = path.join(projectDirectory, `${this.safeSegment(osName)}.${this.safeSegment(project.id)}.csproj`);
            await fs.rm(projectFile, { force: true });
            await fs.rm(path.join(projectDirectory, 'GeneratedFeature.cs'), { force: true });
            await this.removeEmptyGeneratedDirectories(projectDirectory, projectRoot);
        }
    }

    protected async removeEmptyGeneratedDirectories(directory: string, projectRoot: string): Promise<void> {
        let current = directory;
        while (current.toLowerCase() !== projectRoot.toLowerCase()) {
            try {
                const entries = await fs.readdir(current);
                if (entries.length !== 0) {
                    return;
                }
                await fs.rmdir(current);
                current = path.dirname(current);
            } catch {
                return;
            }
        }
    }

    protected validate(configuration: NovaOrynProjectConfiguration): string | undefined {
        if (!/^[A-Za-z][A-Za-z0-9._-]*$/.test(configuration.name)) {
            return 'Project name must begin with a letter and contain only letters, numbers, dot, underscore or hyphen.';
        }
        if (configuration.interruptModel === 'apic' || configuration.interruptModel === 'x2apic' || configuration.interruptModel === 'pic-compat') {
            if (configuration.targetArchitecture !== 'x86_64') {
                return `${configuration.interruptModel} is an x86-64 interrupt-controller option. Use Architecture default for ${configuration.targetArchitecture}.`;
            }
        }
        if (configuration.bootArchitecture === 'multiboot2' && configuration.targetArchitecture !== 'x86_64') {
            return 'Multiboot2 generation is currently available only for x86-64.';
        }
        if (!configuration.userland && (configuration.shell !== 'none' || configuration.gui !== 'none')) {
            return 'Shell and GUI generation require userland support.';
        }
        if (configuration.networkStack === 'none' && configuration.networkDrivers.length > 0) {
            return 'Select a networking stack before selecting network drivers.';
        }
        return undefined;
    }

    protected async createBaseDirectories(projectRoot: string, configuration: NovaOrynProjectConfiguration): Promise<void> {
        const directories = new Set<string>([
            'Kernel', 'Configuration', 'Libraries', 'Applications'
        ]);

        if (configuration.kernelArchitecture === 'microkernel' || configuration.kernelArchitecture === 'hybrid') {
            directories.add('Services');
        }
        if (configuration.kernelArchitecture === 'microkernel') {
            directories.add('Drivers');
        }
        if (configuration.userland) {
            directories.add('Userland');
        }
        if (configuration.testing.length > 0) {
            directories.add('Tests');
        }

        for (const directory of directories) {
            await fs.mkdir(path.join(projectRoot, directory), { recursive: true });
        }
    }

    protected buildProjectGraph(configuration: NovaOrynProjectConfiguration): GeneratedProject[] {
        const projects: GeneratedProject[] = [
            this.project(configuration, 'Kernel.Core', 'kernel', 'Core kernel and KMain entry point', 'Core')
        ];

        projects.push(this.project(configuration, `Architecture.${configuration.targetArchitecture}`, 'kernel-module', 'CPU architecture support', 'Architecture'));
        projects.push(this.project(configuration, `Boot.${configuration.bootArchitecture}`, 'kernel-module', 'Boot architecture support', 'Boot'));
        projects.push(this.project(configuration, `Memory.${configuration.memorySystem}`, 'kernel-module', 'Memory-system implementation', 'Memory'));
        projects.push(this.project(configuration, `Interrupts.${configuration.interruptModel}`, 'kernel-module', 'Interrupt-controller implementation', 'Interrupts'));

        if (configuration.scheduler !== 'none') {
            projects.push(this.project(configuration, `Scheduler.${configuration.scheduler}`, 'kernel-module', 'Scheduler implementation', 'Scheduler'));
        }
        if (configuration.processSupport !== 'none') {
            projects.push(this.project(configuration, `Processes.${configuration.processSupport}`, this.serviceKind(configuration), 'Thread/process support', 'Processes'));
        }
        projects.push(this.project(configuration, `Syscalls.${configuration.syscallModel}`, 'kernel-module', 'System-call dispatch model', 'Syscalls'));

        if (configuration.smp) {
            projects.push(this.project(configuration, 'Smp', 'kernel-module', 'Symmetric multiprocessing and per-CPU support', 'Smp'));
        }
        for (const timer of configuration.timers) {
            projects.push(this.project(configuration, `Timer.${timer}`, 'kernel-module', `${timer} timer/clock support`, 'Timers'));
        }
        for (const driver of configuration.drivers) {
            projects.push(this.project(configuration, `Driver.${driver}`, this.driverKind(configuration), `${driver} device driver`, 'Drivers'));
        }
        for (const storage of configuration.storageControllers) {
            projects.push(this.project(configuration, `Storage.${storage}`, this.driverKind(configuration), `${storage} storage controller`, 'Storage'));
        }
        if (configuration.filesystem !== 'none') {
            projects.push(this.project(configuration, `Filesystem.${configuration.filesystem}`, this.serviceKind(configuration), `${configuration.filesystem} filesystem`, 'Filesystem'));
        }
        if (configuration.networkStack !== 'none') {
            projects.push(this.project(configuration, `Networking.${configuration.networkStack}`, this.serviceKind(configuration), `${configuration.networkStack} networking stack`, 'Networking'));
            for (const driver of configuration.networkDrivers) {
                projects.push(this.project(configuration, `NetworkDriver.${driver}`, this.driverKind(configuration), `${driver} network adapter driver`, 'NetworkDrivers'));
            }
        }
        for (const input of configuration.input) {
            projects.push(this.project(configuration, `Input.${input}`, this.driverKind(configuration), `${input} input support`, 'Input'));
        }
        for (const graphics of configuration.graphics) {
            projects.push(this.project(configuration, `Graphics.${graphics}`, this.driverKind(configuration), `${graphics} graphics support`, 'Graphics'));
        }
        if (configuration.audio !== 'none') {
            projects.push(this.project(configuration, `Audio.${configuration.audio}`, this.driverKind(configuration), `${configuration.audio} audio support`, 'Audio'));
        }
        if (configuration.virtualisation !== 'none') {
            projects.push(this.project(configuration, `Virtualisation.${configuration.virtualisation}`, 'kernel-module', `${configuration.virtualisation} virtualisation support`, 'Virtualisation'));
        }
        if (configuration.debugging.length > 0) {
            projects.push(this.project(configuration, 'Debugging', 'kernel-module', 'Selected debugging facilities', 'Debugging'));
        }
        if (configuration.safetyProfile !== 'general' || configuration.safetyOptions.length > 0) {
            projects.push(this.project(configuration, `Safety.${configuration.safetyProfile}`, 'kernel-module', 'RTOS/safety policy and enforcement hooks', 'Safety'));
        }
        if (configuration.userland) {
            projects.push(this.project(configuration, 'Userland.Runtime', 'userland', 'Base userland runtime', 'Runtime'));
            if (configuration.shell !== 'none') {
                projects.push(this.project(configuration, `Shell.${configuration.shell}`, 'userland', 'NovaOryn command shell', 'Shell'));
            }
            if (configuration.gui !== 'none') {
                projects.push(this.project(configuration, `Gui.${configuration.gui}`, 'userland', 'Graphical user interface', 'Gui'));
            }
        }
        for (const test of configuration.testing) {
            projects.push(this.project(configuration, `Test.${test}`, 'test', `${test} test program`, 'Tests'));
        }

        return projects;
    }

    protected project(
        configuration: NovaOrynProjectConfiguration,
        id: string,
        kind: GeneratedProject['kind'],
        description: string,
        group: string
    ): GeneratedProject {
        const safeId = this.safeSegment(id);
        let relativePath: string;

        if (kind === 'kernel') {
            relativePath = 'Kernel';
        } else if (kind === 'userland') {
            relativePath = path.posix.join('Userland', group, safeId);
        } else if (kind === 'test') {
            relativePath = path.posix.join('Tests', safeId);
        } else if (kind === 'service') {
            relativePath = path.posix.join('Services', group, safeId);
        } else if (kind === 'driver' && configuration.kernelArchitecture === 'microkernel') {
            relativePath = path.posix.join('Drivers', group, safeId);
        } else {
            relativePath = path.posix.join('Kernel', group, safeId);
        }

        return { id, relativePath, kind, description };
    }

    protected driverKind(configuration: NovaOrynProjectConfiguration): GeneratedProject['kind'] {
        return configuration.kernelArchitecture === 'microkernel' ? 'driver' : 'kernel-module';
    }

    protected serviceKind(configuration: NovaOrynProjectConfiguration): GeneratedProject['kind'] {
        return configuration.kernelArchitecture === 'monolithic' ? 'kernel-module' : 'service';
    }

    protected async writeGeneratedProject(projectRoot: string, configuration: NovaOrynProjectConfiguration, project: GeneratedProject): Promise<void> {
        const projectDirectory = path.join(projectRoot, ...project.relativePath.split('/'));
        await fs.mkdir(projectDirectory, { recursive: true });

        const projectFileName = `${this.safeSegment(configuration.name)}.${this.safeSegment(project.id)}.csproj`;
        await fs.writeFile(path.join(projectDirectory, projectFileName), this.csProject(configuration, project), 'utf8');

        if (project.id !== 'Kernel.Core') {
            await fs.writeFile(path.join(projectDirectory, 'GeneratedFeature.cs'), this.featureSource(configuration, project), 'utf8');
        }
    }

    protected csProject(configuration: NovaOrynProjectConfiguration, project: GeneratedProject): string {
        const outputType = project.kind === 'test' ? 'Exe' : 'Library';
        return [
            '<Project Sdk="Microsoft.NET.Sdk">',
            '  <PropertyGroup>',
            '    <TargetFramework>net10.0</TargetFramework>',
            `    <OutputType>${outputType}</OutputType>`,
            '    <ImplicitUsings>disable</ImplicitUsings>',
            '    <Nullable>enable</Nullable>',
            `    <AssemblyName>${this.safeSegment(configuration.name)}.${this.safeSegment(project.id)}</AssemblyName>`,
            `    <RootNamespace>${this.namespace(configuration.name)}.${this.namespace(project.id)}</RootNamespace>`,
            '  </PropertyGroup>',
            '</Project>',
            ''
        ].join('\n');
    }

    protected configurationJson(configuration: NovaOrynProjectConfiguration): string {
        return JSON.stringify({
            ...configuration,
            schemaVersion: 2,
            ideVersion: NOVAORYN_IDE_VERSION,
            sdk: {
                root: NOVAORYN_SDK_ROOT,
                buildEntryPoint: 'Build-NovaOryn.bat',
                runEntryPoint: 'Build-NovaOryn.bat',
                runOperation: 'Run'
            }
        }, null, 2) + '\n';
    }

    protected projectGraphJson(configuration: NovaOrynProjectConfiguration, projects: GeneratedProject[]): string {
        return JSON.stringify({
            schemaVersion: 1,
            generatedBy: `NovaOryn IDE ${NOVAORYN_IDE_VERSION}`,
            kernelArchitecture: configuration.kernelArchitecture,
            projects
        }, null, 2) + '\n';
    }

    protected generatedConfigurationSource(configuration: NovaOrynProjectConfiguration): string {
        const namespaceName = this.namespace(configuration.name);
        const strings = (values: string[]) => values.length === 0 ? 'System.Array.Empty<string>()' : `new string[] { ${values.map(value => JSON.stringify(value)).join(', ')} }`;
        return `// <auto-generated />\nnamespace ${namespaceName}.Configuration;\n\npublic static class GeneratedConfiguration\n{\n    public const string KernelArchitecture = ${JSON.stringify(configuration.kernelArchitecture)};\n    public const string TargetArchitecture = ${JSON.stringify(configuration.targetArchitecture)};\n    public const string BootArchitecture = ${JSON.stringify(configuration.bootArchitecture)};\n    public const string MemorySystem = ${JSON.stringify(configuration.memorySystem)};\n    public const string Scheduler = ${JSON.stringify(configuration.scheduler)};\n    public const string ProcessSupport = ${JSON.stringify(configuration.processSupport)};\n    public const string SyscallModel = ${JSON.stringify(configuration.syscallModel)};\n    public const bool Smp = ${configuration.smp ? 'true' : 'false'};\n    public const string InterruptModel = ${JSON.stringify(configuration.interruptModel)};\n    public const string Filesystem = ${JSON.stringify(configuration.filesystem)};\n    public const string NetworkStack = ${JSON.stringify(configuration.networkStack)};\n    public const string Audio = ${JSON.stringify(configuration.audio)};\n    public const bool Userland = ${configuration.userland ? 'true' : 'false'};\n    public const string Shell = ${JSON.stringify(configuration.shell)};\n    public const string Gui = ${JSON.stringify(configuration.gui)};\n    public const string Virtualisation = ${JSON.stringify(configuration.virtualisation)};\n    public const string SafetyProfile = ${JSON.stringify(configuration.safetyProfile)};\n\n#if DEBUG\n    public const bool DebugBuild = true;\n#else\n    public const bool DebugBuild = false;\n#endif\n    public const bool DebuggingConfigured = ${configuration.debugging.length > 0 ? 'true' : 'false'};\n    public static bool DebuggingEnabled() => DebugBuild && DebuggingConfigured;\n    public static string[] EffectiveDebugging() => DebuggingEnabled() ? Debugging() : System.Array.Empty<string>();\n\n    public static string[] Timers() => ${strings(configuration.timers)};\n    public static string[] Drivers() => ${strings(configuration.drivers)};\n    public static string[] StorageControllers() => ${strings(configuration.storageControllers)};\n    public static string[] NetworkDrivers() => ${strings(configuration.networkDrivers)};\n    public static string[] Input() => ${strings(configuration.input)};\n    public static string[] Graphics() => ${strings(configuration.graphics)};\n    public static string[] Debugging() => ${strings(configuration.debugging)};\n    public static string[] Testing() => ${strings(configuration.testing)};\n    public static string[] SafetyOptions() => ${strings(configuration.safetyOptions)};\n}\n`;
    }

    protected kernelSource(configuration: NovaOrynProjectConfiguration): string {
        const namespaceName = this.namespace(configuration.name);
        return `using System;\nusing NovaOryn.Kernel.Console;\nusing NovaOryn.Kernel.Platform.X64;\n\nnamespace ${namespaceName}.Kernel;\n\npublic static class Kernel\n{\n    private const UInt32 ConsoleFontSize = 32U;\n\n    public static Boolean KMain(BootContext boot)\n    {\n        if (!KernelConsole.Initialize(boot, ConsoleFontSize)) return false;\n        if (!KernelConsole.WriteLine(${JSON.stringify(configuration.name + ' KMain started.')})) return false;\n        if (!boot.HasFinalMemoryMap()) return false;\n        if (!KernelPlatform.InitializeDescriptors()) return false;\n        if (!KernelPlatform.InitializeInterrupts()) return false;\n        if (!KernelPlatform.DisableLegacyPic()) return false;\n        return KernelPlatform.Halt();\n    }\n}\n`;
    }

    protected sdkProjectManifest(configuration: NovaOrynProjectConfiguration): string {
        const targetArchitecture = configuration.targetArchitecture === 'x86_64' ? 'x64' : configuration.targetArchitecture;
        const bootProtocol = configuration.bootArchitecture === 'uefi' ? 'Uefi' : configuration.bootArchitecture;
        return JSON.stringify({
            Name: configuration.name,
            ProjectFile: 'Sdk/NovaOryn.Kernel.Entry.X64/NovaOryn.Kernel.Entry.X64.csproj',
            TargetArchitecture: targetArchitecture,
            BootProtocol: bootProtocol,
            KernelEntry: 'KMain',
            RuntimePack: 'NovaOryn.RuntimePack.X64.Bootstrap',
            OutputDirectory: 'Artifacts',
            Debugging: {
                EnableOnlyInDebugConfiguration: true,
                ConfiguredFeatures: [...configuration.debugging]
            }
        }, null, 2) + '\n';
    }

    protected featureSource(configuration: NovaOrynProjectConfiguration, project: GeneratedProject): string {
        const namespaceName = `${this.namespace(configuration.name)}.${this.namespace(project.id)}`;
        if (project.kind === 'test') {
            return `namespace ${namespaceName};\n\npublic static class Program\n{\n    public static int Main()\n    {\n        // Individual NovaOryn test program: ${project.description}.\n        return 0;\n    }\n}\n`;
        }
        return `namespace ${namespaceName};\n\npublic static class GeneratedFeature\n{\n    public static string Id() => ${JSON.stringify(project.id)};\n    public static string Description() => ${JSON.stringify(project.description)};\n}\n`;
    }

    protected solutionFile(configuration: NovaOrynProjectConfiguration, projects: GeneratedProject[]): string {
        const lines = ['<Solution>'];
        for (const project of projects) {
            const safePath = project.relativePath.replace(/\//g, '\\');
            const projectFileName = `${this.safeSegment(configuration.name)}.${this.safeSegment(project.id)}.csproj`;
            lines.push(`  <Project Path="${safePath}\\${projectFileName}" />`);
        }
        lines.push('</Solution>', '');
        return lines.join('\n');
    }

    protected async refreshSdkBridge(projectRoot: string): Promise<void> {
        const configurationPath = path.join(projectRoot, 'NovaOryn.json');
        const configuration = JSON.parse(await fs.readFile(configurationPath, 'utf8')) as NovaOrynProjectConfiguration;
        await fs.writeFile(path.join(projectRoot, 'NovaOrynProject.json'), this.sdkProjectManifest(configuration), 'utf8');
        await fs.writeFile(path.join(projectRoot, 'Build.bat'), this.buildBatch(), 'utf8');
        await fs.writeFile(path.join(projectRoot, 'Run.bat'), this.runBatch(configuration), 'utf8');

        const kernelPath = path.join(projectRoot, 'Kernel', 'Kernel.cs');
        try {
            const currentKernel = await fs.readFile(kernelPath, 'utf8');
            if (currentKernel.includes('NovaOryn IDE generated only the components selected in NovaOryn.json.') &&
                currentKernel.includes('public static int KMain()')) {
                await fs.writeFile(kernelPath, this.kernelSource(configuration), 'utf8');
            }
        } catch {
            // A missing user kernel is left for the SDK project creator to repair.
        }
    }

    protected sdkToolchainBootstrapLines(): string[] {
        return [
            'set "NOVAORYN_EMBEDDED_SDK=1"',
            'set "NOVAORYN_SDK_TOOLCHAIN_READY=%NOVAORYN_SDK%\\.toolchain\\.novaoryn-embedded-ready"',
            'set "NOVAORYN_SDK_TOOLCHAIN_INSTALL=0"',
            'if not exist "%NOVAORYN_SDK_TOOLCHAIN_READY%" set "NOVAORYN_SDK_TOOLCHAIN_INSTALL=1"',
            'if not exist "%NOVAORYN_SDK%\\.toolchain\\DotNet\\dotnet.exe" set "NOVAORYN_SDK_TOOLCHAIN_INSTALL=1"',
            'if not exist "%NOVAORYN_SDK%\\.toolchain\\LLVM\\bin\\lld-link.exe" set "NOVAORYN_SDK_TOOLCHAIN_INSTALL=1"',
            'if "%NOVAORYN_SDK_TOOLCHAIN_INSTALL%"=="1" (',
            '  echo [INFO] Bundled NovaOryn SDK toolchain is not ready. Installing/verifying it now...',
            '  echo [INFO] This is a first-use operation; later OS builds/runs reuse the installed toolchain.',
            '  call "%NOVAORYN_SDK%\\Install-NovaOrynToolchain.bat"',
            '  if errorlevel 1 (',
            '    echo [FAIL] Bundled NovaOryn SDK toolchain installation/verification failed.',
            '    exit /b 1',
            '  )',
            '  if not exist "%NOVAORYN_SDK%\\.toolchain" mkdir "%NOVAORYN_SDK%\\.toolchain"',
            '  >"%NOVAORYN_SDK_TOOLCHAIN_READY%" echo NovaOryn embedded SDK toolchain ready',
            '  echo [ OK ] Bundled NovaOryn SDK toolchain is ready.',
            ') else (',
            '  echo [ OK ] Bundled NovaOryn SDK toolchain is ready; reusing existing installation.',
            ')'
        ];
    }

    protected buildBatch(): string {
        return this.sdkBatch('Build-NovaOryn.bat', 'build');
    }

    protected runBatch(configuration: NovaOrynProjectConfiguration): string {
        const configuredDebugging = configuration.debugging.join(';');
        const debugFeatureFlags = new Map<string, string>([
            ['serial-log', 'NOVAORYN_DEBUG_SERIAL_LOG'],
            ['kernel-diagnostics', 'NOVAORYN_DEBUG_KERNEL_DIAGNOSTICS'],
            ['symbols', 'NOVAORYN_DEBUG_SYMBOLS'],
            ['panic-dump', 'NOVAORYN_DEBUG_PANIC_DUMP']
        ]);
        const lines = [
            '@echo off',
            `echo [INFO] NovaOryn OS Run launcher generated by NovaOryn IDE ${NOVAORYN_IDE_VERSION}`,
            'setlocal EnableDelayedExpansion',
            `set "NOVAORYN_SDK=${NOVAORYN_SDK_ROOT}"`,
            ...this.sdkToolchainBootstrapLines(),
            'for %%I in ("%~dp0.") do set "NOVAORYN_PROJECT=%%~fI"',
            'set "NOVAORYN_MANIFEST=%NOVAORYN_PROJECT%\\NovaOrynProject.json"',
            'set "NOVAORYN_CONFIGURATION=Release"',
            'set "NOVAORYN_DEBUG_ENABLED=0"',
            `set "NOVAORYN_DEBUG_CONFIGURED=${configuredDebugging}"`,
            'set "NOVAORYN_DEBUG_FEATURES="'
        ];

        for (const environmentName of debugFeatureFlags.values()) {
            lines.push(`set "${environmentName}=0"`);
        }

        lines.push(
            'if /I "%~1"=="Debug" (',
            '  set "NOVAORYN_CONFIGURATION=Debug"'
        );
        if (configuration.debugging.length > 0) {
            lines.push(
                '  set "NOVAORYN_DEBUG_ENABLED=1"',
                `  set "NOVAORYN_DEBUG_FEATURES=${configuredDebugging}"`
            );
            for (const feature of configuration.debugging) {
                const environmentName = debugFeatureFlags.get(feature);
                if (environmentName) {
                    lines.push(`  set "${environmentName}=1"`);
                }
            }
        }
        lines.push(
            ')',
            'if /I "%~1"=="Run" set "NOVAORYN_CONFIGURATION=Release"',
            'echo [INFO] Build configuration: %NOVAORYN_CONFIGURATION%',
            'if "%NOVAORYN_DEBUG_ENABLED%"=="1" (',
            '  echo [INFO] Kernel/OS debugging enabled: %NOVAORYN_DEBUG_FEATURES%',
            ') else (',
            '  if /I "%NOVAORYN_CONFIGURATION%"=="Debug" (',
            '    echo [INFO] Debug build selected, but no Kernel/OS debugging facilities are enabled in NovaOryn.json.',
            '  ) else (',
            '    echo [INFO] Kernel/OS debugging disabled for Release run.',
            '  )',
            ')',
            'if /I "%NOVAORYN_CONFIGURATION%"=="Debug" (',
            '  echo [INFO] Debug build completed by the SDK; NovaOryn IDE will launch QEMU and attach its debugger.',
            `  call "%NOVAORYN_SDK%\\Build-NovaOryn.bat" "%NOVAORYN_MANIFEST%" -Configuration Debug`,
            '  exit /b !ERRORLEVEL!',
            ')',
            this.sdkBatch('Build-NovaOryn.bat', 'run', '-Run -Configuration Release').trimEnd(),
            ''
        );
        return lines.join('\r\n');
    }

    protected sdkBatch(entryPoint: string, operation: string, sdkOperation?: string): string {
        return [
            '@echo off',
            `echo [INFO] NovaOryn OS ${operation} launcher generated by NovaOryn IDE ${NOVAORYN_IDE_VERSION}`,
            'setlocal',
            `set "NOVAORYN_SDK=${NOVAORYN_SDK_ROOT}"`,
            ...this.sdkToolchainBootstrapLines(),
            'for %%I in ("%~dp0.") do set "NOVAORYN_PROJECT=%%~fI"',
            `if not exist "%NOVAORYN_SDK%\\${entryPoint}" (`,
            `  echo [FAIL] NovaOryn SDK ${operation} entry point was not found: %NOVAORYN_SDK%\\${entryPoint}`,
            '  echo [INFO] The NovaOryn SDK must be present in the IDE SDK subfolder: %NOVAORYN_SDK%',
            '  exit /b 1',
            ')',
            'if not exist "%NOVAORYN_PROJECT%\\NovaOryn.json" (',
            '  echo [FAIL] NovaOryn.json was not found in the generated project.',
            '  exit /b 1',
            ')',
            'set "NOVAORYN_MANIFEST=%NOVAORYN_PROJECT%\\NovaOrynProject.json"',
            'if not exist "%NOVAORYN_MANIFEST%" (',
            '  echo [FAIL] NovaOryn SDK project manifest was not found: %NOVAORYN_MANIFEST%',
            '  exit /b 1',
            ')',
            `echo [INFO] NovaOryn SDK: %NOVAORYN_SDK%`,
            `echo [INFO] NovaOryn project: %NOVAORYN_PROJECT%`,
            sdkOperation
                ? `call "%NOVAORYN_SDK%\\${entryPoint}" "%NOVAORYN_MANIFEST%" ${sdkOperation}`
                : `call "%NOVAORYN_SDK%\\${entryPoint}" "%NOVAORYN_MANIFEST%"`,
            'exit /b %ERRORLEVEL%',
            ''
        ].join('\r\n');
    }

    protected projectReadme(configuration: NovaOrynProjectConfiguration, projects: GeneratedProject[]): string {
        const projectList = projects.map(project => `- ${project.id} -> ${project.relativePath} (${project.kind})`).join('\n');
        return `# ${configuration.name}\n\nGenerated by NovaOryn IDE ${NOVAORYN_IDE_VERSION}.\n\nThis project was generated from the authoritative \`NovaOryn.json\` configuration. Only selected subsystems are emitted into the project graph and source tree.\n\n## Core configuration\n\n- Kernel architecture: ${configuration.kernelArchitecture}\n- CPU architecture: ${configuration.targetArchitecture}\n- Boot architecture: ${configuration.bootArchitecture}\n- Memory system: ${configuration.memorySystem}\n- Scheduler: ${configuration.scheduler}\n- Process support: ${configuration.processSupport}\n- Syscall model: ${configuration.syscallModel}\n- SMP: ${configuration.smp}\n- Interrupt model: ${configuration.interruptModel}\n- Safety profile: ${configuration.safetyProfile}\n\n## Generated projects\n\n${projectList}\n\n## SDK integration\n\nNovaOryn.json and NovaOryn.ProjectGraph.json remain the IDE-authoritative configuration and graph. NovaOrynProject.json is the generated compatibility manifest passed to the NovaOryn SDK build pipeline. Build.bat and Run.bat pass that manifest file to the SDK bundled under the NovaOryn IDE installation (\`NovaOrynIDE\\SDK\\Build-NovaOryn.bat\`).\n`;
    }

    protected safeSegment(value: string): string {
        return value.replace(/[^A-Za-z0-9._-]/g, '_').replace(/[.-]+/g, '_');
    }

    protected namespace(value: string): string {
        return value.split(/[^A-Za-z0-9]+/).filter(Boolean).map(part => /^[0-9]/.test(part) ? `_${part}` : part).join('.') || 'NovaOrynGenerated';
    }
}
