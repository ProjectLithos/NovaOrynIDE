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
    NovaOrynDebugExecutionContext,
    NovaOrynDebugVariable,
    NovaOrynDisassemblyInstruction,
    NovaOrynExceptionBreakpointSettings,
    NovaOrynBreakpointRequest,
    NovaOrynExpressionResult,
    NovaOrynMemoryReadResult,
    NovaOrynPageTableInspection,
    NovaOrynPageTableEntry,
    NovaOrynHeapSnapshot,
    NovaOrynHeapBlock,
    NovaOrynCrashDumpSummary,
    NovaOrynCrashDumpResult,
    NovaOrynBreakpointResult,
    NovaOrynRunOutput,
    NovaOrynRunResult,
    NovaOrynTraceEvent,
    NovaOrynBootStage,
    NovaOrynTraceSnapshot,
    NovaOrynTraceSaveResult,
    NovaOrynProfilerSnapshot,
    NovaOrynProfilerFunction,
    NovaOrynProfilerCpu,
    NovaOrynProfilerCounter,
    NovaOrynDriverDescriptor,
    NovaOrynDriverManifest,
    NovaOrynCreateDriverRequest,
    NovaOrynCreateDriverResult,
    NovaOrynTestDescriptor,
    NovaOrynTestRunResult,
    NovaOrynTestOutput,
    NovaOrynTargetProfile,
    NovaOrynTargetState,
    NovaOrynTargetMutationResult,
    NovaOrynAnalyzerSnapshot,
    NovaOrynAnalyzerDiagnostic,
    NovaOrynBinaryDescriptor,
    NovaOrynBinaryInspection,
    NovaOrynBinarySection,
    NovaOrynBinarySymbol,
    NovaOrynMemoryMapSnapshot,
    NovaOrynMemoryMapRegion,
    NovaOrynInterruptSnapshot,
    NovaOrynInterruptVectorInfo,
    NovaOrynInterruptMechanism,
    NovaOrynSyscallSnapshot,
    NovaOrynSyscallEntry,
    NovaOrynSyscallAbi,
    NovaOrynMemoryRegionCategory,
    NovaOrynProjectService
} from '../common/novaoryn-protocol';

const NOVAORYN_IDE_ROOT = process.env.NOVAORYN_IDE_ROOT
    ? path.resolve(process.env.NOVAORYN_IDE_ROOT)
    : path.resolve(__dirname, '..', '..', '..', '..');
const NOVAORYN_SDK_ROOT = path.join(NOVAORYN_IDE_ROOT, 'SDK');
const NOVAORYN_IDE_VERSION = '0.3.0';

class GdbRspClient {
    protected socket: net.Socket | undefined;
    protected buffer = Buffer.alloc(0);
    protected pending: { resolve: (value: string) => void; reject: (error: Error) => void; timer: NodeJS.Timeout; consoleOutput: string[] } | undefined;

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
            this.pending = { resolve, reject, timer, consoleOutput: [] };
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
            if (payload.startsWith('O') && this.pending && /^[0-9a-fA-F]*$/.test(payload.slice(1)) && payload.length % 2 === 1) {
                try { this.pending.consoleOutput.push(Buffer.from(payload.slice(1), 'hex').toString('utf8')); } catch { }
                continue;
            }
            if (this.pending) {
                const pending = this.pending;
                this.pending = undefined;
                clearTimeout(pending.timer);
                pending.resolve(pending.consoleOutput.length > 0 && payload === 'OK' ? pending.consoleOutput.join('') : payload);
            }
        }
    }
}


class NovaOrynExpressionParser {
    protected readonly tokens: string[];
    protected index = 0;

    constructor(
        expression: string,
        protected readonly resolveIdentifier: (name: string) => Promise<bigint | undefined>,
        protected readonly readPointer: (address: bigint) => Promise<bigint>
    ) {
        this.tokens = this.tokenize(expression);
    }

    async evaluate(): Promise<bigint> {
        if (this.tokens.length === 0) { throw new Error('Expression is empty.'); }
        const value = await this.parseLogicalOr();
        if (this.index !== this.tokens.length) {
            throw new Error(`Unexpected token "${this.tokens[this.index]}".`);
        }
        return value;
    }

    protected tokenize(expression: string): string[] {
        const tokens: string[] = [];
        let i = 0;
        while (i < expression.length) {
            const ch = expression[i];
            if (/\s/.test(ch)) { i++; continue; }
            const two = expression.slice(i, i + 2);
            if (['||','&&','==','!=','<=','>=','<<','>>'].includes(two)) { tokens.push(two); i += 2; continue; }
            if ('()+-*/%~!<>&|^[]'.includes(ch)) { tokens.push(ch); i++; continue; }
            if (/[0-9]/.test(ch)) {
                let j = i + 1;
                if (ch === '0' && /[xX]/.test(expression[j] ?? '')) {
                    j++;
                    while (/[0-9a-fA-F]/.test(expression[j] ?? '')) { j++; }
                } else {
                    while (/[0-9]/.test(expression[j] ?? '')) { j++; }
                }
                tokens.push(expression.slice(i, j)); i = j; continue;
            }
            if (/[A-Za-z_$]/.test(ch)) {
                let j = i + 1;
                while (/[A-Za-z0-9_.$]/.test(expression[j] ?? '')) { j++; }
                tokens.push(expression.slice(i, j)); i = j; continue;
            }
            throw new Error(`Unsupported character "${ch}" in expression.`);
        }
        return tokens;
    }

    protected peek(value?: string): boolean {
        const token = this.tokens[this.index];
        return value === undefined ? token !== undefined : token === value;
    }

    protected take(): string {
        const token = this.tokens[this.index++];
        if (token === undefined) { throw new Error('Unexpected end of expression.'); }
        return token;
    }

    protected async parseLogicalOr(): Promise<bigint> {
        let value = await this.parseLogicalAnd();
        while (this.peek('||')) { this.take(); const rhs = await this.parseLogicalAnd(); value = value !== 0n || rhs !== 0n ? 1n : 0n; }
        return value;
    }
    protected async parseLogicalAnd(): Promise<bigint> {
        let value = await this.parseBitwiseOr();
        while (this.peek('&&')) { this.take(); const rhs = await this.parseBitwiseOr(); value = value !== 0n && rhs !== 0n ? 1n : 0n; }
        return value;
    }
    protected async parseBitwiseOr(): Promise<bigint> {
        let value = await this.parseBitwiseXor();
        while (this.peek('|')) { this.take(); value |= await this.parseBitwiseXor(); }
        return value;
    }
    protected async parseBitwiseXor(): Promise<bigint> {
        let value = await this.parseBitwiseAnd();
        while (this.peek('^')) { this.take(); value ^= await this.parseBitwiseAnd(); }
        return value;
    }
    protected async parseBitwiseAnd(): Promise<bigint> {
        let value = await this.parseEquality();
        while (this.peek('&')) { this.take(); value &= await this.parseEquality(); }
        return value;
    }
    protected async parseEquality(): Promise<bigint> {
        let value = await this.parseRelational();
        while (this.peek('==') || this.peek('!=')) {
            const op = this.take(); const rhs = await this.parseRelational();
            value = op === '==' ? (value === rhs ? 1n : 0n) : (value !== rhs ? 1n : 0n);
        }
        return value;
    }
    protected async parseRelational(): Promise<bigint> {
        let value = await this.parseShift();
        while (['<','<=','>','>='].includes(this.tokens[this.index] ?? '')) {
            const op = this.take(); const rhs = await this.parseShift();
            value = op === '<' ? (value < rhs ? 1n : 0n)
                : op === '<=' ? (value <= rhs ? 1n : 0n)
                : op === '>' ? (value > rhs ? 1n : 0n)
                : (value >= rhs ? 1n : 0n);
        }
        return value;
    }
    protected async parseShift(): Promise<bigint> {
        let value = await this.parseAdditive();
        while (this.peek('<<') || this.peek('>>')) {
            const op = this.take(); const rhs = await this.parseAdditive();
            const shift = BigInt.asUintN(64, rhs);
            if (shift > 63n) { throw new Error('Shift count must be between 0 and 63.'); }
            value = op === '<<' ? value << shift : value >> shift;
        }
        return value;
    }
    protected async parseAdditive(): Promise<bigint> {
        let value = await this.parseMultiplicative();
        while (this.peek('+') || this.peek('-')) { const op = this.take(); const rhs = await this.parseMultiplicative(); value = op === '+' ? value + rhs : value - rhs; }
        return value;
    }
    protected async parseMultiplicative(): Promise<bigint> {
        let value = await this.parseUnary();
        while (this.peek('*') || this.peek('/') || this.peek('%')) {
            const op = this.take(); const rhs = await this.parseUnary();
            if ((op === '/' || op === '%') && rhs === 0n) { throw new Error('Division by zero.'); }
            value = op === '*' ? value * rhs : op === '/' ? value / rhs : value % rhs;
        }
        return value;
    }
    protected async parseUnary(): Promise<bigint> {
        if (this.peek('!')) { this.take(); return (await this.parseUnary()) === 0n ? 1n : 0n; }
        if (this.peek('~')) { this.take(); return ~(await this.parseUnary()); }
        if (this.peek('-')) { this.take(); return -(await this.parseUnary()); }
        if (this.peek('+')) { this.take(); return await this.parseUnary(); }
        return this.parsePrimary();
    }
    protected async parsePrimary(): Promise<bigint> {
        if (this.peek('(')) {
            this.take(); const value = await this.parseLogicalOr();
            if (!this.peek(')')) { throw new Error('Missing closing parenthesis.'); }
            this.take(); return value;
        }
        if (this.peek('[')) {
            this.take(); const address = await this.parseLogicalOr();
            if (!this.peek(']')) { throw new Error('Missing closing bracket in memory expression.'); }
            this.take(); return this.readPointer(BigInt.asUintN(64, address));
        }
        const token = this.take();
        if (/^0x[0-9a-f]+$/i.test(token)) { return BigInt(token); }
        if (/^[0-9]+$/.test(token)) { return BigInt(token); }
        if (token.toLowerCase() === 'true') { return 1n; }
        if (token.toLowerCase() === 'false') { return 0n; }
        const resolved = await this.resolveIdentifier(token.replace(/^\$/, '').toLowerCase());
        if (resolved === undefined) { throw new Error(`Unknown identifier "${token}". Current expressions support x64 registers, active named NativeAOT locals/arguments, integer literals, operators and [address] 64-bit memory reads.`); }
        return resolved;
    }
}

interface SourceBreakpoint {
    sourcePath: string;
    line: number;
    resolvedLine: number;
    address: string;
    condition?: string;
    hitCondition?: string;
    hitCount: number;
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
    image?: string;
    pdb?: string;
    anchor: { symbol: string; linkedAddress: string; resumeSymbol?: string; resumeLinkedAddress?: string; transport?: string };
    entries: NativeSourceLine[];
}

interface NativeVariableLocation {
    name: string;
    kind: 'local' | 'argument';
    functionStart: bigint;
    functionEnd: bigint;
    rangeStart?: bigint;
    rangeEnd?: bigint;
    register?: string;
    baseRegister?: string;
    offset?: bigint;
    typeName?: string;
}


interface NativeGlobalSymbol {
    name: string;
    linkedAddress: bigint;
}

interface PeSectionInfo {
    virtualAddress: number;
    virtualSize: number;
    rawOffset: number;
    rawSize: number;
}

interface PeUnwindEntry {
    beginRva: number;
    endRva: number;
    unwindRva: number;
}

interface PeUnwindTable {
    imageBase: bigint;
    bytes: Buffer;
    sections: PeSectionInfo[];
    entries: PeUnwindEntry[];
}

interface PeImageLayout {
    imageBase: bigint;
    sections: Map<number, bigint>;
}

interface StepPlan {
    kind: 'step-into' | 'step-over' | 'step-out';
    sourcePath?: string;
    line?: number;
    machineSteps: number;
    temporaryAddress?: bigint;
}

interface RunSession {
    sessionId: string;
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
    requestedBreakpoints: NovaOrynBreakpointRequest[];
    breakpointResults: NovaOrynBreakpointResult[];
    nativeDebugMap?: NativeDebugMap;
    relocationDelta?: bigint;
    preparingAnchor?: boolean;
    anchorStopResolve?: () => void;
    internalPause?: boolean;
    lastBreakpoint?: SourceBreakpoint;
    stepPlan?: StepPlan;
    exceptionBreakpoints: NovaOrynExceptionBreakpointSettings;
    exceptionBreakpointAddress?: bigint;
    panicBreakpointAddress?: bigint;
    nativeVariables?: NativeVariableLocation[];
    nativeVariablesMessage?: string;
    selectedThreadId?: string;
    unwindTable?: PeUnwindTable;
    unwindTableLoaded?: boolean;
    nativeGlobals?: NativeGlobalSymbol[];
    nativeGlobalsLoaded?: boolean;
    serialLogPath?: string;
    serialLogOffset?: number;
    startedAtMs: number;
    telemetryBuffer: string;
    traceEvents: NovaOrynTraceEvent[];
    bootStages: Map<string, NovaOrynBootStage>;
    currentBootStage?: string;
    lastBootMilestoneMs?: number;
    profileSamples: Map<string, { samples: number; totalDurationMs: number; category: string }>;
    profileCpuSamples: Map<number, { samples: number; busySamples: number }>;
    profileCounters: Map<string, { category: string; count: number; totalDurationMs: number }>;
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
    protected readonly testRuns = new Map<string, { output: string; complete: boolean; exitCode?: number; error?: string }>();
    protected readonly telemetryArchives = new Map<string, { trace: NovaOrynTraceSnapshot; profiler: NovaOrynProfilerSnapshot }>();

    async getSdkApiSiteUrl(): Promise<string> {
        const indexPath = path.join(NOVAORYN_SDK_ROOT, 'docs', 'site', 'index.html');
        await fs.access(indexPath);
        return pathToFileURL(indexPath).toString();
    }

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



    async listDrivers(projectPath: string): Promise<NovaOrynDriverDescriptor[]> {
        const projectRoot = path.resolve(projectPath);
        if (!this.isOperatingSystemPath(projectRoot)) return [];
        const result: NovaOrynDriverDescriptor[] = [];
        const configurationResult = await this.readProjectConfiguration(projectRoot);
        const configured = configurationResult.success && configurationResult.configuration
            ? [...configurationResult.configuration.drivers, ...configurationResult.configuration.storageControllers, ...configurationResult.configuration.networkDrivers, ...configurationResult.configuration.input, ...configurationResult.configuration.graphics]
            : [];
        for (const name of Array.from(new Set(configured)).sort((a, b) => a.localeCompare(b))) {
            result.push({ id: `configured:${name.toLowerCase()}`, name, projectPath: projectRoot, source: 'configured', kind: 'configured', configured: true });
        }
        const roots = [path.join(projectRoot, 'Drivers'), path.join(projectRoot, 'Kernel', 'Drivers'), path.join(projectRoot, 'DriverProjects')];
        for (const driversRoot of roots) {
            const entries = await fs.readdir(driversRoot, { withFileTypes: true }).catch(() => [] as import('fs').Dirent[]);
            for (const entry of entries) {
                if (!entry.isDirectory()) continue;
                const folder = path.join(driversRoot, entry.name);
                const manifestPath = path.join(folder, 'NovaOryn.Driver.json');
                const projectFiles = (await fs.readdir(folder).catch(() => [] as string[])).filter(name => name.toLowerCase().endsWith('.csproj'));
                if (!projectFiles.length) continue;
                let manifest: NovaOrynDriverManifest | undefined;
                try { manifest = JSON.parse(await fs.readFile(manifestPath, 'utf8')) as NovaOrynDriverManifest; } catch { }
                const name = manifest?.name || entry.name;
                result.push({ id: `os:${folder.toLowerCase()}`, name, projectPath: path.join(folder, projectFiles[0]), manifestPath: manifest ? manifestPath : undefined, source: 'os', kind: manifest?.kind ?? 'platform', configured: configured.some(item => item.toLowerCase() === name.toLowerCase()), manifest });
            }
        }
        const unique = new Map<string, NovaOrynDriverDescriptor>();
        for (const item of result) unique.set(`${item.source}:${item.name.toLowerCase()}`, item);
        return [...unique.values()].sort((a, b) => Number(b.source === 'os') - Number(a.source === 'os') || a.name.localeCompare(b.name));
    }

    async createDriver(projectPath: string, request: NovaOrynCreateDriverRequest): Promise<NovaOrynCreateDriverResult> {
        try {
            const projectRoot = path.resolve(projectPath);
            if (!this.isOperatingSystemPath(projectRoot)) return { success: false, error: `Driver projects can only be created beneath ${NOVAORYN_OS_ROOT}.` };
            await fs.access(path.join(projectRoot, 'NovaOryn.json'));
            const safeName = this.safeSegment((request.name || '').trim());
            if (!safeName || safeName.length < 2) return { success: false, error: 'Enter a driver name containing at least two letters or numbers.' };
            const target = path.join(projectRoot, 'DriverProjects', safeName);
            const relative = path.relative(projectRoot, target);
            if (relative.startsWith('..') || path.isAbsolute(relative)) return { success: false, error: 'Invalid driver project path.' };
            try { await fs.access(target); return { success: false, error: `Driver project ${safeName} already exists.` }; } catch { }
            await fs.mkdir(target, { recursive: true });
            const sdkContract = await this.readSdkContractVersions();
            const capabilities = Array.from(new Set((request.capabilities ?? []).filter(value => ['mmio','pio','interrupts','msi','msix','dma','timers'].includes(value))));
            const manifest: NovaOrynDriverManifest = { schemaVersion: 1, name: safeName, kind: request.kind, version: '0.1.0', sdkApiVersion: sdkContract.apiVersion, driverAbiVersion: sdkContract.driverAbiVersion, capabilities, description: request.description?.trim() || undefined };
            if (request.kind === 'pci') { manifest.vendorId = this.normaliseHexId(request.vendorId); manifest.deviceId = this.normaliseHexId(request.deviceId); }
            if (request.kind === 'usb') { manifest.usbVendorId = this.normaliseHexId(request.usbVendorId); manifest.usbProductId = this.normaliseHexId(request.usbProductId); }
            if (request.kind === 'virtio' && Number.isInteger(request.virtioDeviceId)) manifest.virtioDeviceId = Math.max(0, Number(request.virtioDeviceId));
            const manifestPath = path.join(target, 'NovaOryn.Driver.json');
            const projectFile = path.join(target, `${safeName}.csproj`);
            await fs.writeFile(manifestPath, JSON.stringify(manifest, null, 2) + '\n', 'utf8');
            await fs.writeFile(projectFile, this.driverProjectFile(safeName), 'utf8');
            await fs.writeFile(path.join(target, 'Driver.cs'), this.driverSource(safeName, manifest), 'utf8');
            await fs.writeFile(path.join(target, 'README.md'), this.driverReadme(manifest), 'utf8');
            let testProjectPath: string | undefined;
            if (request.createTestProject) {
                const testDir = path.join(projectRoot, 'Tests', `${safeName}.Driver.Tests`);
                await fs.mkdir(testDir, { recursive: true });
                testProjectPath = path.join(testDir, `${safeName}.Driver.Tests.csproj`);
                const relDriver = path.relative(testDir, projectFile).replace(/\\/g, '/');
                await fs.writeFile(testProjectPath, this.driverTestProjectFile(safeName, relDriver), 'utf8');
                await fs.writeFile(path.join(testDir, 'Program.cs'), this.driverTestSource(safeName), 'utf8');
            }
            return { success: true, projectPath: projectFile, manifestPath, testProjectPath };
        } catch (error) { return { success: false, error: error instanceof Error ? error.message : String(error) }; }
    }

    protected targetFile(projectRoot: string): string { return path.join(projectRoot, 'NovaOryn.Targets.json'); }

    protected defaultTargetState(configuration?: NovaOrynProjectConfiguration): NovaOrynTargetState {
        const architecture = configuration?.targetArchitecture ?? 'x86_64';
        const target: NovaOrynTargetProfile = {
            schemaVersion: 1,
            id: `qemu-${architecture}`,
            name: architecture === 'x86_64' ? 'QEMU x64' : `QEMU ${architecture}`,
            kind: 'qemu', architecture,
            qemu: { cpuCount: Math.max(1, Math.ceil(os.cpus().length / 2)), memoryMiB: 512, machine: architecture === 'x86_64' ? 'q35' : 'virt', accelerator: 'tcg', display: 'sdl' }
        };
        return { schemaVersion: 1, activeTargetId: target.id, targets: [target] };
    }

    protected async readTargetState(projectRoot: string): Promise<NovaOrynTargetState> {
        const config = await this.readProjectConfiguration(projectRoot);
        const fallback = this.defaultTargetState(config.configuration);
        try {
            const parsed = JSON.parse(await fs.readFile(this.targetFile(projectRoot), 'utf8')) as NovaOrynTargetState;
            if (parsed.schemaVersion !== 1 || !Array.isArray(parsed.targets) || parsed.targets.length === 0) return fallback;
            const targets = parsed.targets.filter(item => item && item.schemaVersion === 1 && typeof item.id === 'string' && typeof item.name === 'string');
            if (targets.length === 0) return fallback;
            const activeTargetId = targets.some(item => item.id === parsed.activeTargetId) ? parsed.activeTargetId : targets[0].id;
            return { schemaVersion: 1, activeTargetId, targets };
        } catch {
            await fs.writeFile(this.targetFile(projectRoot), JSON.stringify(fallback, null, 2) + '\n', 'utf8').catch(() => undefined);
            return fallback;
        }
    }

    protected validateTarget(target: NovaOrynTargetProfile): string | undefined {
        if (!target || target.schemaVersion !== 1) return 'Unsupported NovaOryn target schema.';
        if (!/^[a-z0-9][a-z0-9._-]{0,63}$/i.test(target.id || '')) return 'Target ID must contain only letters, numbers, dot, underscore and dash.';
        if (!(target.name || '').trim()) return 'Target name is required.';
        if (!['qemu','physical','remote'].includes(target.kind)) return 'Unsupported target kind.';
        if (!['x86_64','arm64','riscv64'].includes(target.architecture)) return 'Unsupported target architecture.';
        if (target.kind === 'qemu') {
            if (!target.qemu) return 'QEMU settings are required.';
            if (!Number.isInteger(target.qemu.cpuCount) || target.qemu.cpuCount < 1 || target.qemu.cpuCount > 256) return 'QEMU CPU count must be between 1 and 256.';
            if (!Number.isInteger(target.qemu.memoryMiB) || target.qemu.memoryMiB < 64 || target.qemu.memoryMiB > 1048576) return 'QEMU RAM must be between 64 MiB and 1 TiB.';
        }
        return undefined;
    }

    async analyzeOperatingSystem(projectPath: string): Promise<NovaOrynAnalyzerSnapshot> {
        const projectRoot = path.resolve(projectPath);
        const diagnostics: NovaOrynAnalyzerDiagnostic[] = [];
        let filesAnalyzed = 0;
        const activeTarget = await this.getActiveTarget(projectRoot).catch(() => undefined);
        if (!this.isOperatingSystemPath(projectRoot)) {
            return { schemaVersion: 1, analyzedAtUtc: new Date().toISOString(), projectPath: projectRoot, filesAnalyzed: 0, diagnostics: [], errorCount: 0, warningCount: 0, infoCount: 0 };
        }

        const add = (code: string, severity: NovaOrynAnalyzerDiagnostic['severity'], category: NovaOrynAnalyzerDiagnostic['category'], message: string, filePath: string, line: number, column: number, rule: string): void => {
            diagnostics.push({ code, severity, category, message, filePath, line, column, rule });
        };
        const location = (text: string, index: number): { line: number; column: number } => {
            const before = text.slice(0, Math.max(0, index));
            const lines = before.split(/\r?\n/);
            return { line: lines.length, column: (lines[lines.length - 1]?.length ?? 0) + 1 };
        };
        const reportPattern = (text: string, regex: RegExp, filePath: string, code: string, severity: NovaOrynAnalyzerDiagnostic['severity'], category: NovaOrynAnalyzerDiagnostic['category'], message: string, rule: string): void => {
            regex.lastIndex = 0;
            let match: RegExpExecArray | null;
            while ((match = regex.exec(text))) {
                const loc = location(text, match.index);
                add(code, severity, category, message, filePath, loc.line, loc.column, rule);
                if (!regex.global) break;
            }
        };
        const sourceFiles: string[] = [];
        const scan = async (directory: string): Promise<void> => {
            const entries = await fs.readdir(directory, { withFileTypes: true }).catch(() => [] as import('fs').Dirent[]);
            for (const entry of entries) {
                if (entry.name === 'bin' || entry.name === 'obj' || entry.name === '.novaoryn' || entry.name === '.git' || entry.name === 'node_modules' || entry.name === 'Sdk' || entry.name === 'SDK') continue;
                const full = path.join(directory, entry.name);
                if (entry.isDirectory()) await scan(full);
                else if (entry.isFile() && entry.name.toLowerCase().endsWith('.cs')) sourceFiles.push(full);
            }
        };
        await scan(projectRoot);

        for (const filePath of sourceFiles) {
            let text: string;
            try { text = await fs.readFile(filePath, 'utf8'); } catch { continue; }
            filesAnalyzed++;
            const relative = path.relative(projectRoot, filePath).replace(/\\/g, '/');
            const lower = relative.toLowerCase();
            const inKernel = lower === 'kernel/kernel.cs' || lower.startsWith('kernel/');
            const inDriver = lower.startsWith('drivers/') || lower.startsWith('driverprojects/') || lower.includes('/drivers/');
            const inUserland = lower.startsWith('userland/') || lower.includes('/userland/');
            const inArchitectureLayer = lower.includes('/arch/') || lower.includes('/architecture/') || lower.includes('/hal/') || lower.startsWith('arch/') || lower.startsWith('hal/');

            if (inUserland) {
                reportPattern(text, /\busing\s+NovaOryn\.Kernel(?:\.|\s*;)/g, filePath, 'NOA1001', 'error', 'boundary', 'Userland code must not reference NovaOryn.Kernel assemblies directly; use a syscall/service contract.', 'kernel-userland-boundary');
                reportPattern(text, /\bunsafe\b|\b(?:byte|sbyte|short|ushort|int|uint|long|ulong|void)\s*\*/g, filePath, 'NOA1002', 'warning', 'userland-safety', 'Unsafe/pointer code in userland bypasses normal NovaOryn isolation expectations and should be justified behind a supported capability/API.', 'unsafe-userland');
            }
            if (inKernel || inDriver) {
                reportPattern(text, /\bThread\.Sleep\s*\(|\bTask\.Delay\s*\(/g, filePath, 'NOA2001', 'error', 'kernel-safety', 'Blocking managed sleep/delay is not valid in kernel or driver code; use NovaOryn timers/scheduler primitives.', 'blocking-kernel-wait');
                reportPattern(text, /\bthrow\s+(?:new\s+)?[A-Za-z_]/g, filePath, 'NOA2002', 'warning', 'kernel-safety', 'Kernel/driver code should return an explicit status/error contract instead of relying on managed exceptions in normal failure paths.', 'kernel-exception-path');
                reportPattern(text, /\basync\s+(?:System\.)?(?:Threading\.Tasks\.)?Task\b|\basync\s+Task\b/g, filePath, 'NOA2003', 'warning', 'kernel-safety', 'Managed async/Task execution is not part of the freestanding kernel scheduling contract unless an SDK subsystem explicitly provides it.', 'kernel-managed-async');
            }
            if (!inArchitectureLayer && !inDriver && (inKernel || lower.startsWith('services/'))) {
                reportPattern(text, /\b(?:PortIO|IoPort|In8|In16|In32|Out8|Out16|Out32)\b/g, filePath, 'NOA3001', 'error', 'architecture', 'Direct I/O-port access leaked outside the architecture/HAL/driver boundary.', 'hardware-access-boundary');
                reportPattern(text, /\bNovaOryn\.Arch\.(?:X64|Arm64|RiscV64)\b/g, filePath, 'NOA3002', 'warning', 'architecture', 'Architecture-specific API referenced from generic OS code; move it behind NovaOryn architecture/HAL contracts.', 'architecture-leakage');
            }
            if (activeTarget?.architecture && activeTarget.architecture !== 'x86_64' && !inArchitectureLayer) {
                reportPattern(text, /\b(?:X64|x86_64|CPUID|MSR|CR0|CR2|CR3|CR4|APIC|x2APIC)\b/g, filePath, 'NOA3003', 'warning', 'architecture', `The active target is ${activeTarget.architecture}, but generic source contains x64-specific implementation vocabulary.`, 'active-target-architecture');
            }

            const interruptMethod = /\b(?:Interrupt|Irq|Isr|Exception)\w*\s*\([^)]*\)\s*(?:=>|\{)/gi;
            let interruptMatch: RegExpExecArray | null;
            while ((interruptMatch = interruptMethod.exec(text))) {
                const start = interruptMatch.index;
                const sample = text.slice(start, Math.min(text.length, start + 1600));
                const allocation = /\bnew\s+[A-Za-z_][A-Za-z0-9_.<>]*\s*(?:\(|\[)/.exec(sample);
                if (allocation) {
                    const loc = location(text, start + allocation.index);
                    add('NOA4001', 'warning', 'interrupt-safety', 'Allocation detected in an interrupt/IRQ/ISR/exception handler. Interrupt paths should avoid heap allocation and unbounded work.', filePath, loc.line, loc.column, 'interrupt-allocation');
                }
            }
        }

        // Driver manifests are authoritative capability declarations. Match obvious SDK surface use
        // against each driver project so undeclared hardware privileges are visible before boot.
        const driverRoot = path.join(projectRoot, 'DriverProjects');
        const driverEntries = await fs.readdir(driverRoot, { withFileTypes: true }).catch(() => [] as import('fs').Dirent[]);
        const capabilityUses: Array<{ capability: string; regex: RegExp; label: string }> = [
            { capability: 'mmio', regex: /\b(?:Mmio|MMIO|MapMmio|MemoryMappedIo)\b/, label: 'MMIO' },
            { capability: 'pio', regex: /\b(?:PortIO|IoPort|In8|In16|In32|Out8|Out16|Out32)\b/, label: 'port I/O' },
            { capability: 'interrupts', regex: /\b(?:Interrupt|Irq|IRQ|Isr|ISR)\b/, label: 'interrupt' },
            { capability: 'msi', regex: /\bMSI\b|\bMsi\b/, label: 'MSI' },
            { capability: 'msix', regex: /\bMSI-X\b|\bMSIX\b|\bMsiX\b|\bMsix\b/, label: 'MSI-X' },
            { capability: 'dma', regex: /\bDMA\b|\bDma\b/, label: 'DMA' },
            { capability: 'timers', regex: /\b(?:KernelTimer|TimerBroker|HighResolutionTimer)\b/, label: 'timer' }
        ];
        for (const entry of driverEntries) {
            if (!entry.isDirectory()) continue;
            const folder = path.join(driverRoot, entry.name);
            const manifestPath = path.join(folder, 'NovaOryn.Driver.json');
            let manifest: NovaOrynDriverManifest | undefined;
            try { manifest = JSON.parse(await fs.readFile(manifestPath, 'utf8')) as NovaOrynDriverManifest; } catch { continue; }
            const declared = new Set(manifest.capabilities ?? []);
            const files = sourceFiles.filter(file => file.toLowerCase().startsWith((folder + path.sep).toLowerCase()));
            for (const filePath of files) {
                const text = await fs.readFile(filePath, 'utf8').catch(() => '');
                for (const use of capabilityUses) {
                    const found = use.regex.exec(text);
                    if (found && !declared.has(use.capability as any)) {
                        const loc = location(text, found.index);
                        add('NOA5001', 'error', 'driver-capability', `${manifest.name} uses ${use.label} functionality but does not declare the '${use.capability}' capability in NovaOryn.Driver.json.`, filePath, loc.line, loc.column, 'driver-capability-declaration');
                    }
                }
            }
        }

        diagnostics.sort((a, b) => a.filePath.localeCompare(b.filePath) || a.line - b.line || a.code.localeCompare(b.code));
        return {
            schemaVersion: 1,
            analyzedAtUtc: new Date().toISOString(),
            projectPath: projectRoot,
            filesAnalyzed,
            diagnostics,
            errorCount: diagnostics.filter(item => item.severity === 'error').length,
            warningCount: diagnostics.filter(item => item.severity === 'warning').length,
            infoCount: diagnostics.filter(item => item.severity === 'info').length,
            targetArchitecture: activeTarget?.architecture
        };
    }

    async listBinaries(projectPath: string): Promise<NovaOrynBinaryDescriptor[]> {
        const projectRoot = path.resolve(projectPath);
        if (!this.isOperatingSystemPath(projectRoot)) return [];
        const result: NovaOrynBinaryDescriptor[] = [];
        const seen = new Set<string>();
        const extensions = new Set(['.efi','.exe','.dll','.obj','.lib','.pdb','.map','.a','.so']);
        const addFile = async (filePath: string, origin: 'os' | 'sdk'): Promise<void> => {
            const resolved = path.resolve(filePath);
            const key = resolved.toLowerCase(); if (seen.has(key)) return;
            const base = path.basename(resolved);
            const lower = base.toLowerCase();
            const ext = path.extname(lower);
            if (!extensions.has(ext) && lower !== 'novaoryn.debugsymbols.json') return;
            try {
                const stat = await fs.stat(resolved); if (!stat.isFile()) return;
                const kind: NovaOrynBinaryDescriptor['kind'] = lower === 'novaoryn.debugsymbols.json' ? 'debug-map'
                    : ext === '.pdb' ? 'pdb' : ext === '.map' ? 'map' : ext === '.lib' || ext === '.a' ? 'archive'
                    : ['.efi','.exe','.dll'].includes(ext) ? 'pe' : ext === '.obj' ? 'coff' : 'unknown';
                result.push({ id: `${origin}:${resolved.toLowerCase()}`, name: base, path: resolved, origin, kind, sizeBytes: stat.size, modifiedUtc: stat.mtime.toISOString() });
                seen.add(key);
            } catch { }
        };
        const scan = async (directory: string, origin: 'os' | 'sdk', depth: number): Promise<void> => {
            if (depth < 0 || result.length >= 600) return;
            const entries = await fs.readdir(directory, { withFileTypes: true }).catch(() => [] as import('fs').Dirent[]);
            for (const entry of entries) {
                if (result.length >= 600) break;
                const full = path.join(directory, entry.name);
                if (entry.isDirectory()) {
                    if (entry.name === 'node_modules' || entry.name === '.git') continue;
                    await scan(full, origin, depth - 1);
                } else if (entry.isFile()) await addFile(full, origin);
            }
        };
        await scan(path.join(projectRoot, 'Artifacts'), 'os', 5);
        await scan(path.join(projectRoot, 'bin'), 'os', 4);
        await scan(path.join(projectRoot, 'obj'), 'os', 4);
        await scan(path.join(NOVAORYN_SDK_ROOT, 'Artifacts', 'MinimalKernel'), 'sdk', 4);
        return result.sort((a,b) => Number(b.origin === 'os') - Number(a.origin === 'os') || a.name.localeCompare(b.name) || a.path.localeCompare(b.path));
    }

    async inspectBinary(projectPath: string, binaryPath: string, symbolFilter = ''): Promise<NovaOrynBinaryInspection> {
        const projectRoot = path.resolve(projectPath);
        const resolved = path.resolve(binaryPath);
        const allowedRoots = [projectRoot, path.resolve(NOVAORYN_SDK_ROOT, 'Artifacts')];
        if (!this.isOperatingSystemPath(projectRoot) || !allowedRoots.some(root => { const rel = path.relative(root, resolved); return rel === '' || (!rel.startsWith('..') && !path.isAbsolute(rel)); })) {
            return { success: false, sections: [], symbols: [], symbolCount: 0, truncated: false, error: 'Binary inspection is limited to the open NovaOryn OS and bundled SDK artifacts.' };
        }
        const binaries = await this.listBinaries(projectRoot);
        const binary = binaries.find(item => path.resolve(item.path).toLowerCase() === resolved.toLowerCase());
        if (!binary) return { success: false, sections: [], symbols: [], symbolCount: 0, truncated: false, error: 'The selected artifact is no longer available.' };
        try {
            if (binary.kind === 'debug-map') return await this.inspectDebugMap(binary, symbolFilter);
            if (binary.kind === 'pdb') return await this.inspectPdb(binary, symbolFilter);
            if (binary.kind === 'map') return await this.inspectLinkerMap(binary, symbolFilter);
            return await this.inspectNativeBinary(binary, symbolFilter);
        } catch (error) {
            return { success: false, binary, sections: [], symbols: [], symbolCount: 0, truncated: false, error: error instanceof Error ? error.message : String(error) };
        }
    }

    protected binaryFilter(symbols: NovaOrynBinarySymbol[], filter: string, limit = 2000): { symbols: NovaOrynBinarySymbol[]; symbolCount: number; truncated: boolean } {
        const needle = filter.trim().toLowerCase();
        const filtered = needle ? symbols.filter(item => item.name.toLowerCase().includes(needle) || (item.sourcePath ?? '').toLowerCase().includes(needle)) : symbols;
        return { symbols: filtered.slice(0, limit), symbolCount: filtered.length, truncated: filtered.length > limit };
    }

    protected async inspectDebugMap(binary: NovaOrynBinaryDescriptor, filter: string): Promise<NovaOrynBinaryInspection> {
        const raw = JSON.parse(await fs.readFile(binary.path, 'utf8')) as any;
        const rows = Array.isArray(raw.entries) ? raw.entries : [];
        const symbols: NovaOrynBinarySymbol[] = rows.flatMap((entry: any) => {
            const address = entry.linkedAddress ?? entry.LinkedAddress;
            const sourcePath = entry.sourcePath ?? entry.SourcePath;
            const line = entry.line ?? entry.Line;
            if (!address || !sourcePath || !Number.isInteger(line)) return [];
            return [{ name: `${path.basename(String(sourcePath))}:${line}`, address: String(address), kind: 'source-line' as const, sourcePath: String(sourcePath), line: Number(line) }];
        });
        if (raw.anchor?.symbol && raw.anchor?.linkedAddress) symbols.unshift({ name: String(raw.anchor.symbol), address: String(raw.anchor.linkedAddress), kind: 'public' });
        const selected = this.binaryFilter(symbols, filter);
        return { success: true, binary, format: 'NovaOryn Debug Symbol Map v1', architecture: 'x86_64', imageBase: raw.imageBase ? String(raw.imageBase) : undefined, sections: [], ...selected, message: 'Source-line addresses are read from NovaOryn.DebugSymbols.json.' };
    }

    protected async inspectPdb(binary: NovaOrynBinaryDescriptor, filter: string): Promise<NovaOrynBinaryInspection> {
        const tool = path.join(NOVAORYN_SDK_ROOT, '.toolchain', 'LLVM', 'bin', 'llvm-pdbutil.exe');
        if (!(await this.exists(tool))) return { success: true, binary, format: 'PDB', sections: [], symbols: [], symbolCount: 0, truncated: false, message: 'llvm-pdbutil is not installed, so PDB metadata cannot be enumerated.' };
        const output = await this.captureTool(tool, ['dump', '--publics', '--globals', binary.path]);
        const symbols: NovaOrynBinarySymbol[] = [];
        const seen = new Set<string>();
        for (const line of output.text.split(/\r?\n/)) {
            const name = line.match(/`([^`]+)`/)?.[1] ?? line.match(/name\s*=\s*([^,]+)$/i)?.[1]?.trim();
            if (!name || name.length > 500 || seen.has(name)) continue;
            const addr = line.match(/addr\s*=\s*([0-9]+):([0-9A-Fa-f]+)/i);
            const address = addr ? `${addr[1]}:${addr[2]}` : undefined;
            symbols.push({ name, address, kind: 'public' }); seen.add(name);
        }
        const selected = this.binaryFilter(symbols, filter);
        return { success: output.exitCode === 0, binary, format: 'Microsoft Program Database (PDB)', sections: [], ...selected, message: output.exitCode === 0 ? 'Public/global symbols enumerated with llvm-pdbutil.' : output.text.trim().slice(0, 500) };
    }

    protected async inspectLinkerMap(binary: NovaOrynBinaryDescriptor, filter: string): Promise<NovaOrynBinaryInspection> {
        const text = await fs.readFile(binary.path, 'utf8');
        const symbols: NovaOrynBinarySymbol[] = [];
        const re = /^\s*(?:0x)?([0-9A-Fa-f]{8,16})\s+(.+?)\s*$/gm; let m: RegExpExecArray | null;
        while ((m = re.exec(text))) { const name = m[2].trim(); if (name && name.length < 500) symbols.push({ name, address: `0x${m[1]}`, kind: 'unknown' }); }
        const selected = this.binaryFilter(symbols, filter);
        return { success: true, binary, format: 'Linker map', sections: [], ...selected };
    }

    protected async inspectNativeBinary(binary: NovaOrynBinaryDescriptor, filter: string): Promise<NovaOrynBinaryInspection> {
        const bytes = await fs.readFile(binary.path);
        const sections: NovaOrynBinarySection[] = [];
        let format = binary.kind === 'coff' ? 'COFF object' : 'Native binary';
        let architecture: string | undefined;
        let imageBase: string | undefined;
        let entryPoint: string | undefined;
        if (bytes.length >= 0x40 && bytes.readUInt16LE(0) === 0x5a4d) {
            const pe = bytes.readUInt32LE(0x3c);
            if (pe + 24 <= bytes.length && bytes.readUInt32LE(pe) === 0x00004550) {
                format = 'PE/COFF';
                const machine = bytes.readUInt16LE(pe + 4); architecture = machine === 0x8664 ? 'x86_64' : machine === 0xaa64 ? 'arm64' : machine === 0x5064 ? 'riscv64' : `machine 0x${machine.toString(16)}`;
                const sectionCount = bytes.readUInt16LE(pe + 6); const optionalSize = bytes.readUInt16LE(pe + 20); const optional = pe + 24;
                if (optional + optionalSize <= bytes.length) {
                    const magic = bytes.readUInt16LE(optional); const entryRva = bytes.readUInt32LE(optional + 16);
                    if (magic === 0x20b) { const base = bytes.readBigUInt64LE(optional + 24); imageBase = `0x${base.toString(16)}`; entryPoint = `0x${(base + BigInt(entryRva)).toString(16)}`; }
                    else if (magic === 0x10b) { const base = BigInt(bytes.readUInt32LE(optional + 28)); imageBase = `0x${base.toString(16)}`; entryPoint = `0x${(base + BigInt(entryRva)).toString(16)}`; }
                }
                const table = optional + optionalSize;
                for (let i=0;i<sectionCount;i++) { const o=table+i*40; if (o+40>bytes.length) break; const name=bytes.subarray(o,o+8).toString('ascii').replace(/\0.*$/,''); const va=bytes.readUInt32LE(o+12); const vs=bytes.readUInt32LE(o+8); const raw=bytes.readUInt32LE(o+16); const ch=bytes.readUInt32LE(o+36); sections.push({name,virtualAddress:`0x${va.toString(16)}`,virtualSize:vs,rawSize:raw,characteristics:`0x${ch.toString(16).padStart(8,'0')}`}); }
            }
        } else if (bytes.length >= 20) {
            const machine=bytes.readUInt16LE(0); architecture = machine===0x8664?'x86_64':machine===0xaa64?'arm64':undefined;
        }
        const nm = path.join(NOVAORYN_SDK_ROOT, '.toolchain', 'LLVM', 'bin', 'llvm-nm.exe');
        let symbols: NovaOrynBinarySymbol[] = [];
        let message: string | undefined;
        if (await this.exists(nm)) {
            const output = await this.captureTool(nm, ['--print-size','--size-sort','--demangle', binary.path]);
            if (output.exitCode === 0) {
                for (const line of output.text.split(/\r?\n/)) {
                    const m = /^\s*([0-9A-Fa-f]+)\s+([0-9A-Fa-f]+)\s+([A-Za-z?])\s+(.+)$/.exec(line); if (!m) continue;
                    const type=m[3].toUpperCase(); const kind: NovaOrynBinarySymbol['kind'] = ['T','W'].includes(type)?'function':['B','D','R','S','G'].includes(type)?'data':'unknown';
                    symbols.push({name:m[4].trim(),address:`0x${m[1]}`,size:Number.parseInt(m[2],16),kind});
                }
            } else message = output.text.trim().slice(0,500);
        } else message = 'llvm-nm is not installed in the bundled SDK toolchain; binary headers and sections are still available.';
        const selected=this.binaryFilter(symbols,filter);
        return {success:true,binary,format,architecture,imageBase,entryPoint,sections,...selected,message};
    }

    async inspectMemoryMap(projectPath: string): Promise<NovaOrynMemoryMapSnapshot> {
        const capturedAtUtc = new Date().toISOString();
        const empty = (message: string, active = false, paused = false, error?: string): NovaOrynMemoryMapSnapshot => ({
            success: false, active, paused, capturedAtUtc, regions: [], categories: [], reservations: [], message, error
        });
        const projectRoot = path.resolve(projectPath);
        if (!this.isOperatingSystemPath(projectRoot)) return empty('Open a NovaOryn operating system to inspect its memory map.');
        const session = this.latestSessionForProject(projectRoot);
        if (!session || session.mode !== 'debug' || !session.debug?.active || !session.gdb) {
            return empty('Start the operating system in Debug mode, then pause it after KMain to inspect the retained final UEFI memory map.');
        }
        if (!session.debug.paused) return empty('Pause the kernel to read its retained final UEFI memory map safely.', true, false);
        if (session.relocationDelta === undefined) return empty('The debugger has not resolved the relocated kernel image yet.', true, true);
        try {
            const linkedBootContext = await this.findLinkedNativeSymbol(session, 'NovaOrynBootContext');
            if (linkedBootContext === undefined) return empty('NovaOrynBootContext was not found in the linked kernel map. Rebuild the Debug kernel with symbols enabled.', true, true);
            const runtimeBootContext = linkedBootContext + session.relocationDelta;
            const header = await this.readMemoryChunked(session.gdb, runtimeBootContext, 0x98, 0x98);
            if (header.length !== 0x98 || header.readBigUInt64LE(0) !== 0x4E59524F41564F4En) {
                return empty(`The NovaOryn boot-context ABI was not readable at ${this.formatAddress(runtimeBootContext)}.`, true, true);
            }
            const mapAddress = header.readBigUInt64LE(0x38);
            const mapLength = header.readBigUInt64LE(0x40);
            const mapKey = header.readBigUInt64LE(0x48);
            const descriptorSize64 = header.readBigUInt64LE(0x50);
            const descriptorVersion = header.readUInt32LE(0x58);
            const captureAttempts = header.readUInt32LE(0x5c);
            const exitStatus = header.readBigUInt64LE(0x60);
            const finalFlag = header.readBigUInt64LE(0x68);
            if (finalFlag !== 1n || exitStatus !== 0n) return empty(`The retained firmware map is not final (flag=${finalFlag}, ExitBootServices status=${exitStatus}).`, true, true);
            if (descriptorSize64 < 40n || descriptorSize64 > 4096n || (descriptorSize64 & 7n) !== 0n || mapLength === 0n || mapLength > 524288n || mapLength % descriptorSize64 !== 0n) {
                return empty('The retained UEFI memory-map metadata is invalid or outside the supported NovaOryn boot-context limits.', true, true);
            }
            const descriptorSize = Number(descriptorSize64);
            const descriptorCount = Number(mapLength / descriptorSize64);
            const bytes = await this.readMemoryChunked(session.gdb, mapAddress, Number(mapLength), 1024);
            if (bytes.length !== Number(mapLength)) return empty(`Could not read the complete UEFI memory map at ${this.formatAddress(mapAddress)}.`, true, true);
            const regions: NovaOrynMemoryMapRegion[] = [];
            let total = 0n; let usable = 0n; let highest = 0n;
            const categoryBytes = new Map<NovaOrynMemoryRegionCategory, { count: number; bytes: bigint }>();
            for (let index = 0; index < descriptorCount; index++) {
                const offset = index * descriptorSize;
                const firmwareType = bytes.readUInt32LE(offset);
                const physicalStart = bytes.readBigUInt64LE(offset + 8);
                const virtualStart = bytes.readBigUInt64LE(offset + 16);
                const pages = bytes.readBigUInt64LE(offset + 24);
                const attributes = bytes.readBigUInt64LE(offset + 32);
                const byteCount = pages * 4096n;
                const physicalEnd = physicalStart + byteCount;
                const type = this.uefiMemoryType(firmwareType);
                total += byteCount; if (type.category === 'usable') usable += byteCount; if (physicalEnd > highest) highest = physicalEnd;
                const previous = categoryBytes.get(type.category) ?? { count: 0, bytes: 0n };
                previous.count++; previous.bytes += byteCount; categoryBytes.set(type.category, previous);
                regions.push({ index, firmwareType, typeName: type.name, category: type.category, physicalStart: this.formatAddress(physicalStart), physicalEnd: this.formatAddress(physicalEnd), virtualStart: this.formatAddress(virtualStart), pageCount: this.safeNumber(pages), byteCount: this.safeNumber(byteCount), attributes: `0x${attributes.toString(16).padStart(16, '0')}` });
            }
            const reservations = [] as NovaOrynMemoryMapSnapshot['reservations'];
            const addReservation = (name: string, address: bigint, byteCount: bigint, details?: string): void => {
                if (address !== 0n && byteCount !== 0n) reservations.push({ name, physicalStart: this.formatAddress(address), byteCount: this.safeNumber(byteCount), details });
            };
            addReservation('Framebuffer', header.readBigUInt64LE(0x08), header.readBigUInt64LE(0x10), 'UEFI GOP framebuffer');
            addReservation('Bootstrap page-table workspace', header.readBigUInt64LE(0x70), header.readBigUInt64LE(0x78) * 4096n, 'Reserved before ExitBootServices');
            addReservation('AP startup trampoline', header.readBigUInt64LE(0x88), header.readBigUInt64LE(0x90) * 4096n, 'SIPI trampoline below 1 MiB');
            const categories = [...categoryBytes.entries()].map(([category, value]) => ({ category, regionCount: value.count, byteCount: this.safeNumber(value.bytes) })).sort((a,b) => b.byteCount - a.byteCount);
            return {
                success: true, active: true, paused: true, capturedAtUtc, descriptorVersion, descriptorSize, descriptorCount,
                mapKey: `0x${mapKey.toString(16)}`, mapRuntimeAddress: this.formatAddress(mapAddress), captureAttempts,
                totalBytes: this.safeNumber(total), usableBytes: this.safeNumber(usable), highestPhysicalAddress: this.formatAddress(highest),
                regions, categories, reservations,
                message: `Read ${descriptorCount} descriptor(s) directly from the retained final UEFI memory map in the paused NovaOryn kernel.`
            };
        } catch (error) {
            return empty('The memory map could not be read from the paused kernel.', true, true, error instanceof Error ? error.message : String(error));
        }
    }


    async inspectInterrupts(projectPath: string): Promise<NovaOrynInterruptSnapshot> {
        const capturedAtUtc = new Date().toISOString();
        const empty = (message: string, active = false, paused = false, error?: string): NovaOrynInterruptSnapshot => ({
            success: false, active, paused, capturedAtUtc, vectors: [], routes: [], ioApics: [], localApicRegisters: [], message, error
        });
        const projectRoot = path.resolve(projectPath);
        if (!this.isOperatingSystemPath(projectRoot)) return empty('Open a NovaOryn operating system to inspect interrupt routing.');
        const session = this.latestSessionForProject(projectRoot);
        if (!session || session.mode !== 'debug' || !session.debug?.active || !session.gdb) return empty('Start the operating system in Debug mode, then pause it after interrupt initialization.');
        if (!session.debug.paused) return empty('Pause the kernel to read interrupt-controller state safely.', true, false);
        if (session.relocationDelta === undefined) return empty('The debugger has not resolved the relocated kernel image yet.', true, true);
        try {
            await this.ensureNativeGlobalSymbols(session);
            const runtime = (component: string, suffix: string): bigint | undefined => {
                const symbol = this.findKernelGlobal(session, component, suffix);
                return symbol ? symbol.linkedAddress + session.relocationDelta! : undefined;
            };
            const readU8 = async (component: string, suffix: string): Promise<number | undefined> => {
                const address = runtime(component, suffix); if (address === undefined) return undefined;
                const bytes = await this.readMemory(session.gdb!, address, 1); return bytes.length === 1 ? bytes[0] : undefined;
            };
            const readU32 = async (component: string, suffix: string): Promise<number | undefined> => {
                const address = runtime(component, suffix); if (address === undefined) return undefined;
                const bytes = await this.readMemory(session.gdb!, address, 4); return bytes.length === 4 ? bytes.readUInt32LE(0) : undefined;
            };
            const readU64 = async (component: string, suffix: string): Promise<bigint | undefined> => {
                const address = runtime(component, suffix); if (address === undefined) return undefined;
                const bytes = await this.readMemory(session.gdb!, address, 8); return bytes.length === 8 ? bytes.readBigUInt64LE(0) : undefined;
            };
            const dispatchInitialized = (await readU8('KernelInterruptDispatch', '_initialized')) === 1;
            const brokerInitialized = (await readU8('KernelInterruptBroker', '_initialized')) === 1;
            const localApic = (await readU8('KernelInterruptBroker', '_localApic')) === 1;
            const ioApic = (await readU8('KernelInterruptBroker', '_ioApic')) === 1;
            const x2Apic = (await readU8('KernelInterruptBroker', '_x2Apic')) === 1;
            const localApicBase = await readU64('KernelInterruptDispatch', '_localApicBase');
            const routeCount = await readU32('KernelInterruptBroker', '_count') ?? 0;
            const routeCapacity = await readU32('KernelInterruptBroker', '_capacity') ?? 0;
            const ioApicCount = await readU32('KernelInterruptBroker', '_ioApicCount') ?? 0;
            const allocatedPointer = await readU64('KernelInterruptDispatch', '_allocated');
            const callbacksPointer = await readU64('KernelInterruptDispatch', '_callbacks');
            const cookiesPointer = await readU64('KernelInterruptDispatch', '_cookies');
            const allocated = allocatedPointer ? await this.readMemoryChunked(session.gdb, allocatedPointer, 256, 256) : Buffer.alloc(0);
            const callbacks = callbacksPointer ? await this.readMemoryChunked(session.gdb, callbacksPointer, 2048, 512) : Buffer.alloc(0);
            const cookies = cookiesPointer ? await this.readMemoryChunked(session.gdb, cookiesPointer, 2048, 512) : Buffer.alloc(0);
            const exceptionNames = ['Divide by zero','Debug','NMI','Breakpoint','Overflow','Bound range','Invalid opcode','Device not available','Double fault','Coprocessor segment overrun','Invalid TSS','Segment not present','Stack fault','General protection','Page fault','Reserved','x87 floating point','Alignment check','Machine check','SIMD floating point','Virtualisation','Control protection','Reserved','Reserved','Reserved','Reserved','Reserved','Reserved','Hypervisor injection','VMM communication','Security','Reserved'];
            const breakVectors = new Set(session.exceptionBreakpoints.vectors ?? []);
            const vectors: NovaOrynInterruptVectorInfo[] = [];
            let allocatedDynamicVectors = 0;
            for (let vector=0; vector<256; vector++) {
                const dynamic = vector >= 0x40 && vector <= 0xEF;
                const isAllocated = allocated.length === 256 ? allocated[vector] !== 0 : false;
                if (dynamic && isAllocated) allocatedDynamicVectors++;
                let callback: string | undefined; let cookie: string | undefined;
                if (callbacks.length === 2048) { const value=callbacks.readBigUInt64LE(vector*8); if (value) callback=this.formatAddress(value); }
                if (cookies.length === 2048) { const value=cookies.readBigUInt64LE(vector*8); if (value) cookie=`0x${value.toString(16)}`; }
                if (vector < 32 || isAllocated || callback) vectors.push({ vector, hex:`0x${vector.toString(16).padStart(2,'0')}`, kind:vector<32?'exception':dynamic?'dynamic':'system', allocated:isAllocated, callback, cookie, exceptionName:vector<32?exceptionNames[vector]:undefined, breakOnException:vector<32?breakVectors.has(vector):undefined });
            }
            const mechanism = (value: number): NovaOrynInterruptMechanism => { switch (value) { case 1:return 'io-apic'; case 2:return 'msi'; case 3:return 'msi-x'; case 4:return 'local-apic'; case 5:return 'x2apic'; default:return 'none'; } };
            const routes: NovaOrynInterruptSnapshot['routes'] = [];
            const routesPointer = await readU64('KernelInterruptBroker', '_routes');
            if (routesPointer && routeCapacity > 0 && routeCapacity <= 4096) {
                const raw = await this.readMemoryChunked(session.gdb, routesPointer, routeCapacity*48, 768);
                for (let i=0;i<routeCapacity && i*48+48<=raw.length;i++) {
                    const o=i*48; if (raw[o]===0) continue;
                    const segment=raw.readUInt16LE(o+40), bus=raw[o+42], dev=raw[o+43], fn=raw[o+44];
                    routes.push({ handle:`0x${raw.readBigUInt64LE(o+16).toString(16)}`, vector:raw[o+1], mechanism:mechanism(raw[o+2]), device:raw.readUInt32LE(o+4), source:raw.readUInt32LE(o+8), targetProcessor:raw.readUInt32LE(o+12), direct:raw[o+3]!==0, pci:(segment||bus||dev||fn)?`${segment.toString(16).padStart(4,'0')}:${bus.toString(16).padStart(2,'0')}:${dev.toString(16).padStart(2,'0')}.${fn}`:undefined, cookie:`0x${raw.readBigUInt64LE(o+24).toString(16)}` });
                }
            }
            const ioApics: NovaOrynInterruptSnapshot['ioApics'] = [];
            const ioApicsPointer = await readU64('KernelInterruptBroker', '_ioApics');
            if (ioApicsPointer && ioApicCount <= 256) {
                const raw = await this.readMemoryChunked(session.gdb, ioApicsPointer, ioApicCount*16, 512);
                for (let i=0;i<ioApicCount && i*16+16<=raw.length;i++) { const o=i*16, base=raw.readUInt32LE(o+8), max=raw.readUInt32LE(o+12); ioApics.push({index:i,mappedAddress:this.formatAddress(raw.readBigUInt64LE(o)),baseGsi:base,maximumGsi:max,pinCount:max>=base?max-base+1:0}); }
            }
            const localApicRegisters: NovaOrynInterruptSnapshot['localApicRegisters'] = [];
            if (localApic && !x2Apic && localApicBase) {
                for (const [name,off] of [['APIC ID',0x20],['Version',0x30],['Task Priority',0x80],['Processor Priority',0xA0],['Spurious Vector',0xF0],['LVT Timer',0x320],['LVT LINT0',0x350],['LVT LINT1',0x360],['LVT Error',0x370],['Timer Current',0x390],['Timer Divide',0x3E0]] as Array<[string,number]>) {
                    let value: string | undefined; try { const b=await this.readMemory(session.gdb,localApicBase+BigInt(off),4); if(b.length===4)value=`0x${b.readUInt32LE(0).toString(16).padStart(8,'0')}`; } catch { }
                    localApicRegisters.push({name,offset:`0x${off.toString(16)}`,value});
                }
            }
            return { success:true, active:true, paused:true, capturedAtUtc, dispatchInitialized, brokerInitialized, localApic, ioApic, x2Apic, msi:brokerInitialized, msiX:brokerInitialized, localApicBase:localApicBase?this.formatAddress(localApicBase):undefined, routeCount, routeCapacity, ioApicCount, allocatedDynamicVectors, vectors, routes, ioApics, localApicRegisters, message:`Read NovaOryn interrupt dispatch and broker state from paused kernel memory (${routes.length} active route(s), ${allocatedDynamicVectors} allocated dynamic vector(s)).` };
        } catch (error) { return empty('Interrupt/APIC state could not be read from the paused kernel.', true, true, error instanceof Error ? error.message : String(error)); }
    }

    async inspectSyscalls(projectPath: string): Promise<NovaOrynSyscallSnapshot> {
        const root = path.resolve(projectPath);
        const capturedAtUtc = new Date().toISOString();
        const configurationResult = await this.readProjectConfiguration(root);
        const configuredModel = configurationResult.success ? configurationResult.configuration?.syscallModel : undefined;
        const counts = (): Record<NovaOrynSyscallAbi, number> => ({ 'novaoryn-get':0, 'novaoryn-set':0, 'novaoryn-event':0, linux:0, 'windows-nt':0 });
        const builtins = (): NovaOrynSyscallEntry[] => [
            { abi:'novaoryn-get', number:0, encoded:'0x1000000000000000', name:'ABI version', source:'builtin', registered:true, description:'Returns the NovaOryn native syscall ABI version.' },
            { abi:'novaoryn-get', number:1, encoded:'0x1000000000000001', name:'Monotonic time', source:'builtin', registered:true, description:'Returns monotonic nanoseconds.' },
            { abi:'novaoryn-get', number:2, encoded:'0x1000000000000002', name:'Online processor count', source:'builtin', registered:true },
            { abi:'novaoryn-set', number:0, encoded:'0x1100000000000000', name:'Scheduler quantum', source:'builtin', registered:true, description:'Sets the scheduler quantum in nanoseconds.' },
            { abi:'novaoryn-event', number:0, encoded:'0x1200000000000000', name:'Yield', source:'builtin', registered:true, description:'Yields the current processor scheduler context.' },
            { abi:'linux', number:24, name:'sched_yield', source:'builtin', registered:true, description:'Linux-style scheduler yield compatibility syscall.' }
        ];
        const session = this.latestSessionForProject(root);
        if (!session || session.mode !== 'debug' || !session.gdb || !session.debug?.active) {
            return { success:true, active:false, paused:false, capturedAtUtc, configuredModel, registrySlots:64, entries:builtins(), registeredCounts:counts(), message:'Showing configured and built-in syscall contracts. Start Debug and pause the kernel to inspect live registered handlers.' };
        }
        if (!session.debug.paused) {
            return { success:true, active:true, paused:false, capturedAtUtc, configuredModel, registrySlots:64, entries:builtins(), registeredCounts:counts(), message:'Kernel is running. Pause it to read the live syscall registry.' };
        }
        try {
            await this.ensureNativeGlobalSymbols(session);
            if (session.relocationDelta === undefined) throw new Error('Kernel relocation delta is unavailable.');
            const addressOf = (suffix: string): bigint | undefined => {
                const symbol = this.findKernelGlobal(session, 'KernelSystemCalls', suffix);
                return symbol ? symbol.linkedAddress + session.relocationDelta! : undefined;
            };
            const readByte = async (suffix:string):Promise<number|undefined> => { const a=addressOf(suffix); if(a===undefined)return undefined; const b=await this.readMemory(session.gdb!,a,1); return b.length===1?b[0]:undefined; };
            const readU32 = async (suffix:string):Promise<number|undefined> => { const a=addressOf(suffix); if(a===undefined)return undefined; const b=await this.readMemory(session.gdb!,a,4); return b.length===4?b.readUInt32LE(0):undefined; };
            const readU64 = async (suffix:string):Promise<bigint|undefined> => { const a=addressOf(suffix); if(a===undefined)return undefined; return this.readU64(session.gdb!,a); };
            const initialized=(await readByte('_initialized'))===1;
            const smapEnabled=(await readByte('_smapEnabled'))===1;
            const configuredProcessors=await readU32('_configuredProcessors');
            const stackBase=await readU64('_stackBase'), stackTop=await readU64('_stackTop');
            const registryAddress=addressOf('_registry');
            const entries=builtins(); const registeredCounts=counts();
            const abiAtIndex: NovaOrynSyscallAbi[]=['novaoryn-get','novaoryn-set','novaoryn-event','linux','windows-nt'];
            if (registryAddress !== undefined) {
                const raw=await this.readMemoryChunked(session.gdb,registryAddress,64*8*5,1024);
                if(raw.length===64*8*5) {
                    for(let table=0;table<5;table++) for(let number=0;number<64;number++) {
                        const handler=raw.readBigUInt64LE((table*64+number)*8); if(handler===0n) continue;
                        const abi=abiAtIndex[table]; registeredCounts[abi]++;
                        const linked=session.relocationDelta!==undefined?handler-session.relocationDelta:undefined;
                        const source=linked!==undefined&&session.nativeDebugMap?this.resolveSourceLocation(session.nativeDebugMap,linked):undefined;
                        const encoded=abi==='novaoryn-get'?`0x${(0x1000000000000000n+BigInt(number)).toString(16)}`:abi==='novaoryn-set'?`0x${(0x1100000000000000n+BigInt(number)).toString(16)}`:abi==='novaoryn-event'?`0x${(0x1200000000000000n+BigInt(number)).toString(16)}`:undefined;
                        entries.push({abi,number,encoded,name:`Registered ${abi} ${number}`,source:'registered',registered:true,handlerAddress:this.formatAddress(handler),sourcePath:source?.sourcePath,line:source?.line});
                    }
                }
            }
            entries.sort((a,b)=>abiAtIndex.indexOf(a.abi)-abiAtIndex.indexOf(b.abi)||a.number-b.number||a.source.localeCompare(b.source));
            return { success:true,active:true,paused:true,capturedAtUtc,configuredModel,initialized,smapEnabled,configuredProcessors,syscallStackBase:stackBase?this.formatAddress(stackBase):undefined,syscallStackTop:stackTop?this.formatAddress(stackTop):undefined,syscallStackBytes:stackBase&&stackTop&&stackTop>=stackBase?this.safeNumber(stackTop-stackBase):32768,registrySlots:64,entries,registeredCounts,message:`Read KernelSystemCalls and ${Object.values(registeredCounts).reduce((a,b)=>a+b,0)} registered handler(s) from paused kernel memory.` };
        } catch(error) {
            return { success:false,active:true,paused:true,capturedAtUtc,configuredModel,registrySlots:64,entries:builtins(),registeredCounts:counts(),error:error instanceof Error?error.message:String(error),message:'Live syscall registry could not be read.' };
        }
    }

    protected uefiMemoryType(type: number): { name: string; category: NovaOrynMemoryRegionCategory } {
        switch (type) {
            case 0: return { name: 'Reserved', category: 'reserved' };
            case 1: return { name: 'Loader Code', category: 'boot-reclaimable' };
            case 2: return { name: 'Loader Data', category: 'boot-reclaimable' };
            case 3: return { name: 'Boot Services Code', category: 'boot-reclaimable' };
            case 4: return { name: 'Boot Services Data', category: 'boot-reclaimable' };
            case 5: return { name: 'Runtime Services Code', category: 'runtime' };
            case 6: return { name: 'Runtime Services Data', category: 'runtime' };
            case 7: return { name: 'Conventional Memory', category: 'usable' };
            case 8: return { name: 'Unusable Memory', category: 'unusable' };
            case 9: return { name: 'ACPI Reclaim Memory', category: 'acpi-reclaimable' };
            case 10: return { name: 'ACPI NVS Memory', category: 'acpi-nvs' };
            case 11: return { name: 'Memory-mapped I/O', category: 'mmio' };
            case 12: return { name: 'Memory-mapped I/O Port Space', category: 'mmio' };
            case 13: return { name: 'PAL Code', category: 'reserved' };
            case 14: return { name: 'Persistent Memory', category: 'persistent' };
            case 15: return { name: 'Unaccepted Memory', category: 'unaccepted' };
            default: return { name: `Firmware Type ${type}`, category: 'unknown' };
        }
    }

    protected async findLinkedNativeSymbol(session: RunSession, symbolName: string): Promise<bigint | undefined> {
        const image = session.nativeDebugMap?.image ?? path.join(NOVAORYN_SDK_ROOT, 'Artifacts', 'MinimalKernel', 'MinimalKernel.efi');
        const nm = path.join(NOVAORYN_SDK_ROOT, '.toolchain', 'LLVM', 'bin', 'llvm-nm.exe');
        if (await this.exists(nm) && await this.exists(image)) {
            const output = await this.captureTool(nm, ['--numeric-sort', image]);
            if (output.exitCode === 0) {
                for (const line of output.text.split(/\r?\n/)) {
                    const match = /^\s*([0-9a-fA-F]+)\s+[A-Za-z?]\s+(.+?)\s*$/.exec(line);
                    if (match && match[2] === symbolName) return BigInt(`0x${match[1]}`);
                }
            }
        }
        try {
            const mapText = await fs.readFile(path.join(NOVAORYN_SDK_ROOT, 'Artifacts', 'MinimalKernel', 'MinimalKernel.map'), 'utf8');
            for (const line of mapText.split(/\r?\n/)) {
                if (!line.includes(symbolName)) continue;
                const values = Array.from(line.matchAll(/(?:0x)?([0-9a-fA-F]{8,16})/g)).map(match => BigInt(`0x${match[1]}`));
                const linkedAddress = values.find(value => value >= 0x100000000n);
                if (linkedAddress !== undefined) return linkedAddress;
            }
        } catch { }
        return undefined;
    }

    async listTargets(projectPath: string): Promise<NovaOrynTargetState> {
        const root = path.resolve(projectPath);
        if (!this.isOperatingSystemPath(root)) return this.defaultTargetState();
        return this.readTargetState(root);
    }

    async getActiveTarget(projectPath: string): Promise<NovaOrynTargetProfile | undefined> {
        const state = await this.listTargets(projectPath);
        return state.targets.find(item => item.id === state.activeTargetId);
    }

    async saveTarget(projectPath: string, target: NovaOrynTargetProfile): Promise<NovaOrynTargetMutationResult> {
        try {
            const root = path.resolve(projectPath);
            if (!this.isOperatingSystemPath(root)) return { success: false, error: 'Targets can only be saved for a NovaOryn OS workspace.' };
            const invalid = this.validateTarget(target); if (invalid) return { success: false, error: invalid };
            const state = await this.readTargetState(root);
            const index = state.targets.findIndex(item => item.id === target.id);
            if (index >= 0) state.targets[index] = target; else state.targets.push(target);
            if (!state.activeTargetId) state.activeTargetId = target.id;
            await fs.writeFile(this.targetFile(root), JSON.stringify(state, null, 2) + '\n', 'utf8');
            return { success: true, state };
        } catch (error) { return { success: false, error: error instanceof Error ? error.message : String(error) }; }
    }

    async deleteTarget(projectPath: string, targetId: string): Promise<NovaOrynTargetMutationResult> {
        try {
            const root = path.resolve(projectPath); const state = await this.readTargetState(root);
            if (state.targets.length <= 1) return { success: false, state, error: 'A NovaOryn OS must retain at least one target.' };
            state.targets = state.targets.filter(item => item.id !== targetId);
            if (state.targets.length === 0) return { success: false, error: 'A NovaOryn OS must retain at least one target.' };
            if (!state.targets.some(item => item.id === state.activeTargetId)) state.activeTargetId = state.targets[0].id;
            await fs.writeFile(this.targetFile(root), JSON.stringify(state, null, 2) + '\n', 'utf8');
            return { success: true, state };
        } catch (error) { return { success: false, error: error instanceof Error ? error.message : String(error) }; }
    }

    async setActiveTarget(projectPath: string, targetId: string): Promise<NovaOrynTargetMutationResult> {
        try {
            const root = path.resolve(projectPath); const state = await this.readTargetState(root);
            if (!state.targets.some(item => item.id === targetId)) return { success: false, state, error: `Target ${targetId} was not found.` };
            state.activeTargetId = targetId;
            await fs.writeFile(this.targetFile(root), JSON.stringify(state, null, 2) + '\n', 'utf8');
            return { success: true, state };
        } catch (error) { return { success: false, error: error instanceof Error ? error.message : String(error) }; }
    }

    protected isOperatingSystemPath(projectRoot: string): boolean {
        const osRoot = path.resolve(NOVAORYN_OS_ROOT);
        const relative = path.relative(osRoot, projectRoot);
        return !!relative && !relative.startsWith('..') && !path.isAbsolute(relative);
    }

    protected async readSdkContractVersions(): Promise<{ apiVersion: string; driverAbiVersion: string }> {
        try {
            const raw = JSON.parse(await fs.readFile(path.join(NOVAORYN_SDK_ROOT, 'NovaOryn.SdkManifest.json'), 'utf8')) as { apiVersion?: string; abi?: { driver?: string } };
            return { apiVersion: raw.apiVersion || '1.0', driverAbiVersion: raw.abi?.driver || '1.0' };
        } catch { return { apiVersion: '1.0', driverAbiVersion: '1.0' }; }
    }

    protected normaliseHexId(value?: string): string | undefined {
        const cleaned = (value ?? '').trim().replace(/^0x/i, '').replace(/[^0-9a-f]/gi, '').slice(0, 4);
        return cleaned ? `0x${cleaned.toUpperCase().padStart(4, '0')}` : undefined;
    }

    protected driverProjectFile(name: string): string {
        return `<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n    <ImplicitUsings>disable</ImplicitUsings>\n    <Nullable>enable</Nullable>\n    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>\n    <AssemblyName>NovaOryn.Driver.${name}</AssemblyName>\n  </PropertyGroup>\n  <ItemGroup>\n    <ProjectReference Include="..\\..\\Sdk\\NovaOryn.Kernel.Drivers\\NovaOryn.Kernel.Drivers.csproj" />\n  </ItemGroup>\n</Project>\n`;
    }

    protected driverSource(name: string, manifest: NovaOrynDriverManifest): string {
        const ns = `NovaOryn.Driver.${this.namespace(name)}`;
        const match = manifest.kind === 'pci'
            ? `// PCI match: vendor ${manifest.vendorId ?? 'any'}, device ${manifest.deviceId ?? 'any'}`
            : manifest.kind === 'usb' ? `// USB match: VID ${manifest.usbVendorId ?? 'any'}, PID ${manifest.usbProductId ?? 'any'}`
            : manifest.kind === 'virtio' ? `// VirtIO device id: ${manifest.virtioDeviceId ?? 0}` : '// Platform-device driver';
        return `using System;\nusing NovaOryn.Kernel.Drivers;\n\nnamespace ${ns};\n\n/// <summary>${manifest.description || `${name} NovaOryn device driver.`}</summary>\npublic static unsafe class ${this.namespace(name)}Driver\n{\n    ${match}\n    public const string DriverAbiVersion = ${JSON.stringify(manifest.driverAbiVersion)};\n\n    /// <summary>Registers this driver with the NovaOryn driver framework.</summary>\n    public static Boolean Initialize()\n    {\n        // TODO: replace the generic match rule with the device identifiers from NovaOryn.Driver.json.\n        KernelDriverMatchRule rule = new(KernelDeviceBus.Synthetic, false, 0, false, 0, false, 0U, 0U);\n        KernelDriverCallbacks callbacks = new(&Probe, &Start, &Stop, &Remove, &Interrupt);\n        return KernelDrivers.RegisterDriver(rule, callbacks, out _);\n    }\n\n    private static Boolean Probe(KernelDriverDeviceContext* context) => context != null;\n    private static Boolean Start(KernelDriverDeviceContext* context) => context != null;\n    private static Boolean Stop(KernelDriverDeviceContext* context) => context != null;\n    private static Boolean Remove(KernelDriverDeviceContext* context) => context != null;\n    private static Boolean Interrupt(KernelDriverDeviceContext* context, UInt64 cookie) => context != null;\n}\n`;
    }

    protected driverReadme(manifest: NovaOrynDriverManifest): string {
        return `# ${manifest.name}\n\nNovaOryn ${manifest.kind} driver project generated by NovaOryn IDE ${NOVAORYN_IDE_VERSION}.\n\n- SDK API: ${manifest.sdkApiVersion}\n- Driver ABI: ${manifest.driverAbiVersion}\n- Capabilities: ${manifest.capabilities.join(', ') || 'none declared'}\n\nEdit \`NovaOryn.Driver.json\` when changing device identifiers or capabilities. Keep the declared ABI compatible with the SDK manifest.\n`;
    }

    protected driverTestProjectFile(name: string, driverProjectRelative: string): string {
        return `<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n    <OutputType>Exe</OutputType>\n    <ImplicitUsings>disable</ImplicitUsings>\n    <Nullable>enable</Nullable>\n  </PropertyGroup>\n  <ItemGroup><ProjectReference Include="${driverProjectRelative}" /></ItemGroup>\n</Project>\n`;
    }

    protected driverTestSource(name: string): string {
        return `using System;\n\nConsole.WriteLine("NovaOryn driver contract test: ${name}");\nConsole.WriteLine("[ OK ] Driver project and manifest are loadable.");\nreturn 0;\n`;
    }

    async listTests(projectPath: string): Promise<NovaOrynTestDescriptor[]> {
        const projectRoot = path.resolve(projectPath);
        const tests: NovaOrynTestDescriptor[] = [];
        const scan = async (root: string, source: 'os' | 'sdk'): Promise<void> => {
            try {
                const entries = await fs.readdir(root, { withFileTypes: true });
                for (const entry of entries) {
                    if (!entry.isDirectory()) continue;
                    const folder = path.join(root, entry.name);
                    const children = await fs.readdir(folder).catch(() => [] as string[]);
                    const csproj = children.find(name => name.toLowerCase().endsWith('.csproj'));
                    if (!csproj) continue;
                    const projectFile = path.join(folder, csproj);
                    const name = path.basename(csproj, '.csproj');
                    const category = name.replace(/^NovaOryn\./i, '').replace(/\.Tests$/i, '').replace(/[._-]+/g, ' ');
                    tests.push({ id: `${source}:${projectFile.toLowerCase()}`, name, projectPath: projectFile, source, category });
                }
            } catch { }
        };
        await scan(path.join(projectRoot, 'Tests'), 'os');
        await scan(path.join(projectRoot, 'tests'), 'os');
        await scan(path.join(NOVAORYN_SDK_ROOT, 'tests'), 'sdk');
        const unique = new Map<string, NovaOrynTestDescriptor>();
        for (const test of tests) unique.set(test.projectPath.toLowerCase(), test);
        return [...unique.values()].sort((a, b) => a.name.localeCompare(b.name));
    }

    async runTest(projectPath: string, testId: string): Promise<NovaOrynTestRunResult> {
        try {
            const tests = await this.listTests(projectPath);
            const test = tests.find(item => item.id === testId);
            if (!test) return { success: false, error: 'The selected NovaOryn test was not found.' };
            const dotnet = path.join(NOVAORYN_SDK_ROOT, '.toolchain', 'DotNet', 'dotnet.exe');
            await fs.access(dotnet);
            const runId = `test-${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;
            const run = { output: `[INFO] NovaOryn Test Explorer\r\n[INFO] ${test.name}\r\n[INFO] Project: ${test.projectPath}\r\n\r\n`, complete: false } as { output: string; complete: boolean; exitCode?: number; error?: string };
            this.testRuns.set(runId, run);
            const child = spawn(dotnet, ['run', '--project', test.projectPath, '--configuration', 'Debug', '--nologo'], {
                cwd: path.dirname(test.projectPath), windowsHide: true, stdio: ['ignore', 'pipe', 'pipe']
            });
            child.stdout?.setEncoding('utf8'); child.stderr?.setEncoding('utf8');
            child.stdout?.on('data', data => { run.output += data; });
            child.stderr?.on('data', data => { run.output += data; });
            child.on('error', error => { run.error = error.message; run.output += `\r\n[FAIL] ${error.message}\r\n`; run.exitCode = 1; run.complete = true; });
            child.on('close', code => { run.exitCode = code ?? 1; run.output += code === 0 ? '\r\n[ OK ] Test passed.\r\n' : `\r\n[FAIL] Test exited with code ${code ?? -1}.\r\n`; run.complete = true; });
            return { success: true, runId };
        } catch (error) { return { success: false, error: error instanceof Error ? error.message : String(error) }; }
    }

    async readTestOutput(runId: string, offset: number): Promise<NovaOrynTestOutput> {
        const run = this.testRuns.get(runId);
        if (!run) return { text: '', nextOffset: offset, complete: true, exitCode: 1, error: 'Unknown NovaOryn test run.' };
        const safeOffset = Math.max(0, Math.min(offset, run.output.length));
        const text = run.output.slice(safeOffset);
        if (run.complete) this.testRuns.delete(runId);
        return { text, nextOffset: run.output.length, complete: run.complete, exitCode: run.exitCode, error: run.error };
    }


    async runOperatingSystem(projectPath: string, mode: NovaOrynRunMode, breakpoints: NovaOrynBreakpointRequest[] = [], exceptionBreakpoints: NovaOrynExceptionBreakpointSettings = { vectors: [0, 2, 6, 8, 12, 13, 14, 18], breakOnPanic: true }): Promise<NovaOrynRunResult> {
        try {
            const projectRoot = path.resolve(projectPath);
            const osRoot = path.resolve(NOVAORYN_OS_ROOT);
            const relative = path.relative(osRoot, projectRoot);
            if (!relative || relative.startsWith('..') || path.isAbsolute(relative)) {
                return { success: false, error: `NovaOryn Run only accepts operating systems beneath ${NOVAORYN_OS_ROOT}.` };
            }

            await fs.access(path.join(projectRoot, 'NovaOryn.json'));
            await this.refreshSdkBridge(projectRoot);
            const activeTarget = await this.getActiveTarget(projectRoot);
            if (!activeTarget) return { success: false, error: 'NovaOryn Target Manager has no active target.' };
            if (activeTarget.kind !== 'qemu') return { success: false, error: `${activeTarget.name} is a ${activeTarget.kind} target. Physical/remote execution is reserved for NovaOryn IDE item 22; the target is retained and can already be configured.` };
            if (activeTarget.architecture !== 'x86_64') return { success: false, error: `${activeTarget.name} targets ${activeTarget.architecture}. The current bundled NovaOryn build/boot pipeline is x86_64; the Target Manager will retain this target until that architecture backend is installed.` };

            const runPath = path.join(projectRoot, 'Run.bat');
            await fs.access(runPath);

            const modeArgument = mode === 'debug' ? 'Debug' : 'Run';
            const sessionId = `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;
            const session: RunSession = {
                sessionId, output: `[INFO] NovaOryn target: ${activeTarget.name} (${activeTarget.kind}/${activeTarget.architecture})\r\n`, complete: false, mode, projectRoot,
                breakpoints: new Map<string, SourceBreakpoint>(),
                requestedBreakpoints: breakpoints.map(item => ({ sourcePath: path.resolve(item.sourcePath), line: item.line, condition: item.condition?.trim() || undefined, hitCondition: item.hitCondition?.trim() || undefined })),
                breakpointResults: [],
                exceptionBreakpoints: { vectors: Array.from(new Set((exceptionBreakpoints.vectors ?? []).filter(vector => Number.isInteger(vector) && vector >= 0 && vector < 32))), breakOnPanic: !!exceptionBreakpoints.breakOnPanic },
                debug: mode === 'debug' ? { active: false, paused: false, sourceSymbols: false, message: 'Building Debug kernel…' } : undefined,
                startedAtMs: Date.now(), telemetryBuffer: '', traceEvents: [], bootStages: new Map<string, NovaOrynBootStage>(),
                profileSamples: new Map(), profileCpuSamples: new Map(), profileCounters: new Map()
            };
            this.runSessions.set(sessionId, session);

            const child = spawn('cmd.exe', ['/d', '/c', 'call', runPath, modeArgument], {
                cwd: projectRoot,
                detached: false,
                windowsHide: true,
                stdio: ['ignore', 'pipe', 'pipe'],
                env: {
                    ...process.env,
                    NOVAORYN_TARGET_ID: activeTarget.id,
                    NOVAORYN_TARGET_NAME: activeTarget.name,
                    NOVAORYN_TARGET_KIND: activeTarget.kind,
                    NOVAORYN_TARGET_ARCH: activeTarget.architecture,
                    NOVAORYN_TARGET_CPUS: String(activeTarget.qemu?.cpuCount ?? 1),
                    NOVAORYN_TARGET_MEMORY_MIB: String(activeTarget.qemu?.memoryMiB ?? 512),
                    NOVAORYN_TARGET_MACHINE: activeTarget.qemu?.machine ?? 'q35',
                    NOVAORYN_TARGET_ACCELERATOR: activeTarget.qemu?.accelerator ?? 'tcg',
                    NOVAORYN_TARGET_DISPLAY: activeTarget.qemu?.display ?? 'sdl'
                }
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

        if (session.serialLogPath) {
            try {
                const serial = await fs.readFile(session.serialLogPath, 'utf8');
                const serialOffset = Math.max(0, Math.min(session.serialLogOffset ?? 0, serial.length));
                if (serial.length > serialOffset) {
                    const fresh = serial.slice(serialOffset).replace(/\r/g, '');
                    this.ingestTelemetry(session, fresh);
                    const labelled = fresh
                        .split('\n')
                        .filter((line, index, all) => line.length > 0 || index < all.length - 1)
                        .map(line => `[KERNEL] ${line}`)
                        .join('\n');
                    if (labelled) {
                        session.output += `\n${labelled}\n`;
                    }
                    session.serialLogOffset = serial.length;
                }
            } catch { }
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
            this.telemetryArchives.set(session.projectRoot, { trace: this.traceSnapshotForSession(session), profiler: this.profilerSnapshotForSession(session) });
            setTimeout(() => this.runSessions.delete(sessionId), 60_000);
        }
        return result;
    }

    async readTraceSnapshot(projectPath: string): Promise<NovaOrynTraceSnapshot> {
        const session = this.latestSessionForProject(projectPath);
        if (session) { return this.traceSnapshotForSession(session); }
        return this.telemetryArchives.get(path.resolve(projectPath))?.trace ?? { active: false, capturedAtUtc: new Date().toISOString(), elapsedMs: 0, events: [], bootStages: [], message: 'Run or Debug the operating system to collect kernel trace data.' };
    }

    async saveTrace(projectPath: string): Promise<NovaOrynTraceSaveResult> {
        try {
            const root = path.resolve(projectPath);
            const snapshot = await this.readTraceSnapshot(root);
            if (snapshot.events.length === 0 && snapshot.bootStages.length === 0) return { success: false, error: 'No NovaOryn trace data has been collected yet.' };
            const directory = path.join(root, '.novaoryn', 'traces');
            await fs.mkdir(directory, { recursive: true });
            const stamp = new Date().toISOString().replace(/[:.]/g, '-');
            const filePath = path.join(directory, `NovaOryn-Trace-${stamp}.notrace.json`);
            await fs.writeFile(filePath, JSON.stringify({ schema: 'novaoryn-trace/v1', ...snapshot }, undefined, 2), 'utf8');
            return { success: true, path: filePath };
        } catch (error) { return { success: false, error: error instanceof Error ? error.message : String(error) }; }
    }

    async resetTrace(projectPath: string): Promise<NovaOrynTraceSnapshot> {
        const session = this.latestSessionForProject(projectPath);
        if (session) { session.traceEvents.length = 0; session.bootStages.clear(); session.currentBootStage = undefined; return this.traceSnapshotForSession(session); }
        this.telemetryArchives.delete(path.resolve(projectPath));
        return { active: false, capturedAtUtc: new Date().toISOString(), elapsedMs: 0, events: [], bootStages: [], message: 'Trace data cleared.' };
    }

    async readProfilerSnapshot(projectPath: string): Promise<NovaOrynProfilerSnapshot> {
        const session = this.latestSessionForProject(projectPath);
        if (session) return this.profilerSnapshotForSession(session);
        return this.telemetryArchives.get(path.resolve(projectPath))?.profiler ?? { active: false, capturedAtUtc: new Date().toISOString(), elapsedMs: 0, totalSamples: 0, functions: [], cpus: [], counters: [], message: 'Run or Debug the operating system to collect profiling telemetry.' };
    }

    async resetProfiler(projectPath: string): Promise<NovaOrynProfilerSnapshot> {
        const session = this.latestSessionForProject(projectPath);
        if (session) { session.profileSamples.clear(); session.profileCpuSamples.clear(); session.profileCounters.clear(); return this.profilerSnapshotForSession(session); }
        const archived = this.telemetryArchives.get(path.resolve(projectPath));
        if (archived) archived.profiler = { active: false, capturedAtUtc: new Date().toISOString(), elapsedMs: 0, totalSamples: 0, functions: [], cpus: [], counters: [], message: 'Profiler data cleared.' };
        return archived?.profiler ?? { active: false, capturedAtUtc: new Date().toISOString(), elapsedMs: 0, totalSamples: 0, functions: [], cpus: [], counters: [], message: 'Profiler data cleared.' };
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

    async toggleBreakpoint(sessionId: string, sourcePath: string, line: number, condition?: string, hitCondition?: string): Promise<NovaOrynBreakpointResult> {
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

            const result = await this.armSourceBreakpoint(session, { sourcePath: normalizedSource, line, condition: condition?.trim() || undefined, hitCondition: hitCondition?.trim() || undefined });
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


    async updateBreakpoint(sessionId: string, request: NovaOrynBreakpointRequest): Promise<NovaOrynBreakpointResult> {
        const session = this.runSessions.get(sessionId);
        const sourcePath = path.resolve(request.sourcePath);
        const line = request.line;
        const condition = request.condition?.trim() || undefined;
        const hitCondition = request.hitCondition?.trim() || undefined;
        if (!session || session.mode !== 'debug' || !session.gdb || !session.debug?.active) {
            return { success: false, verified: false, sourcePath, line, condition, hitCondition, message: 'No active NovaOryn debug session.' };
        }
        if (hitCondition && !this.isValidHitCondition(hitCondition)) {
            return { success: false, verified: false, sourcePath, line, condition, hitCondition, message: `Invalid hit-count expression "${hitCondition}". Use N, =N, >=N, >N, <=N, <N, or %N.` };
        }
        const key = `${sourcePath.toLowerCase()}:${line}`;
        const existing = session.breakpoints.get(key);
        if (!existing) {
            const result = await this.armSourceBreakpoint(session, { sourcePath, line, condition, hitCondition });
            session.breakpointResults = session.breakpointResults.filter(item => !(path.resolve(item.sourcePath).toLowerCase() === sourcePath.toLowerCase() && item.line === line));
            session.breakpointResults.push(result);
            return result;
        }
        existing.condition = condition;
        existing.hitCondition = hitCondition;
        existing.hitCount = 0;
        const result: NovaOrynBreakpointResult = {
            success: true,
            verified: true,
            sourcePath,
            line,
            resolvedLine: existing.resolvedLine,
            address: existing.address,
            condition,
            hitCondition,
            hitCount: 0,
            message: `Breakpoint options updated${condition ? `; condition: ${condition}` : ''}${hitCondition ? `; hit count: ${hitCondition}` : ''}. Hit counter reset.`
        };
        this.replaceBreakpointResult(session, result);
        return result;
    }

    async configureExceptionBreakpoints(sessionId: string, settings: NovaOrynExceptionBreakpointSettings): Promise<NovaOrynDebugState> {
        const session = this.runSessions.get(sessionId);
        if (!session || session.mode !== 'debug' || !session.gdb || !session.debug?.active) {
            return { active: false, paused: false, sourceSymbols: false, message: 'No active NovaOryn debug session.' };
        }
        if (!session.debug.paused) {
            return { ...session.debug, message: 'Pause the kernel before changing CPU exception/panic breakpoints.' };
        }
        const vectors = Array.from(new Set((settings.vectors ?? []).filter(vector => Number.isInteger(vector) && vector >= 0 && vector < 32)));
        if (session.exceptionBreakpointAddress) {
            try { await session.gdb.command(`z0,${session.exceptionBreakpointAddress.toString(16)},1`); } catch { }
            session.exceptionBreakpointAddress = undefined;
        }
        if (session.panicBreakpointAddress) {
            try { await session.gdb.command(`z0,${session.panicBreakpointAddress.toString(16)},1`); } catch { }
            session.panicBreakpointAddress = undefined;
        }
        session.exceptionBreakpoints = { vectors, breakOnPanic: !!settings.breakOnPanic };
        await this.armExceptionBreakpoints(session, path.join(NOVAORYN_SDK_ROOT, 'Artifacts', 'MinimalKernel'));
        session.debug = {
            ...session.debug,
            message: `Exception breakpoints updated: ${vectors.length} CPU vector(s)${settings.breakOnPanic ? ' + fatal/panic stop' : ''}.`
        };
        return session.debug;
    }

    async selectExecutionContext(sessionId: string, threadId: string): Promise<NovaOrynDebugState> {
        const session = this.runSessions.get(sessionId);
        if (!session || session.mode !== 'debug' || !session.gdb || !session.debug?.active) {
            return { active: false, paused: false, sourceSymbols: false, message: 'No active NovaOryn debug session.' };
        }
        if (!session.debug.paused) {
            return { ...session.debug, message: 'Pause the kernel before switching CPU/thread context.' };
        }
        const normalized = threadId.trim();
        if (!normalized || !/^(?:p[0-9a-f]+\.)?[0-9a-f-]+$/i.test(normalized)) {
            return { ...session.debug, message: `Invalid GDB thread id "${threadId}".` };
        }
        const reply = await session.gdb.command(`Hg${normalized}`);
        if (reply !== 'OK') {
            return { ...session.debug, message: `QEMU rejected CPU/thread selection ${normalized}: ${reply}` };
        }
        session.selectedThreadId = normalized;
        const rip = await this.readRegister(session.gdb, 16);
        const source = this.resolveRuntimeSourceLocation(session, rip);
        session.debug = {
            ...session.debug,
            sourcePath: source?.sourcePath,
            line: source?.line,
            selectedThreadId: normalized,
            message: `Selected CPU/thread ${normalized}${source ? ` at ${path.basename(source.sourcePath)}:${source.line}` : ''}.`
        };
        await this.populatePausedDebugData(session, rip);
        return session.debug!;
    }

    async evaluateExpression(sessionId: string, expression: string): Promise<NovaOrynExpressionResult> {
        const session = this.runSessions.get(sessionId);
        const trimmed = expression.trim();
        if (!trimmed) { return { success: false, expression, error: 'Expression is empty.' }; }
        if (!session || session.mode !== 'debug' || !session.gdb || !session.debug?.active) {
            return { success: false, expression: trimmed, error: 'No active NovaOryn debug session.' };
        }
        if (!session.debug.paused) {
            return { success: false, expression: trimmed, error: 'Watch expressions can be evaluated only while the kernel is paused.' };
        }
        try {
            const value = await this.evaluateExpressionValue(session, trimmed);
            const unsigned = BigInt.asUintN(64, value);
            return {
                success: true,
                expression: trimmed,
                value: value.toString(10),
                hexValue: `0x${unsigned.toString(16).padStart(16, '0')}`
            };
        } catch (error) {
            return { success: false, expression: trimmed, error: error instanceof Error ? error.message : String(error) };
        }
    }

    async readMemoryRange(sessionId: string, addressExpression: string, length: number): Promise<NovaOrynMemoryReadResult> {
        const session = this.runSessions.get(sessionId);
        const expression = addressExpression.trim();
        const boundedLength = Math.max(1, Math.min(1024, Math.trunc(length || 0)));
        if (!expression) { return { success: false, expression, error: 'Memory address expression is empty.' }; }
        if (!session || session.mode !== 'debug' || !session.gdb || !session.debug?.active) {
            return { success: false, expression, error: 'No active NovaOryn debug session.' };
        }
        if (!session.debug.paused) {
            return { success: false, expression, error: 'Memory can be inspected only while the kernel is paused.' };
        }
        try {
            const address = BigInt.asUintN(64, await this.evaluateExpressionValue(session, expression));
            const bytes = await this.readMemory(session.gdb, address, boundedLength);
            if (bytes.length !== boundedLength) {
                return { success: false, expression, address: `0x${address.toString(16)}`, error: `QEMU could not read ${boundedLength} byte(s) at 0x${address.toString(16)}.` };
            }
            return {
                success: true,
                expression,
                address: `0x${address.toString(16).padStart(16, '0')}`,
                length: bytes.length,
                bytes: bytes.toString('hex')
            };
        } catch (error) {
            return { success: false, expression, error: error instanceof Error ? error.message : String(error) };
        }
    }

    async inspectPageTable(sessionId: string, addressExpression: string): Promise<NovaOrynPageTableInspection> {
        const session = this.runSessions.get(sessionId);
        const expression = addressExpression.trim();
        if (!expression) { return { success: false, expression, error: 'Virtual-address expression is empty.' }; }
        if (!session || session.mode !== 'debug' || !session.gdb || !session.debug?.active || !session.debug.paused) {
            return { success: false, expression, error: 'Page tables can be inspected only while a NovaOryn kernel is paused.' };
        }
        try {
            const virtualAddress = BigInt.asUintN(64, await this.evaluateExpressionValue(session, expression));
            const monitorRegisters = await this.qemuMonitor(session.gdb, 'info registers');
            const cr3Match = /\bCR3\s*=\s*(?:0x)?([0-9a-fA-F]+)/i.exec(monitorRegisters);
            if (!cr3Match) {
                return { success: false, expression, virtualAddress: this.formatAddress(virtualAddress), error: 'QEMU did not expose CR3 through its monitor.' };
            }
            const cr3 = BigInt(`0x${cr3Match[1]}`) & 0x000ffffffffff000n;
            const indexes = [
                Number((virtualAddress >> 39n) & 0x1ffn),
                Number((virtualAddress >> 30n) & 0x1ffn),
                Number((virtualAddress >> 21n) & 0x1ffn),
                Number((virtualAddress >> 12n) & 0x1ffn)
            ];
            const levels: Array<'PML4' | 'PDPT' | 'PD' | 'PT'> = ['PML4', 'PDPT', 'PD', 'PT'];
            const entries: NovaOrynPageTableEntry[] = [];
            let table = cr3;
            let physicalAddress: bigint | undefined;
            let pageSize = '';
            for (let depth = 0; depth < 4; depth++) {
                const index = indexes[depth];
                const entryPhysicalAddress = table + BigInt(index * 8);
                const value = await this.readPhysicalU64(session.gdb, entryPhysicalAddress);
                const present = (value & 1n) !== 0n;
                const largePage = depth >= 1 && depth <= 2 && (value & (1n << 7n)) !== 0n;
                let target: bigint | undefined;
                if (present) {
                    if (depth === 1 && largePage) target = value & 0x000fffffc0000000n;
                    else if (depth === 2 && largePage) target = value & 0x000fffffffe00000n;
                    else target = value & 0x000ffffffffff000n;
                }
                entries.push({
                    level: levels[depth], index,
                    entryPhysicalAddress: this.formatAddress(entryPhysicalAddress),
                    entryValue: this.formatAddress(value),
                    present,
                    writable: (value & (1n << 1n)) !== 0n,
                    user: (value & (1n << 2n)) !== 0n,
                    writeThrough: (value & (1n << 3n)) !== 0n,
                    cacheDisable: (value & (1n << 4n)) !== 0n,
                    accessed: (value & (1n << 5n)) !== 0n,
                    dirty: depth === 3 || largePage ? (value & (1n << 6n)) !== 0n : false,
                    largePage,
                    global: depth === 3 || largePage ? (value & (1n << 8n)) !== 0n : false,
                    noExecute: (value & (1n << 63n)) !== 0n,
                    targetPhysicalAddress: target !== undefined ? this.formatAddress(target) : undefined
                });
                if (!present || target === undefined) { break; }
                if (depth === 1 && largePage) {
                    physicalAddress = target + (virtualAddress & ((1n << 30n) - 1n));
                    pageSize = '1 GiB';
                    break;
                }
                if (depth === 2 && largePage) {
                    physicalAddress = target + (virtualAddress & ((1n << 21n) - 1n));
                    pageSize = '2 MiB';
                    break;
                }
                if (depth === 3) {
                    physicalAddress = target + (virtualAddress & 0xfffn);
                    pageSize = '4 KiB';
                    break;
                }
                table = target;
            }
            return {
                success: physicalAddress !== undefined,
                expression,
                virtualAddress: this.formatAddress(virtualAddress),
                cr3: this.formatAddress(cr3),
                pageSize: physicalAddress !== undefined ? pageSize : undefined,
                physicalAddress: physicalAddress !== undefined ? this.formatAddress(physicalAddress) : undefined,
                entries,
                error: physicalAddress === undefined ? 'The virtual address is not present in the active x64 page tables.' : undefined
            };
        } catch (error) {
            return { success: false, expression, error: error instanceof Error ? error.message : String(error) };
        }
    }

    async inspectHeap(sessionId: string): Promise<NovaOrynHeapSnapshot> {
        const session = this.runSessions.get(sessionId);
        if (!session || session.mode !== 'debug' || !session.gdb || !session.debug?.active || !session.debug.paused) {
            return { success: false, error: 'Kernel heap state can be inspected only while a NovaOryn kernel is paused.' };
        }
        try {
            // NovaOryn KernelHeap ABI v1 keeps its live allocator metadata in a fixed debugger-readable
            // virtual region at the top of the kernel-heap reservation. This is authoritative and does
            // not depend on private NativeAOT static-field names surviving PDB/link-map generation.
            const diagnosticAddress = 0xFFFF81FFFFFFC000n;
            const diagnosticHeader = await this.readMemoryChunked(session.gdb, diagnosticAddress, 64, 64);
            const diagnosticMagic = 0x4E4F484541503031n;
            let stateAddress: bigint;
            let stateBytes: Buffer;
            let diagnosticCommitted: bigint | undefined;
            let diagnosticAllocated: bigint | undefined;
            let diagnosticPeak: bigint | undefined;
            let diagnosticLive: number | undefined;
            let diagnosticInitialized: boolean | undefined;
            let diagnosticAbi = false;
            if (diagnosticHeader.length === 64 && diagnosticHeader.readBigUInt64LE(0) === diagnosticMagic && diagnosticHeader.readUInt32LE(8) === 1 && diagnosticHeader.readUInt32LE(12) === 512) {
                diagnosticAbi = true;
                diagnosticCommitted = diagnosticHeader.readBigUInt64LE(16);
                diagnosticAllocated = diagnosticHeader.readBigUInt64LE(24);
                diagnosticPeak = diagnosticHeader.readBigUInt64LE(32);
                diagnosticLive = diagnosticHeader.readUInt32LE(48);
                diagnosticInitialized = diagnosticHeader[56] !== 0;
                stateAddress = diagnosticAddress + 64n;
                stateBytes = await this.readMemoryChunked(session.gdb, stateAddress, 12800, 512);
            } else {
                // Backward compatibility for kernels built before the stable heap diagnostic ABI.
                await this.ensureNativeGlobalSymbols(session);
                const stateSymbol = this.findHeapGlobal(session, '_state');
                if (!stateSymbol || session.relocationDelta === undefined) {
                    return { success: false, error: 'This kernel predates the stable KernelHeap diagnostic ABI and NativeAOT did not expose its private _state symbol. Rebuild the OS with the bundled NovaOryn SDK from IDE 0.3.0 or later.' };
                }
                stateAddress = stateSymbol.linkedAddress + session.relocationDelta;
                stateBytes = await this.readMemoryChunked(session.gdb, stateAddress, 12800, 512);
            }
            if (stateBytes.length !== 12800) {
                return { success: false, error: `Could not read the KernelHeap state table at ${this.formatAddress(stateAddress)}.` };
            }
            const blocks: NovaOrynHeapBlock[] = [];
            let allocatedDerived = 0n;
            let freeDerived = 0n;
            let liveDerived = 0;
            let freeBlocksDerived = 0;
            for (let index = 0; index < 512; index++) {
                const start = stateBytes.readBigUInt64LE(index * 8);
                const length = stateBytes.readBigUInt64LE(4096 + index * 8);
                const token = stateBytes.readBigUInt64LE(8192 + index * 8);
                const state = stateBytes[12288 + index];
                if ((state !== 1 && state !== 2) || length === 0n) { continue; }
                if (state === 2) { allocatedDerived += length; liveDerived++; }
                else { freeDerived += length; freeBlocksDerived++; }
                blocks.push({
                    index,
                    state: state === 2 ? 'allocated' : 'free',
                    address: this.formatAddress(start),
                    byteCount: this.safeNumber(length),
                    token: state === 2 ? `0x${token.toString(16)}` : undefined
                });
            }
            const readGlobalU64 = async (suffix: string): Promise<bigint | undefined> => {
                const symbol = this.findHeapGlobal(session, suffix);
                if (!symbol) return undefined;
                return this.readU64(session.gdb!, symbol.linkedAddress + session.relocationDelta!);
            };
            const readGlobalU32 = async (suffix: string): Promise<number | undefined> => {
                const symbol = this.findHeapGlobal(session, suffix);
                if (!symbol) return undefined;
                const bytes = await this.readMemory(session.gdb!, symbol.linkedAddress + session.relocationDelta!, 4);
                return bytes.length === 4 ? bytes.readUInt32LE(0) : undefined;
            };
            let committed = diagnosticCommitted;
            let allocated = diagnosticAllocated;
            let peak = diagnosticPeak;
            let live = diagnosticLive;
            let initialized = diagnosticInitialized;
            if (!diagnosticAbi) {
                committed = await readGlobalU64('_committed');
                allocated = await readGlobalU64('_allocated');
                peak = await readGlobalU64('_peak');
                live = await readGlobalU32('_live');
                const initializedSymbol = this.findHeapGlobal(session, '_initialized');
                if (initializedSymbol) {
                    const byte = await this.readMemory(session.gdb, initializedSymbol.linkedAddress + session.relocationDelta!, 1);
                    if (byte.length === 1) initialized = byte[0] !== 0;
                }
            }
            return {
                success: true,
                initialized: initialized ?? blocks.length > 0,
                committedBytes: this.safeNumber(committed ?? (allocatedDerived + freeDerived)),
                allocatedBytes: this.safeNumber(allocated ?? allocatedDerived),
                freeBytes: this.safeNumber(freeDerived),
                peakAllocatedBytes: this.safeNumber(peak ?? allocatedDerived),
                liveAllocations: live ?? liveDerived,
                freeBlocks: freeBlocksDerived,
                blocks,
                message: diagnosticAbi
                    ? `KernelHeap metadata read from NovaOryn heap diagnostic ABI v1 (${blocks.length} active/free block record(s)).`
                    : `KernelHeap metadata read from legacy NativeAOT globals (${blocks.length} active/free block record(s)).`
            };
        } catch (error) {
            return { success: false, error: error instanceof Error ? error.message : String(error) };
        }
    }

    async captureCrashDump(sessionId: string, reason = 'manual debugger capture'): Promise<NovaOrynCrashDumpResult> {
        const session = this.runSessions.get(sessionId);
        if (!session || session.mode !== 'debug' || !session.gdb || !session.debug?.active || !session.debug.paused) {
            return { success: false, error: 'A crash/debug dump can be captured only while the NovaOryn kernel is paused.' };
        }
        try {
            const rip = session.debug.registers?.find(r => r.name === 'rip')?.value ?? 'rip';
            const rsp = session.debug.registers?.find(r => r.name === 'rsp')?.value ?? 'rsp';
            const pageTable = await this.inspectPageTable(sessionId, rip);
            const heap = await this.inspectHeap(sessionId);
            const stackMemory = await this.readMemoryRange(sessionId, rsp, 512);
            const codeMemory = await this.readMemoryRange(sessionId, rip, 128);
            const createdUtc = new Date().toISOString();
            const dumpRoot = path.join(session.projectRoot, '.novaoryn', 'crash-dumps');
            await fs.mkdir(dumpRoot, { recursive: true });
            const stamp = createdUtc.replace(/[:.]/g, '-');
            const dumpPath = path.join(dumpRoot, `NovaOryn-${stamp}.nodump.json`);
            const payload = {
                schemaVersion: 1,
                product: 'NovaOryn IDE',
                ideVersion: NOVAORYN_IDE_VERSION,
                createdUtc,
                reason,
                projectRoot: session.projectRoot,
                debugState: session.debug,
                pageTable,
                heap,
                memory: { stack: stackMemory, code: codeMemory }
            };
            await fs.writeFile(dumpPath, JSON.stringify(payload, null, 2), 'utf8');
            const dump: NovaOrynCrashDumpSummary = { path: dumpPath, createdUtc, reason, sourcePath: session.debug.sourcePath, line: session.debug.line };
            session.output += `[ OK ] NovaOryn crash/debug dump captured: ${dumpPath}\r\n`;
            return { success: true, dump, state: { ...session.debug }, pageTable, heap, memory: { stack: stackMemory, code: codeMemory } };
        } catch (error) {
            return { success: false, error: error instanceof Error ? error.message : String(error) };
        }
    }

    async listCrashDumps(projectPath: string): Promise<NovaOrynCrashDumpSummary[]> {
        const projectRoot = path.resolve(projectPath);
        const dumpRoot = path.join(projectRoot, '.novaoryn', 'crash-dumps');
        try {
            const names = (await fs.readdir(dumpRoot)).filter(name => name.endsWith('.nodump.json')).sort().reverse();
            const result: NovaOrynCrashDumpSummary[] = [];
            for (const name of names.slice(0, 100)) {
                try {
                    const file = path.join(dumpRoot, name);
                    const parsed = JSON.parse(await fs.readFile(file, 'utf8')) as any;
                    result.push({ path: file, createdUtc: String(parsed.createdUtc ?? ''), reason: String(parsed.reason ?? 'crash/debug dump'), sourcePath: parsed.debugState?.sourcePath, line: parsed.debugState?.line });
                } catch { }
            }
            return result;
        } catch { return []; }
    }

    async loadCrashDump(dumpPath: string): Promise<NovaOrynCrashDumpResult> {
        try {
            const resolved = path.resolve(dumpPath);
            if (!resolved.toLowerCase().endsWith('.nodump.json')) return { success: false, error: 'NovaOryn crash dumps must use the .nodump.json format.' };
            const parsed = JSON.parse(await fs.readFile(resolved, 'utf8')) as any;
            if (parsed.schemaVersion !== 1 || !parsed.debugState) return { success: false, error: 'The file is not a supported NovaOryn crash dump.' };
            const state: NovaOrynDebugState = { ...parsed.debugState, active: true, paused: true, message: `Offline crash dump: ${parsed.reason ?? 'captured debugger state'}` };
            const dump: NovaOrynCrashDumpSummary = { path: resolved, createdUtc: String(parsed.createdUtc ?? ''), reason: String(parsed.reason ?? 'crash/debug dump'), sourcePath: state.sourcePath, line: state.line };
            return { success: true, dump, state, pageTable: parsed.pageTable, heap: parsed.heap, memory: parsed.memory };
        } catch (error) {
            return { success: false, error: error instanceof Error ? error.message : String(error) };
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

        const rawDebugMap = JSON.parse(await fs.readFile(debugMapPath, 'utf8')) as { image?: string; pdb?: string; anchor?: NativeDebugMap['anchor']; entries?: Array<Record<string, unknown>> };
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
        session.nativeDebugMap = { image: rawDebugMap.image, pdb: rawDebugMap.pdb, anchor: rawDebugMap.anchor!, entries: normalizedEntries };
        if (session.nativeDebugMap.anchor?.symbol !== 'NovaOrynDebugImageAnchor' || !session.nativeDebugMap.anchor.linkedAddress || !session.nativeDebugMap.anchor.resumeLinkedAddress || session.nativeDebugMap.anchor.transport !== 'qemu-debugcon-0xe9-binary-v1' || session.nativeDebugMap.entries.length === 0) {
            throw new Error('NovaOryn.DebugSymbols.json is incomplete or does not contain the NovaOryn 0.37.4 debug rendezvous metadata. Rebuild the SDK/kernel in Debug mode.');
        }

        const stamp = new Date().toISOString().replace(/[-:TZ.]/g, '').slice(0, 17);
        const runDirectory = path.join(artifactRoot, 'Runs', `debug-${stamp}`);
        await fs.mkdir(runDirectory, { recursive: true });
        const varsCopy = path.join(runDirectory, 'OVMF_VARS.fd');
        const serialLog = path.join(runDirectory, 'serial.log');
        session.serialLogPath = serialLog;
        session.serialLogOffset = 0;
        const debugConLog = path.join(runDirectory, 'debugcon.bin');
        await fs.copyFile(ovmfVars, varsCopy);
        const gdbPort = await this.findFreePort(1234, 1299);
        const activeTarget = await this.getActiveTarget(session.projectRoot);
        const qemu = activeTarget?.kind === 'qemu' ? activeTarget.qemu : undefined;
        const qemuCpus = Math.max(1, qemu?.cpuCount ?? Math.ceil(os.cpus().length / 2));
        const memoryMiB = Math.max(64, qemu?.memoryMiB ?? 512);
        const machine = qemu?.machine || 'q35';
        const accelerator = qemu?.accelerator === 'whpx' ? 'whpx' : 'tcg,thread=multi';
        const display = qemu?.display || 'sdl';
        const args = [
            '-machine', machine, '-accel', accelerator, '-cpu', 'max', '-smp', String(qemuCpus), '-m', `${memoryMiB}M`,
            '-display', display,
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
            const result = await this.armSourceBreakpoint(session, requested);
            session.breakpointResults.push(result);
            session.output += `[DEBUG] ${result.sourcePath}:${result.line}: ${result.message}\r\n`;
        }
        await this.armExceptionBreakpoints(session, artifactRoot);

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

    protected async armSourceBreakpoint(session: RunSession, request: NovaOrynBreakpointRequest): Promise<NovaOrynBreakpointResult> {
        const sourcePath = request.sourcePath;
        const line = request.line;
        const condition = request.condition?.trim() || undefined;
        const hitCondition = request.hitCondition?.trim() || undefined;
        if (!session.gdb || session.relocationDelta === undefined || !session.nativeDebugMap) {
            return { success: false, verified: false, sourcePath, line, condition, hitCondition, hitCount: 0, message: 'The native source map or EFI relocation is not ready.' };
        }
        if (hitCondition && !this.isValidHitCondition(hitCondition)) {
            return { success: false, verified: false, sourcePath, line, condition, hitCondition, hitCount: 0, message: `Invalid hit-count expression "${hitCondition}". Use N, =N, >=N, >N, <=N, <N, or %N.` };
        }
        const resolved = this.resolveLinkedSourceAddress(session.nativeDebugMap, sourcePath, line);
        if (resolved === undefined) {
            return { success: false, verified: false, sourcePath, line, condition, hitCondition, hitCount: 0, message: 'No executable NativeAOT sequence point exists on this C# line or a nearby executable line in the same source file.' };
        }
        const runtimeAddress = resolved.linkedAddress + session.relocationDelta;
        const address = runtimeAddress.toString(16);
        const reply = await session.gdb.command(`Z0,${address},1`);
        const breakpoint: SourceBreakpoint = { sourcePath: path.resolve(sourcePath), line, resolvedLine: resolved.resolvedLine, address, condition, hitCondition, hitCount: 0 };
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
            condition,
            hitCondition,
            hitCount: 0,
            message: reply === 'OK'
                ? `Breakpoint verified (${binding}) at runtime address 0x${address}.`
                : `QEMU rejected the source breakpoint (${binding}): ${reply}`
        };
    }

    protected async armExceptionBreakpoints(session: RunSession, artifactRoot: string): Promise<void> {
        if (!session.gdb || session.relocationDelta === undefined) { return; }
        const mapPath = path.join(artifactRoot, 'MinimalKernel.map');
        try {
            const mapText = await fs.readFile(mapPath, 'utf8');
            if (session.exceptionBreakpoints.vectors.length > 0) {
                const linked = this.findLinkedSymbolAddress(mapText, 'NovaOrynX64InterruptCommon');
                if (linked !== undefined) {
                    const runtime = linked + session.relocationDelta;
                    const reply = await session.gdb.command(`Z0,${runtime.toString(16)},1`);
                    if (reply === 'OK') {
                        session.exceptionBreakpointAddress = runtime;
                        session.output += `[ OK ] CPU exception breakpoint gate armed for vectors ${session.exceptionBreakpoints.vectors.join(', ')}.\r\n`;
                    } else {
                        session.output += `[WARN] QEMU rejected the CPU exception breakpoint gate: ${reply}.\r\n`;
                    }
                } else {
                    session.output += '[WARN] CPU exception breakpoint gate symbol was not found in MinimalKernel.map.\r\n';
                }
            }
            if (session.exceptionBreakpoints.breakOnPanic) {
                const linked = this.findLinkedSymbolAddress(mapText, 'NovaOrynX64StopProcessor');
                if (linked !== undefined) {
                    const runtime = linked + session.relocationDelta;
                    const reply = await session.gdb.command(`Z0,${runtime.toString(16)},1`);
                    if (reply === 'OK') {
                        session.panicBreakpointAddress = runtime;
                        session.output += '[ OK ] Fatal/panic stop breakpoint armed.\r\n';
                    } else {
                        session.output += `[WARN] QEMU rejected the fatal/panic stop breakpoint: ${reply}.\r\n`;
                    }
                } else {
                    session.output += '[WARN] Fatal/panic stop symbol was not found in MinimalKernel.map.\r\n';
                }
            }
        } catch (error) {
            session.output += `[WARN] Exception/panic breakpoints could not be armed: ${error instanceof Error ? error.message : String(error)}.\r\n`;
        }
    }

    protected findLinkedSymbolAddress(mapText: string, symbol: string): bigint | undefined {
        for (const line of mapText.split(/\r?\n/)) {
            if (!line.includes(symbol)) { continue; }
            const values = Array.from(line.matchAll(/(?:0x)?([0-9a-fA-F]{8,16})/g))
                .map(match => BigInt(`0x${match[1]}`));
            const va = values.filter(value => value >= 0x100000000n).sort((a, b) => a < b ? -1 : a > b ? 1 : 0)[0];
            if (va !== undefined) { return va; }
        }
        return undefined;
    }

    protected exceptionName(vector: number): string {
        const names: Record<number, string> = {
            0: 'Divide error', 1: 'Debug', 2: 'Non-maskable interrupt', 3: 'Breakpoint', 4: 'Overflow',
            5: 'BOUND range exceeded', 6: 'Invalid opcode', 7: 'Device not available', 8: 'Double fault',
            10: 'Invalid TSS', 11: 'Segment not present', 12: 'Stack-segment fault', 13: 'General protection fault',
            14: 'Page fault', 16: 'x87 floating-point exception', 17: 'Alignment check', 18: 'Machine check',
            19: 'SIMD floating-point exception', 20: 'Virtualization exception', 21: 'Control protection exception'
        };
        return names[vector] ?? `CPU exception ${vector}`;
    }

    protected async buildDisassembly(session: RunSession, rip: bigint): Promise<NovaOrynDisassemblyInstruction[]> {
        if (session.relocationDelta === undefined || !session.nativeDebugMap?.image) { return []; }
        const linkedRip = rip - session.relocationDelta;
        const tool = path.join(NOVAORYN_SDK_ROOT, '.toolchain', 'LLVM', 'bin', 'llvm-objdump.exe');
        if (!(await this.exists(tool)) || !(await this.exists(session.nativeDebugMap.image))) { return []; }
        const stop = linkedRip + 192n;
        const output = await this.captureTool(tool, [
            '-d', '--no-show-raw-insn', `--start-address=0x${linkedRip.toString(16)}`,
            `--stop-address=0x${stop.toString(16)}`, session.nativeDebugMap.image
        ]);
        if (output.exitCode !== 0) { return []; }
        const instructions: NovaOrynDisassemblyInstruction[] = [];
        for (const line of output.text.split(/\r?\n/)) {
            const match = line.match(/^\s*([0-9a-fA-F]+):\s*(.+?)\s*$/);
            if (!match) { continue; }
            const linked = BigInt(`0x${match[1]}`);
            const runtime = linked + session.relocationDelta;
            const location = this.resolveSourceLocation(session.nativeDebugMap, linked);
            instructions.push({
                runtimeAddress: `0x${runtime.toString(16)}`,
                linkedAddress: `0x${linked.toString(16)}`,
                instruction: match[2],
                sourcePath: location?.sourcePath,
                line: location?.line,
                current: linked === linkedRip
            });
            if (instructions.length >= 32) { break; }
        }
        return instructions;
    }

    protected async captureTool(command: string, args: string[]): Promise<{ exitCode: number; text: string }> {
        return new Promise(resolve => {
            const child = spawn(command, args, { windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'] });
            let text = '';
            child.stdout?.on('data', data => { text += data.toString(); });
            child.stderr?.on('data', data => { text += data.toString(); });
            child.once('error', error => resolve({ exitCode: 1, text: error.message }));
            child.once('close', code => resolve({ exitCode: code ?? 1, text }));
        });
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
        const stoppedThread = /(?:^|;)thread:([^;]+)/i.exec(packet)?.[1];
        if (stoppedThread) { session.selectedThreadId = stoppedThread; }
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

            if (session.exceptionBreakpointAddress && candidates.some(candidate => candidate === session.exceptionBreakpointAddress)) {
                const rsp = await this.readRegister(session.gdb, 7);
                const vector = Number((await this.readU64(session.gdb, rsp)) & 0xffn);
                if (session.exceptionBreakpoints.vectors.includes(vector)) {
                    const faultRip = await this.readU64(session.gdb, rsp + 16n);
                    const name = this.exceptionName(vector);
                    const linkedFault = faultRip - session.relocationDelta;
                    const location = this.resolveSourceLocation(session.nativeDebugMap, linkedFault);
                    session.debug = {
                        ...session.debug, paused: true, sourcePath: location?.sourcePath, line: location?.line,
                        exceptionVector: vector, exceptionName: name,
                        message: `CPU exception breakpoint: ${name} (vector ${vector})${location ? ` at ${path.basename(location.sourcePath)}:${location.line}` : ` at RIP 0x${faultRip.toString(16)}`}.`
                    };
                    await this.populatePausedDebugData(session, faultRip);
                    const dump = await this.captureCrashDump(session.sessionId, `CPU exception: ${name} (vector ${vector})`);
                    if (!dump.success) session.output += `[WARN] Automatic exception crash dump failed: ${dump.error}\r\n`;
                    return;
                }
                session.gdb.run('c');
                session.debug = this.runningDebugState(session, `Kernel running. Ignored CPU exception vector ${vector}.`);
                return;
            }

            if (session.panicBreakpointAddress && candidates.some(candidate => candidate === session.panicBreakpointAddress)) {
                session.debug = { ...session.debug, paused: true, exceptionName: 'Kernel fatal/panic stop', message: 'Kernel fatal/panic breakpoint reached before the processor halt loop.' };
                await this.populatePausedDebugData(session, rip);
                const dump = await this.captureCrashDump(session.sessionId, 'Kernel fatal/panic stop');
                if (!dump.success) session.output += `[WARN] Automatic panic crash dump failed: ${dump.error}\r\n`;
                return;
            }

            const hit = Array.from(session.breakpoints.values()).find(bp =>
                candidates.some(candidate => candidate === this.parseAddress(bp.address)));

            if (hit) {
                await this.clearTemporaryStepBreakpoint(session);
                session.stepPlan = undefined;
                hit.hitCount++;
                const result: NovaOrynBreakpointResult = {
                    success: true, verified: true, sourcePath: hit.sourcePath, line: hit.line,
                    resolvedLine: hit.resolvedLine, address: hit.address, condition: hit.condition,
                    hitCondition: hit.hitCondition, hitCount: hit.hitCount,
                    message: `Breakpoint armed; hit count ${hit.hitCount}.`
                };
                this.replaceBreakpointResult(session, result);

                const hitCountMatches = this.hitConditionMatches(hit.hitCondition, hit.hitCount);
                let conditionMatches = true;
                if (hit.condition && hitCountMatches) {
                    try {
                        conditionMatches = (await this.evaluateExpressionValue(session, hit.condition)) !== 0n;
                    } catch (error) {
                        session.lastBreakpoint = hit;
                        session.debug = {
                            ...session.debug, paused: true, sourcePath: hit.sourcePath, line: hit.resolvedLine,
                            message: `Breakpoint condition error at ${path.basename(hit.sourcePath)}:${hit.resolvedLine}: ${error instanceof Error ? error.message : String(error)}`
                        };
                        await this.populatePausedDebugData(session, rip);
                        return;
                    }
                }

                if (!hitCountMatches || !conditionMatches) {
                    const reason = !hitCountMatches
                        ? `hit ${hit.hitCount} does not match ${hit.hitCondition}`
                        : `condition "${hit.condition}" evaluated false`;
                    session.output += `[DEBUG] Breakpoint skipped at ${hit.sourcePath}:${hit.line}: ${reason}.\r\n`;
                    session.gdb.run('c');
                    session.debug = this.runningDebugState(session, `Kernel running. Breakpoint skipped (${reason}).`);
                    return;
                }

                session.lastBreakpoint = hit;
                const qualifiers = [hit.condition ? `condition: ${hit.condition}` : '', hit.hitCondition ? `hit rule: ${hit.hitCondition}, hit ${hit.hitCount}` : ''].filter(Boolean).join('; ');
                session.debug = {
                    ...session.debug,
                    paused: true,
                    sourcePath: hit.sourcePath,
                    line: hit.resolvedLine,
                    message: (hit.resolvedLine === hit.line
                        ? `Breakpoint reached at ${path.basename(hit.sourcePath)}:${hit.line}.`
                        : `Breakpoint reached at ${path.basename(hit.sourcePath)}:${hit.resolvedLine} (requested line ${hit.line}).`) + (qualifiers ? ` ${qualifiers}.` : '')
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
            disassembly: undefined,
            exceptionVector: undefined,
            exceptionName: undefined,
            message
        };
    }

    protected stepLabel(kind: StepPlan['kind']): string {
        if (kind === 'step-into') { return 'Step Into'; }
        if (kind === 'step-over') { return 'Step Over'; }
        return 'Step Out';
    }


    protected replaceBreakpointResult(session: RunSession, result: NovaOrynBreakpointResult): void {
        const normalized = path.resolve(result.sourcePath).toLowerCase();
        session.breakpointResults = session.breakpointResults.filter(item => !(path.resolve(item.sourcePath).toLowerCase() === normalized && item.line === result.line));
        session.breakpointResults.push(result);
    }

    protected isValidHitCondition(value: string): boolean {
        const match = value.trim().match(/^(?:=|==|>=|<=|>|<|%)?\s*([1-9][0-9]*)$/);
        return !!match;
    }

    protected hitConditionMatches(value: string | undefined, hitCount: number): boolean {
        if (!value) { return true; }
        const match = value.trim().match(/^(=|==|>=|<=|>|<|%)?\s*([1-9][0-9]*)$/);
        if (!match) { return false; }
        const op = match[1] || '=';
        const target = Number(match[2]);
        if (op === '%') { return hitCount % target === 0; }
        if (op === '>') { return hitCount > target; }
        if (op === '>=') { return hitCount >= target; }
        if (op === '<') { return hitCount < target; }
        if (op === '<=') { return hitCount <= target; }
        return hitCount === target;
    }

    protected async evaluateExpressionValue(session: RunSession, expression: string): Promise<bigint> {
        if (!session.gdb) { throw new Error('QEMU debugger is not attached.'); }
        const registerNames = ['rax','rbx','rcx','rdx','rsi','rdi','rbp','rsp','r8','r9','r10','r11','r12','r13','r14','r15','rip','rflags'];
        const registerIndexes = new Map(registerNames.map((name, index) => [name, index]));
        const cache = new Map<string, bigint>();
        const parser = new NovaOrynExpressionParser(
            expression,
            async name => {
                const index = registerIndexes.get(name);
                if (index !== undefined) {
                    const cached = cache.get(name);
                    if (cached !== undefined) { return cached; }
                    const value = await this.readRegister(session.gdb!, index);
                    cache.set(name, value);
                    return value;
                }
                const named = await this.resolveNamedVariableValue(session, name);
                if (named !== undefined) { cache.set(name, named); }
                return named;
            },
            async address => this.readU64(session.gdb!, address)
        );
        return parser.evaluate();
    }

    protected async resolveNamedVariableValue(session: RunSession, name: string): Promise<bigint | undefined> {
        if (!session.gdb || session.relocationDelta === undefined) { return undefined; }
        await this.ensureNativeVariableMap(session);
        if (!session.nativeVariables?.length) { return undefined; }
        const rip = await this.readRegister(session.gdb, 16);
        const linkedRip = rip - session.relocationDelta;
        const variable = session.nativeVariables.find(item => item.name.toLowerCase() === name.toLowerCase() &&
            linkedRip >= item.functionStart && linkedRip < item.functionEnd &&
            (item.rangeStart === undefined || linkedRip >= item.rangeStart) &&
            (item.rangeEnd === undefined || linkedRip < item.rangeEnd));
        if (!variable) { return undefined; }
        if (variable.register) {
            const index = this.x64RegisterIndex(variable.register);
            return index === undefined ? undefined : this.readRegister(session.gdb, index);
        }
        if (variable.baseRegister) {
            const index = this.x64RegisterIndex(variable.baseRegister);
            if (index === undefined) { return undefined; }
            const base = await this.readRegister(session.gdb, index);
            return this.readU64(session.gdb, BigInt.asUintN(64, base + (variable.offset ?? 0n)));
        }
        return undefined;
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
        const executionContexts = await this.readExecutionContexts(session);
        const registers = await this.readRegisterSet(session.gdb);
        const registerMap = new Map(registers.map(item => [item.name, this.parseAddress(item.value)]));
        const rbp = registerMap.get('rbp') ?? 0n;
        const rsp = registerMap.get('rsp') ?? 0n;
        const callStack = await this.readCallStack(session, rip, rbp, rsp, registerMap);
        const namedVariables = await this.readNamedNativeVariables(session, rip);
        const locals = namedVariables.length > 0 ? namedVariables : await this.readFrameSlots(session.gdb, rbp, rsp);
        const disassembly = await this.buildDisassembly(session, rip);
        session.debug = {
            ...session.debug,
            registers,
            callStack,
            executionContexts,
            selectedThreadId: session.selectedThreadId,
            locals,
            disassembly,
            localsMessage: namedVariables.length > 0
                ? (session.nativeVariablesMessage ?? 'Named C# arguments/locals resolved from NativeAOT CodeView/PDB variable records.')
                : (session.nativeVariablesMessage ?? 'No active named NativeAOT variable records were available at this instruction; showing native frame/stack slots instead.')
        };
    }

    protected resolveRuntimeSourceLocation(session: RunSession, runtimeAddress: bigint): NativeSourceLine | undefined {
        if (session.relocationDelta === undefined || !session.nativeDebugMap) { return undefined; }
        return this.resolveSourceLocation(session.nativeDebugMap, runtimeAddress - session.relocationDelta);
    }

    protected async readExecutionContexts(session: RunSession): Promise<NovaOrynDebugExecutionContext[]> {
        if (!session.gdb) { return []; }
        const ids: string[] = [];
        try {
            let reply = await session.gdb.command('qfThreadInfo');
            while (reply.startsWith('m')) {
                ids.push(...reply.slice(1).split(',').map(item => item.trim()).filter(Boolean));
                reply = await session.gdb.command('qsThreadInfo');
            }
        } catch { }
        let current = session.selectedThreadId;
        if (!current) {
            try {
                const currentReply = await session.gdb.command('qC');
                if (currentReply.startsWith('QC')) { current = currentReply.slice(2); }
            } catch { }
        }
        if (ids.length === 0 && current) { ids.push(current); }
        const contexts: NovaOrynDebugExecutionContext[] = [];
        for (let index = 0; index < ids.length; index++) {
            const id = ids[index];
            let name = `CPU ${index} / thread ${id}`;
            try {
                const extra = await session.gdb.command(`qThreadExtraInfo,${id}`);
                if (/^[0-9a-f]+$/i.test(extra) && extra.length % 2 === 0) {
                    const decoded = Buffer.from(extra, 'hex').toString('utf8').replace(/\0/g, '').trim();
                    if (decoded) { name = decoded; }
                }
            } catch { }
            const match = /^p([0-9a-f]+)\.([0-9a-f]+)$/i.exec(id);
            contexts.push({
                id,
                threadId: match ? match[2] : id,
                processId: match ? match[1] : undefined,
                cpuIndex: index,
                name,
                current: !!current && id.toLowerCase() === current.toLowerCase()
            });
        }
        if (!session.selectedThreadId && current) { session.selectedThreadId = current; }
        return contexts;
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

    protected async readCallStack(session: RunSession, rip: bigint, initialRbp: bigint, rsp: bigint, registerMap: Map<string, bigint>): Promise<NovaOrynDebugFrame[]> {
        const frames: NovaOrynDebugFrame[] = [];
        const addFrame = (address: bigint, index: number, unwoundBy: 'x64-unwind' | 'leaf') => {
            const location = this.resolveRuntimeSourceLocation(session, address);
            frames.push({
                index,
                address: `0x${address.toString(16)}`,
                label: location ? `${path.basename(location.sourcePath)}:${location.line}` : `native 0x${address.toString(16)}`,
                sourcePath: location?.sourcePath,
                line: location?.line,
                kind: location ? 'managed' : 'native',
                unwoundBy
            });
        };
        addFrame(rip, 0, 'x64-unwind');
        if (!session.gdb || rsp === 0n || session.relocationDelta === undefined) { return frames; }

        const table = await this.ensurePeUnwindTable(session);
        if (!table) {
            // Do not fall back to scanning arbitrary stack words: that creates false frames.
            return frames;
        }

        const context = new Map(registerMap);
        context.set('rip', rip);
        context.set('rsp', rsp);
        context.set('rbp', initialRbp);
        const seen = new Set<string>();
        for (let index = 1; index < 64; index++) {
            const currentRip = context.get('rip') ?? 0n;
            let currentRsp = context.get('rsp') ?? 0n;
            if (currentRip === 0n || currentRsp === 0n) { break; }
            const signature = `${currentRip.toString(16)}:${currentRsp.toString(16)}`;
            if (seen.has(signature)) { break; }
            seen.add(signature);

            const linked = currentRip - session.relocationDelta;
            const rvaBig = linked - table.imageBase;
            if (rvaBig < 0n || rvaBig > 0xffffffffn) { break; }
            const rva = Number(rvaBig);
            const entry = table.entries.find(item => rva >= item.beginRva && rva < item.endRva);
            let method: 'x64-unwind' | 'leaf' = 'leaf';
            if (entry) {
                method = 'x64-unwind';
                const nextRsp = await this.applyX64UnwindInfo(session.gdb, table, entry.unwindRva, context);
                if (nextRsp === undefined) { break; }
                currentRsp = nextRsp;
            }

            const returnAddress = await this.readU64(session.gdb, currentRsp);
            if (returnAddress === 0n) { break; }
            context.set('rip', returnAddress);
            context.set('rsp', currentRsp + 8n);
            addFrame(returnAddress, index, method);
        }
        return frames;
    }

    protected async ensurePeUnwindTable(session: RunSession): Promise<PeUnwindTable | undefined> {
        if (session.unwindTableLoaded) { return session.unwindTable; }
        session.unwindTableLoaded = true;
        try {
            const image = session.nativeDebugMap?.image ?? path.join(NOVAORYN_SDK_ROOT, 'Artifacts', 'MinimalKernel', 'MinimalKernel.efi');
            const bytes = await fs.readFile(image);
            if (bytes.length < 0x100 || bytes.toString('ascii', 0, 2) !== 'MZ') { return undefined; }
            const pe = bytes.readUInt32LE(0x3c);
            if (pe + 0x100 >= bytes.length || bytes.toString('ascii', pe, pe + 4) !== 'PE\0\0') { return undefined; }
            const sectionCount = bytes.readUInt16LE(pe + 6);
            const optionalSize = bytes.readUInt16LE(pe + 20);
            const optional = pe + 24;
            const magic = bytes.readUInt16LE(optional);
            if (magic !== 0x20b) { return undefined; }
            const imageBase = bytes.readBigUInt64LE(optional + 24);
            const exceptionRva = bytes.readUInt32LE(optional + 112 + 3 * 8);
            const exceptionSize = bytes.readUInt32LE(optional + 112 + 3 * 8 + 4);
            const sectionTable = optional + optionalSize;
            const sections: PeSectionInfo[] = [];
            for (let i = 0; i < sectionCount; i++) {
                const o = sectionTable + i * 40;
                sections.push({
                    virtualSize: bytes.readUInt32LE(o + 8),
                    virtualAddress: bytes.readUInt32LE(o + 12),
                    rawSize: bytes.readUInt32LE(o + 16),
                    rawOffset: bytes.readUInt32LE(o + 20)
                });
            }
            const rvaToOffset = (rva: number): number | undefined => {
                for (const section of sections) {
                    const size = Math.max(section.virtualSize, section.rawSize);
                    if (rva >= section.virtualAddress && rva < section.virtualAddress + size) {
                        return section.rawOffset + (rva - section.virtualAddress);
                    }
                }
                return rva < bytes.length ? rva : undefined;
            };
            const exceptionOffset = rvaToOffset(exceptionRva);
            if (exceptionOffset === undefined) { return undefined; }
            const entries: PeUnwindEntry[] = [];
            const count = Math.floor(exceptionSize / 12);
            for (let i = 0; i < count; i++) {
                const o = exceptionOffset + i * 12;
                if (o + 12 > bytes.length) { break; }
                const beginRva = bytes.readUInt32LE(o);
                const endRva = bytes.readUInt32LE(o + 4);
                const unwindRva = bytes.readUInt32LE(o + 8);
                if (beginRva && endRva > beginRva && unwindRva) { entries.push({ beginRva, endRva, unwindRva }); }
            }
            entries.sort((a, b) => a.beginRva - b.beginRva);
            session.unwindTable = { imageBase, bytes, sections, entries };
            return session.unwindTable;
        } catch {
            return undefined;
        }
    }

    protected peRvaToOffset(table: PeUnwindTable, rva: number): number | undefined {
        for (const section of table.sections) {
            const size = Math.max(section.virtualSize, section.rawSize);
            if (rva >= section.virtualAddress && rva < section.virtualAddress + size) {
                const offset = section.rawOffset + (rva - section.virtualAddress);
                return offset < table.bytes.length ? offset : undefined;
            }
        }
        return rva < table.bytes.length ? rva : undefined;
    }

    protected x64UnwindRegisterName(index: number): string | undefined {
        return ['rax','rcx','rdx','rbx','rsp','rbp','rsi','rdi','r8','r9','r10','r11','r12','r13','r14','r15'][index];
    }

    protected async applyX64UnwindInfo(gdb: GdbRspClient, table: PeUnwindTable, unwindRva: number, context: Map<string, bigint>, depth = 0): Promise<bigint | undefined> {
        if (depth > 8) { return undefined; }
        const offset = this.peRvaToOffset(table, unwindRva);
        if (offset === undefined || offset + 4 > table.bytes.length) { return undefined; }
        const versionFlags = table.bytes[offset];
        const flags = versionFlags >> 3;
        const countCodes = table.bytes[offset + 2];
        const frameByte = table.bytes[offset + 3];
        const frameRegisterIndex = frameByte & 0x0f;
        const frameOffset = (frameByte >> 4) * 16;
        let virtualRsp = context.get('rsp') ?? 0n;
        let slot = 0;
        const codeBase = offset + 4;
        while (slot < countCodes) {
            const co = codeBase + slot * 2;
            if (co + 2 > table.bytes.length) { return undefined; }
            const opByte = table.bytes[co + 1];
            const unwindOp = opByte & 0x0f;
            const opInfo = opByte >> 4;
            slot++;
            if (unwindOp === 0) { // UWOP_PUSH_NONVOL
                const reg = this.x64UnwindRegisterName(opInfo);
                if (reg) { context.set(reg, await this.readU64(gdb, virtualRsp)); }
                virtualRsp += 8n;
            } else if (unwindOp === 1) { // UWOP_ALLOC_LARGE
                if (opInfo === 0) {
                    const oo = codeBase + slot * 2;
                    if (oo + 2 > table.bytes.length) { return undefined; }
                    virtualRsp += BigInt(table.bytes.readUInt16LE(oo) * 8);
                    slot += 1;
                } else {
                    const oo = codeBase + slot * 2;
                    if (oo + 4 > table.bytes.length) { return undefined; }
                    virtualRsp += BigInt(table.bytes.readUInt32LE(oo));
                    slot += 2;
                }
            } else if (unwindOp === 2) { // UWOP_ALLOC_SMALL
                virtualRsp += BigInt(opInfo * 8 + 8);
            } else if (unwindOp === 3) { // UWOP_SET_FPREG
                const reg = this.x64UnwindRegisterName(frameRegisterIndex);
                const frameValue = reg ? context.get(reg) : undefined;
                if (frameValue !== undefined) { virtualRsp = frameValue - BigInt(frameOffset); }
            } else if (unwindOp === 4 || unwindOp === 8) { // SAVE_NONVOL / SAVE_XMM128
                const oo = codeBase + slot * 2;
                if (oo + 2 > table.bytes.length) { return undefined; }
                if (unwindOp === 4) {
                    const reg = this.x64UnwindRegisterName(opInfo);
                    if (reg) { context.set(reg, await this.readU64(gdb, virtualRsp + BigInt(table.bytes.readUInt16LE(oo) * 8))); }
                }
                slot += 1;
            } else if (unwindOp === 5 || unwindOp === 9) { // FAR saves
                const oo = codeBase + slot * 2;
                if (oo + 4 > table.bytes.length) { return undefined; }
                if (unwindOp === 5) {
                    const reg = this.x64UnwindRegisterName(opInfo);
                    if (reg) { context.set(reg, await this.readU64(gdb, virtualRsp + BigInt(table.bytes.readUInt32LE(oo)))); }
                }
                slot += 2;
            } else if (unwindOp === 10) { // UWOP_PUSH_MACHFRAME
                virtualRsp += BigInt(opInfo === 0 ? 40 : 48);
            }
        }
        context.set('rsp', virtualRsp);

        // UNW_FLAG_CHAININFO: continue through the chained runtime function's unwind metadata.
        if ((flags & 0x4) !== 0) {
            const alignedSlots = (countCodes + 1) & ~1;
            const chained = codeBase + alignedSlots * 2;
            if (chained + 12 <= table.bytes.length) {
                const chainedUnwindRva = table.bytes.readUInt32LE(chained + 8);
                const chainedRsp = await this.applyX64UnwindInfo(gdb, table, chainedUnwindRva, context, depth + 1);
                if (chainedRsp !== undefined) { virtualRsp = chainedRsp; }
            }
        }
        return virtualRsp;
    }

    protected async readNamedNativeVariables(session: RunSession, rip: bigint): Promise<NovaOrynDebugVariable[]> {
        if (!session.gdb || session.relocationDelta === undefined) { return []; }
        await this.ensureNativeVariableMap(session);
        if (!session.nativeVariables || session.nativeVariables.length === 0) { return []; }
        const linkedRip = rip - session.relocationDelta;
        const active = session.nativeVariables.filter(variable =>
            linkedRip >= variable.functionStart && linkedRip < variable.functionEnd &&
            (variable.rangeStart === undefined || linkedRip >= variable.rangeStart) &&
            (variable.rangeEnd === undefined || linkedRip < variable.rangeEnd));
        const result: NovaOrynDebugVariable[] = [];
        const seen = new Set<string>();
        for (const variable of active) {
            const key = `${variable.kind}:${variable.name}`;
            if (seen.has(key)) { continue; }
            seen.add(key);
            let value: bigint | undefined;
            let location = '';
            if (variable.register) {
                const registerIndex = this.x64RegisterIndex(variable.register);
                if (registerIndex !== undefined) {
                    value = await this.readRegister(session.gdb, registerIndex);
                    location = variable.register.toLowerCase();
                }
            } else if (variable.baseRegister) {
                const registerIndex = this.x64RegisterIndex(variable.baseRegister);
                if (registerIndex !== undefined) {
                    const base = await this.readRegister(session.gdb, registerIndex);
                    const address = BigInt.asUintN(64, base + (variable.offset ?? 0n));
                    value = await this.readU64(session.gdb, address);
                    const signedOffset = variable.offset ?? 0n;
                    location = `[${variable.baseRegister.toLowerCase()}${signedOffset < 0n ? `-0x${(-signedOffset).toString(16)}` : `+0x${signedOffset.toString(16)}`}]`;
                }
            }
            result.push({
                name: variable.name,
                kind: variable.kind,
                value: value === undefined ? '<location unavailable>' : `0x${BigInt.asUintN(64, value).toString(16).padStart(16, '0')}`,
                location: location || undefined,
                typeName: variable.typeName
            });
        }
        return result;
    }

    protected async ensureNativeVariableMap(session: RunSession): Promise<void> {
        if (session.nativeVariables !== undefined) { return; }
        session.nativeVariables = [];
        const pdb = session.nativeDebugMap?.pdb ?? path.join(NOVAORYN_SDK_ROOT, 'Artifacts', 'MinimalKernel', 'MinimalKernel.pdb');
        const image = session.nativeDebugMap?.image ?? path.join(NOVAORYN_SDK_ROOT, 'Artifacts', 'MinimalKernel', 'MinimalKernel.efi');
        const pdbutil = path.join(NOVAORYN_SDK_ROOT, '.toolchain', 'LLVM', 'bin', 'llvm-pdbutil.exe');
        if (!(await this.exists(pdbutil))) {
            session.nativeVariablesMessage = 'llvm-pdbutil is not installed in the bundled LLVM toolchain, so named NativeAOT locals cannot be resolved; native frame slots are shown instead.';
            return;
        }
        if (!(await this.exists(pdb)) || !(await this.exists(image))) {
            session.nativeVariablesMessage = 'Native debug PDB/EFI image is unavailable, so named locals cannot be resolved.';
            return;
        }
        const layout = await this.readPeImageLayout(image);
        if (!layout) {
            session.nativeVariablesMessage = 'The linked EFI image layout could not be read for NativeAOT local-variable address resolution.';
            return;
        }
        const output = await this.captureTool(pdbutil, ['dump', '--symbols', pdb]);
        if (output.exitCode !== 0) {
            session.nativeVariablesMessage = `llvm-pdbutil could not read NativeAOT variable records: ${output.text.trim().slice(0, 240)}`;
            return;
        }
        session.nativeVariables = this.parseNativeVariableRecords(output.text, layout);
        session.nativeVariablesMessage = session.nativeVariables.length > 0
            ? `Named NativeAOT locals/arguments enabled (${session.nativeVariables.length} live-range record(s) loaded from MinimalKernel.pdb).`
            : 'MinimalKernel.pdb contains source lines but no usable NativeAOT local-variable live-range records; native frame slots are shown instead.';
    }

    protected async readPeImageLayout(image: string): Promise<PeImageLayout | undefined> {
        try {
            const bytes = await fs.readFile(image);
            if (bytes.length < 0x100 || bytes.readUInt16LE(0) !== 0x5a4d) { return undefined; }
            const pe = bytes.readUInt32LE(0x3c);
            if (pe + 0x108 > bytes.length || bytes.readUInt32LE(pe) !== 0x00004550) { return undefined; }
            const sectionCount = bytes.readUInt16LE(pe + 6);
            const optionalSize = bytes.readUInt16LE(pe + 20);
            const optional = pe + 24;
            if (bytes.readUInt16LE(optional) !== 0x20b) { return undefined; }
            const imageBase = bytes.readBigUInt64LE(optional + 24);
            const sections = new Map<number, bigint>();
            const sectionTable = optional + optionalSize;
            for (let index = 0; index < sectionCount; index++) {
                const offset = sectionTable + index * 40;
                if (offset + 40 > bytes.length) { break; }
                sections.set(index + 1, imageBase + BigInt(bytes.readUInt32LE(offset + 12)));
            }
            return { imageBase, sections };
        } catch { return undefined; }
    }

    protected parseNativeVariableRecords(text: string, layout: PeImageLayout): NativeVariableLocation[] {
        const records = text.split(/(?=^\s*\d+\s+\|\s+S_)/m);
        const result: NativeVariableLocation[] = [];
        let functionStart: bigint | undefined;
        let functionEnd: bigint | undefined;
        let currentLocal: { name: string; kind: 'local' | 'argument'; typeName?: string } | undefined;
        let frameRegister = 'rbp';
        const linkedAddress = (sectionText: string, offsetText: string): bigint | undefined => {
            const section = Number.parseInt(sectionText, 10);
            const base = layout.sections.get(section);
            if (base === undefined) { return undefined; }
            return base + BigInt(`0x${offsetText}`);
        };
        for (const record of records) {
            const kindMatch = record.match(/^\s*\d+\s+\|\s+(S_[A-Z0-9_]+)/m);
            const kind = kindMatch?.[1] ?? '';
            if (kind === 'S_GPROC32' || kind === 'S_LPROC32' || kind === 'S_GPROC32_ID' || kind === 'S_LPROC32_ID') {
                const proc = record.match(/addr\s*=\s*([0-9]+):([0-9a-fA-F]+),\s*code size\s*=\s*(\d+)/i);
                functionStart = proc ? linkedAddress(proc[1], proc[2]) : undefined;
                functionEnd = functionStart !== undefined && proc ? functionStart + BigInt(proc[3]) : undefined;
                frameRegister = 'rbp';
                currentLocal = undefined;
                continue;
            }
            if (kind === 'S_END') {
                currentLocal = undefined;
                continue;
            }
            if (kind === 'S_FRAMEPROC') {
                const fp = record.match(/(?:local|param) fp reg\s*=\s*([A-Za-z0-9]+)/i);
                if (fp) { frameRegister = fp[1].toLowerCase(); }
                continue;
            }
            if (kind === 'S_LOCAL') {
                const name = record.match(/`([^`]+)`/)?.[1];
                if (!name) { currentLocal = undefined; continue; }
                const flagsText = record.match(/flags\s*=\s*([^\r\n]+)/i)?.[1] ?? '';
                const typeName = record.match(/type\s*=\s*`([^`]+)`/i)?.[1];
                currentLocal = { name, kind: /param/i.test(flagsText) ? 'argument' : 'local', typeName };
                continue;
            }
            if (!currentLocal || functionStart === undefined || functionEnd === undefined) { continue; }
            const range = record.match(/range\s*=\s*\[([0-9]+):([0-9a-fA-F]+),\s*\+?\s*(?:0x)?([0-9a-fA-F]+)\)/i);
            const rangeStart = range ? linkedAddress(range[1], range[2]) : undefined;
            const rangeLength = range ? BigInt(`0x${range[3]}`) : undefined;
            const rangeEnd = rangeStart !== undefined && rangeLength !== undefined ? rangeStart + rangeLength : undefined;
            if (kind === 'S_DEFRANGE_REGISTER' || kind === 'S_DEFRANGE_SUBFIELD_REGISTER') {
                const register = record.match(/register\s*=\s*([A-Za-z][A-Za-z0-9]*)/i)?.[1];
                if (register) result.push({ ...currentLocal, functionStart, functionEnd, rangeStart, rangeEnd, register: register.toLowerCase() });
                continue;
            }
            if (kind === 'S_DEFRANGE_REGISTER_REL') {
                const register = record.match(/register\s*=\s*([A-Za-z][A-Za-z0-9]*)/i)?.[1];
                const offsetText = record.match(/offset\s*=\s*(-?(?:0x)?[0-9a-fA-F]+)/i)?.[1];
                if (register && offsetText) result.push({ ...currentLocal, functionStart, functionEnd, rangeStart, rangeEnd, baseRegister: register.toLowerCase(), offset: this.parseSignedInteger(offsetText) });
                continue;
            }
            if (kind === 'S_DEFRANGE_FRAMEPOINTER_REL' || kind === 'S_DEFRANGE_FRAMEPOINTER_REL_FULL_SCOPE') {
                const offsetText = record.match(/offset\s*=\s*(-?(?:0x)?[0-9a-fA-F]+)/i)?.[1];
                if (offsetText) result.push({ ...currentLocal, functionStart, functionEnd, rangeStart, rangeEnd, baseRegister: frameRegister, offset: this.parseSignedInteger(offsetText) });
            }
        }
        return result;
    }

    protected parseSignedInteger(text: string): bigint {
        const trimmed = text.trim().toLowerCase();
        const negative = trimmed.startsWith('-');
        const body = negative ? trimmed.slice(1) : trimmed;
        const value = body.startsWith('0x') ? BigInt(body) : /^\d+$/.test(body) ? BigInt(body) : BigInt(`0x${body}`);
        return negative ? -value : value;
    }

    protected x64RegisterIndex(name: string): number | undefined {
        const names = ['rax','rbx','rcx','rdx','rsi','rdi','rbp','rsp','r8','r9','r10','r11','r12','r13','r14','r15','rip','rflags'];
        const index = names.indexOf(name.toLowerCase().replace(/^cv_/, ''));
        return index >= 0 ? index : undefined;
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

    protected formatAddress(value: bigint): string {
        return `0x${BigInt.asUintN(64, value).toString(16).padStart(16, '0')}`;
    }

    protected safeNumber(value: bigint): number {
        const max = BigInt(Number.MAX_SAFE_INTEGER);
        return Number(value > max ? max : value < 0n ? 0n : value);
    }

    protected async qemuMonitor(gdb: GdbRspClient, monitorCommand: string): Promise<string> {
        const encoded = Buffer.from(monitorCommand, 'utf8').toString('hex');
        const reply = await gdb.command(`qRcmd,${encoded}`);
        if (/^E[0-9a-f]+$/i.test(reply)) throw new Error(`QEMU monitor rejected "${monitorCommand}": ${reply}`);
        return reply;
    }

    protected async readPhysicalU64(gdb: GdbRspClient, physicalAddress: bigint): Promise<bigint> {
        const output = await this.qemuMonitor(gdb, `xp /1gx 0x${physicalAddress.toString(16)}`);
        const values = Array.from(output.matchAll(/0x([0-9a-fA-F]{1,16})/g)).map(match => BigInt(`0x${match[1]}`));
        if (values.length === 0) throw new Error(`QEMU could not read physical memory at ${this.formatAddress(physicalAddress)}.`);
        // HMP prints the requested address followed by the value. If both are prefixed with 0x,
        // the last 64-bit value is the memory contents.
        return values[values.length - 1];
    }

    protected async readMemoryChunked(gdb: GdbRspClient, address: bigint, length: number, chunkSize = 512): Promise<Buffer> {
        const parts: Buffer[] = [];
        let offset = 0;
        while (offset < length) {
            const count = Math.min(chunkSize, length - offset);
            const part = await this.readMemory(gdb, address + BigInt(offset), count);
            if (part.length !== count) break;
            parts.push(part);
            offset += count;
        }
        return Buffer.concat(parts);
    }

    protected async ensureNativeGlobalSymbols(session: RunSession): Promise<void> {
        if (session.nativeGlobalsLoaded) return;
        session.nativeGlobalsLoaded = true;
        session.nativeGlobals = [];
        const pdb = session.nativeDebugMap?.pdb ?? path.join(NOVAORYN_SDK_ROOT, 'Artifacts', 'MinimalKernel', 'MinimalKernel.pdb');
        const image = session.nativeDebugMap?.image ?? path.join(NOVAORYN_SDK_ROOT, 'Artifacts', 'MinimalKernel', 'MinimalKernel.efi');
        const pdbutil = path.join(NOVAORYN_SDK_ROOT, '.toolchain', 'LLVM', 'bin', 'llvm-pdbutil.exe');
        if (await this.exists(pdbutil) && await this.exists(pdb) && await this.exists(image)) {
            const layout = await this.readPeImageLayout(image);
            if (layout) {
                const output = await this.captureTool(pdbutil, ['dump', '--symbols', pdb]);
                if (output.exitCode === 0) session.nativeGlobals.push(...this.parseNativeGlobalSymbols(output.text, layout));
            }
        }
        // Some NativeAOT toolchain revisions omit static data from CodeView but retain it in the linker map.
        // Supplement the PDB with any KernelHeap data symbols that can be recognized there.
        try {
            const mapText = await fs.readFile(path.join(NOVAORYN_SDK_ROOT, 'Artifacts', 'MinimalKernel', 'MinimalKernel.map'), 'utf8');
            for (const suffix of ['_state', '_committed', '_allocated', '_peak', '_live', '_initialized', '_status']) {
                if (this.findHeapGlobal(session, suffix)) continue;
                for (const line of mapText.split(/\r?\n/)) {
                    if (!/KernelHeap/i.test(line) || !line.includes(suffix)) continue;
                    const values = Array.from(line.matchAll(/(?:0x)?([0-9a-fA-F]{8,16})/g)).map(match => BigInt(`0x${match[1]}`));
                    const linkedAddress = values.find(value => value >= 0x100000000n);
                    if (linkedAddress !== undefined) {
                        session.nativeGlobals.push({ name: `KernelHeap${suffix}`, linkedAddress });
                        break;
                    }
                }
            }
            for (const [component, suffixes] of [
                ['KernelInterruptDispatch', ['_initialized','_localApicBase','_callbacks','_cookies','_allocated']],
                ['KernelInterruptBroker', ['_initialized','_localApic','_ioApic','_x2Apic','_routes','_capacity','_count','_ioApics','_ioApicCount']],
                ['KernelSystemCalls', ['_registry','_initialized','_smapEnabled','_stateAddress','_stackBase','_stackTop','_configuredProcessors']]
            ] as Array<[string,string[]]>) {
                for (const suffix of suffixes) {
                    if (this.findKernelGlobal(session, component, suffix)) continue;
                    for (const line of mapText.split(/\r?\n/)) {
                        if (!line.includes(component) || !line.includes(suffix)) continue;
                        const values = Array.from(line.matchAll(/(?:0x)?([0-9a-fA-F]{8,16})/g)).map(match => BigInt(`0x${match[1]}`));
                        const linkedAddress = values.find(value => value >= 0x100000000n);
                        if (linkedAddress !== undefined) { session.nativeGlobals.push({ name: `${component}${suffix}`, linkedAddress }); break; }
                    }
                }
            }
        } catch { }
    }

    protected parseNativeGlobalSymbols(text: string, layout: PeImageLayout): NativeGlobalSymbol[] {
        const records = text.split(/(?=^\s*\d+\s+\|\s+S_)/m);
        const result: NativeGlobalSymbol[] = [];
        for (const record of records) {
            if (!/^\s*\d+\s+\|\s+S_(?:GDATA32|LDATA32)/m.test(record)) continue;
            const addr = /addr\s*=\s*([0-9]+):([0-9a-fA-F]+)/i.exec(record);
            const name = /`([^`]+)`/.exec(record)?.[1];
            if (!addr || !name) continue;
            const sectionBase = layout.sections.get(Number.parseInt(addr[1], 10));
            if (sectionBase === undefined) continue;
            result.push({ name, linkedAddress: sectionBase + BigInt(`0x${addr[2]}`) });
        }
        return result;
    }

    protected findKernelGlobal(session: RunSession, component: string, suffix: string): NativeGlobalSymbol | undefined {
        const globals = session.nativeGlobals ?? [];
        const c=component.toLowerCase(), s=suffix.toLowerCase();
        return globals.find(item => item.name.toLowerCase().includes(c) && item.name.toLowerCase().endsWith(s))
            ?? globals.find(item => item.name.toLowerCase().includes(c) && item.name.toLowerCase().includes(s));
    }

    protected findHeapGlobal(session: RunSession, suffix: string): NativeGlobalSymbol | undefined {
        const globals = session.nativeGlobals ?? [];
        const exactish = globals.find(item => /KernelHeap/i.test(item.name) && item.name.toLowerCase().endsWith(suffix.toLowerCase()));
        if (exactish) return exactish;
        return globals.find(item => /KernelHeap/i.test(item.name) && item.name.toLowerCase().includes(suffix.toLowerCase()));
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

    protected latestSessionForProject(projectPath: string): RunSession | undefined {
        const root = path.resolve(projectPath);
        return Array.from(this.runSessions.values()).filter(item => item.projectRoot === root).sort((a, b) => b.startedAtMs - a.startedAtMs)[0];
    }

    protected elapsedMs(session: RunSession): number { return Math.max(0, Date.now() - session.startedAtMs); }

    protected ingestTelemetry(session: RunSession, text: string): void {
        session.telemetryBuffer += text.replace(/\r/g, '');
        const lines = session.telemetryBuffer.split('\n');
        session.telemetryBuffer = lines.pop() ?? '';
        for (const raw of lines) {
            const line = raw.trim(); if (!line) continue;
            const now = this.elapsedMs(session);
            if (this.ingestStructuredTelemetry(session, line, now)) continue;
            this.ingestBootMilestone(session, line, now);
        }
    }

    protected ingestStructuredTelemetry(session: RunSession, line: string, now: number): boolean {
        const match = /^\[NOVAORYN:(TRACE|BOOT|PROFILE)\]\s*(.*)$/i.exec(line); if (!match) return false;
        const kind = match[1].toUpperCase(); const values = this.parseTelemetryFields(match[2]);
        const timestamp = Number(values['ms'] ?? values['timestamp_ms'] ?? now); const cpu = values['cpu'] !== undefined ? Number(values['cpu']) : undefined;
        if (kind === 'BOOT') {
            const name = values['stage'] ?? values['name'] ?? 'Boot'; const phase = (values['phase'] ?? 'end').toLowerCase();
            if (phase === 'begin') this.beginBootStage(session, name, Number.isFinite(timestamp) ? timestamp : now, values['details']);
            else this.endBootStage(session, name, Number.isFinite(timestamp) ? timestamp : now, (values['status'] as NovaOrynBootStage['status']) || 'complete', values['details']);
            return true;
        }
        if (kind === 'TRACE') {
            this.pushTraceEvent(session, { id: 0, timestampMs: Number.isFinite(timestamp) ? timestamp : now, category: this.traceCategory(values['category']), name: values['name'] ?? values['event'] ?? 'event', phase: this.tracePhase(values['phase']), cpuIndex: Number.isFinite(cpu) ? cpu : undefined, durationMs: this.numberField(values, 'duration_ms'), details: values['details'] });
            return true;
        }
        const subtype = (values['kind'] ?? values['type'] ?? 'sample').toLowerCase();
        if (subtype === 'sample') {
            const name = values['function'] ?? values['name'] ?? values['symbol'] ?? 'unknown'; const category = values['category'] ?? 'cpu'; const duration = this.numberField(values, 'duration_ms') ?? 0;
            const item = session.profileSamples.get(name) ?? { samples: 0, totalDurationMs: 0, category }; item.samples++; item.totalDurationMs += duration; session.profileSamples.set(name, item);
            if (Number.isFinite(cpu)) { const c = session.profileCpuSamples.get(cpu!) ?? { samples: 0, busySamples: 0 }; c.samples++; c.busySamples += values['idle'] === '1' || values['idle'] === 'true' ? 0 : 1; session.profileCpuSamples.set(cpu!, c); }
        } else {
            const name = values['name'] ?? subtype; const category = values['category'] ?? subtype; const delta = this.numberField(values, 'delta') ?? 1; const duration = this.numberField(values, 'duration_ms') ?? 0;
            const counter = session.profileCounters.get(name) ?? { category, count: 0, totalDurationMs: 0 }; counter.count += delta; counter.totalDurationMs += duration; session.profileCounters.set(name, counter);
        }
        return true;
    }

    protected parseTelemetryFields(text: string): Record<string, string> {
        const result: Record<string, string> = {}; const regex = /([A-Za-z0-9_.-]+)=(?:"([^"]*)"|'([^']*)'|([^\s]+))/g; let m: RegExpExecArray | null;
        while ((m = regex.exec(text))) result[m[1].toLowerCase()] = m[2] ?? m[3] ?? m[4] ?? '';
        return result;
    }
    protected numberField(values: Record<string, string>, name: string): number | undefined { const value = Number(values[name]); return Number.isFinite(value) ? value : undefined; }
    protected traceCategory(value?: string): NovaOrynTraceEvent['category'] { const allowed = new Set(['boot','interrupt','syscall','scheduler','driver','memory','storage','network','graphics','diagnostic','custom']); return allowed.has((value ?? '').toLowerCase()) ? (value!.toLowerCase() as NovaOrynTraceEvent['category']) : 'custom'; }
    protected tracePhase(value?: string): NovaOrynTraceEvent['phase'] { const v=(value ?? 'instant').toLowerCase(); return v === 'begin' || v === 'end' ? v : 'instant'; }
    protected pushTraceEvent(session: RunSession, event: NovaOrynTraceEvent): void { event.id = session.traceEvents.length ? session.traceEvents[session.traceEvents.length - 1].id + 1 : 1; session.traceEvents.push(event); if (session.traceEvents.length > 25000) session.traceEvents.splice(0, session.traceEvents.length - 25000); }

    protected beginBootStage(session: RunSession, name: string, at: number, details?: string): void {
        session.currentBootStage = name; session.bootStages.set(name, { name, startMs: at, status: 'running', details });
        this.pushTraceEvent(session, { id: 0, timestampMs: at, category: 'boot', name, phase: 'begin', details });
    }
    protected endBootStage(session: RunSession, name: string, at: number, status: NovaOrynBootStage['status'] = 'complete', details?: string): void {
        const current = session.bootStages.get(name); const startMs = current?.startMs ?? at; const durationMs = Math.max(0, at - startMs);
        session.bootStages.set(name, { name, startMs, endMs: at, durationMs, status, details: details ?? current?.details }); session.currentBootStage = undefined;
        this.pushTraceEvent(session, { id: 0, timestampMs: at, category: 'boot', name, phase: 'end', durationMs, details });
        const key = `boot:${name}`; const sample = session.profileSamples.get(key) ?? { samples: 0, totalDurationMs: 0, category: 'boot' }; sample.samples++; sample.totalDurationMs += durationMs; session.profileSamples.set(key, sample);
    }

    protected ingestBootMilestone(session: RunSession, line: string, now: number): void {
        const milestones: Array<[string, string]> = [
            ['NovaOryn KMain started.', 'Kernel entry'], ['Final UEFI memory map retained', 'UEFI handoff'], ['GDT and TSS installed.', 'CPU descriptors'], ['IDT with 256 vectors installed.', 'Interrupt table'], ['Legacy PIC masked', 'Interrupt controllers'], ['ACPI MADT, MCFG, HPET, FADT and platform power services online.', 'ACPI / platform'], ['HPET, Local APIC timer, TSC, RTC/CMOS and invariant-TSC clock source online.', 'Timers / clocks'], ['Physical memory manager initialized from final UEFI map.', 'Physical memory'], ['Virtual memory manager attached to active x64 page tables.', 'Virtual memory'], ['Kernel heap status:', 'Kernel heap'], ['SMP and per-CPU state online.', 'SMP / per-CPU'], ['Scheduler and threads online.', 'Scheduler'], ['User/kernel separation online.', 'Protection'], ['System calls online.', 'System calls']
        ];
        const hit = milestones.find(([needle]) => line.includes(needle)); if (!hit) return;
        const name = hit[1];
        if (session.currentBootStage && session.currentBootStage !== name) this.endBootStage(session, session.currentBootStage, now, 'complete');
        if (!session.bootStages.has(name)) this.beginBootStage(session, name, session.lastBootMilestoneMs ?? Math.max(0, now - 0.1));
        this.endBootStage(session, name, now, line.includes('[FAIL]') ? 'failed' : line.includes('[WARN]') ? 'warning' : 'complete', line);
        session.lastBootMilestoneMs = now;
    }

    protected traceSnapshotForSession(session: RunSession): NovaOrynTraceSnapshot {
        return { active: !session.complete, sessionId: session.sessionId, capturedAtUtc: new Date().toISOString(), elapsedMs: this.elapsedMs(session), events: session.traceEvents.map(item => ({ ...item })), bootStages: Array.from(session.bootStages.values()).sort((a,b)=>a.startMs-b.startMs).map(item => ({ ...item })), message: session.traceEvents.length ? undefined : 'Waiting for NovaOryn kernel trace telemetry…' };
    }
    protected profilerSnapshotForSession(session: RunSession): NovaOrynProfilerSnapshot {
        const raw = Array.from(session.profileSamples.entries()); const totalSamples = raw.reduce((sum,[,v])=>sum+v.samples,0); const totalDuration = raw.reduce((sum,[,v])=>sum+v.totalDurationMs,0);
        const functions: NovaOrynProfilerFunction[] = raw.map(([name,v]) => ({ name: name.startsWith('boot:') ? name.slice(5) : name, category: v.category, samples: v.samples, totalDurationMs: v.totalDurationMs, averageDurationMs: v.samples ? v.totalDurationMs/v.samples : 0, percent: totalDuration > 0 ? v.totalDurationMs/totalDuration*100 : totalSamples ? v.samples/totalSamples*100 : 0 })).sort((a,b)=>b.percent-a.percent);
        const cpus: NovaOrynProfilerCpu[] = Array.from(session.profileCpuSamples.entries()).map(([cpuIndex,v])=>({ cpuIndex, samples:v.samples, busySamples:v.busySamples, utilisationPercent:v.samples ? v.busySamples/v.samples*100 : 0 })).sort((a,b)=>a.cpuIndex-b.cpuIndex);
        const counters: NovaOrynProfilerCounter[] = Array.from(session.profileCounters.entries()).map(([name,v])=>({ name, category:v.category, count:v.count, totalDurationMs:v.totalDurationMs || undefined, averageDurationMs:v.count && v.totalDurationMs ? v.totalDurationMs/v.count : undefined })).sort((a,b)=>b.count-a.count);
        const stages=Array.from(session.bootStages.values()).filter(s=>s.endMs!==undefined); const bootDurationMs=stages.length ? Math.max(...stages.map(s=>s.endMs!))-Math.min(...stages.map(s=>s.startMs)) : undefined;
        return { active: !session.complete, sessionId: session.sessionId, capturedAtUtc:new Date().toISOString(), elapsedMs:this.elapsedMs(session), totalSamples, functions, cpus, counters, bootDurationMs, message: totalSamples || counters.length ? undefined : 'Boot timing is collected automatically. Runtime CPU/function/counter profiling appears when the kernel emits [NOVAORYN:PROFILE] telemetry.' };
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
