export const NOVAORYN_PROJECT_SERVICE_PATH = '/services/novaoryn-projects';
export const NovaOrynProjectService = Symbol('NovaOrynProjectService');
export const NOVAORYN_OS_ROOT = 'C:\\NovaOrynOSes';

export type KernelArchitecture = 'monolithic' | 'microkernel' | 'hybrid';
export type TargetArchitecture = 'x86_64' | 'arm64' | 'riscv64';
export type BootArchitecture = 'uefi' | 'multiboot2' | 'direct';
export type MemorySystem = 'paged' | 'identity-mapped' | 'minimal';
export type SchedulerModel = 'none' | 'cooperative' | 'preemptive' | 'realtime';
export type ProcessSupport = 'none' | 'kernel-threads' | 'processes';
export type SyscallModel = 'novaoryn' | 'linux' | 'windows-nt' | 'multi';
export type InterruptModel = 'architecture-default' | 'apic' | 'x2apic' | 'pic-compat';
export type FilesystemModel = 'none' | 'fatfs' | 'fat32';
export type NetworkStack = 'none' | 'ipv4' | 'dual-stack';
export type ShellModel = 'none' | 'novaoryn-shell';
export type GuiModel = 'none' | 'framebuffer' | 'desktop';
export type AudioModel = 'none' | 'hda' | 'ac97';
export type VirtualisationModel = 'none' | 'guest' | 'hypervisor';
export type SafetyProfile = 'general' | 'rtos' | 'safety-critical';

export interface NovaOrynProjectConfiguration {
    schemaVersion: 2;
    name: string;
    location: string;
    kernelArchitecture: KernelArchitecture;
    targetArchitecture: TargetArchitecture;
    bootArchitecture: BootArchitecture;
    memorySystem: MemorySystem;
    scheduler: SchedulerModel;
    processSupport: ProcessSupport;
    syscallModel: SyscallModel;
    smp: boolean;
    interruptModel: InterruptModel;
    timers: string[];
    drivers: string[];
    storageControllers: string[];
    filesystem: FilesystemModel;
    networkStack: NetworkStack;
    networkDrivers: string[];
    input: string[];
    graphics: string[];
    audio: AudioModel;
    userland: boolean;
    shell: ShellModel;
    gui: GuiModel;
    debugging: string[];
    testing: string[];
    virtualisation: VirtualisationModel;
    safetyProfile: SafetyProfile;
    safetyOptions: string[];
}

export interface NovaOrynOperatingSystem {
    name: string;
    path: string;
    uri: string;
}

export type NovaOrynRunMode = 'run' | 'debug';
export type NovaOrynDebugCommand = 'continue' | 'pause' | 'step-into' | 'step-over' | 'step-out' | 'restart' | 'stop';

export interface NovaOrynDebugRegister {
    name: string;
    value: string;
}

export interface NovaOrynDebugFrame {
    index: number;
    address: string;
    label: string;
    sourcePath?: string;
    line?: number;
    kind?: 'managed' | 'native';
    unwoundBy?: 'x64-unwind' | 'leaf';
}

export interface NovaOrynDebugExecutionContext {
    id: string;
    threadId: string;
    processId?: string;
    cpuIndex?: number;
    name: string;
    current: boolean;
}


export interface NovaOrynDebugVariable {
    name: string;
    value: string;
    kind: 'local' | 'argument' | 'stack';
    location?: string;
    typeName?: string;
}

export interface NovaOrynMemoryReadResult {
    success: boolean;
    expression: string;
    address?: string;
    length?: number;
    bytes?: string;
    error?: string;
}


export interface NovaOrynPageTableEntry {
    level: 'PML4' | 'PDPT' | 'PD' | 'PT';
    index: number;
    entryPhysicalAddress: string;
    entryValue: string;
    present: boolean;
    writable: boolean;
    user: boolean;
    writeThrough: boolean;
    cacheDisable: boolean;
    accessed: boolean;
    dirty: boolean;
    largePage: boolean;
    global: boolean;
    noExecute: boolean;
    targetPhysicalAddress?: string;
}

export interface NovaOrynPageTableInspection {
    success: boolean;
    expression: string;
    virtualAddress?: string;
    cr3?: string;
    pageSize?: string;
    physicalAddress?: string;
    entries?: NovaOrynPageTableEntry[];
    error?: string;
}

export interface NovaOrynHeapBlock {
    index: number;
    state: 'free' | 'allocated';
    address: string;
    byteCount: number;
    token?: string;
}

export interface NovaOrynHeapSnapshot {
    success: boolean;
    initialized?: boolean;
    committedBytes?: number;
    allocatedBytes?: number;
    freeBytes?: number;
    peakAllocatedBytes?: number;
    liveAllocations?: number;
    freeBlocks?: number;
    blocks?: NovaOrynHeapBlock[];
    message?: string;
    error?: string;
}

export interface NovaOrynCrashDumpSummary {
    path: string;
    createdUtc: string;
    reason: string;
    sourcePath?: string;
    line?: number;
}

export interface NovaOrynCrashDumpResult {
    success: boolean;
    dump?: NovaOrynCrashDumpSummary;
    state?: NovaOrynDebugState;
    pageTable?: NovaOrynPageTableInspection;
    heap?: NovaOrynHeapSnapshot;
    memory?: { stack?: NovaOrynMemoryReadResult; code?: NovaOrynMemoryReadResult };
    error?: string;
}

export interface NovaOrynDisassemblyInstruction {
    runtimeAddress: string;
    linkedAddress: string;
    instruction: string;
    sourcePath?: string;
    line?: number;
    current?: boolean;
}

export interface NovaOrynExceptionBreakpointSettings {
    vectors: number[];
    breakOnPanic: boolean;
}

export interface NovaOrynBreakpointRequest {
    sourcePath: string;
    line: number;
    condition?: string;
    hitCondition?: string;
}

export interface NovaOrynExpressionResult {
    success: boolean;
    expression: string;
    value?: string;
    hexValue?: string;
    error?: string;
}

export interface NovaOrynRunResult {
    success: boolean;
    sessionId?: string;
    error?: string;
}

export interface NovaOrynDebugState {
    active: boolean;
    paused: boolean;
    sourceSymbols: boolean;
    breakpoints?: NovaOrynBreakpointResult[];
    gdbPort?: number;
    sourcePath?: string;
    line?: number;
    message?: string;
    registers?: NovaOrynDebugRegister[];
    callStack?: NovaOrynDebugFrame[];
    locals?: NovaOrynDebugVariable[];
    localsMessage?: string;
    disassembly?: NovaOrynDisassemblyInstruction[];
    exceptionVector?: number;
    exceptionName?: string;
    executionContexts?: NovaOrynDebugExecutionContext[];
    selectedThreadId?: string;
}



export interface NovaOrynBreakpointResult {
    success: boolean;
    verified: boolean;
    sourcePath: string;
    line: number;
    resolvedLine?: number;
    address?: string;
    condition?: string;
    hitCondition?: string;
    hitCount?: number;
    message?: string;
}

export interface NovaOrynRunOutput {
    text: string;
    nextOffset: number;
    complete: boolean;
    exitCode?: number;
    error?: string;
}


export interface NovaOrynConfigurationResult {
    success: boolean;
    projectPath?: string;
    configuration?: NovaOrynProjectConfiguration;
    error?: string;
}

export interface NovaOrynTestDescriptor {
    id: string;
    name: string;
    projectPath: string;
    source: 'os' | 'sdk';
    category: string;
}

export interface NovaOrynTestRunResult {
    success: boolean;
    runId?: string;
    error?: string;
}

export interface NovaOrynTestOutput {
    text: string;
    nextOffset: number;
    complete: boolean;
    exitCode?: number;
    error?: string;
}

export interface NovaOrynProjectResult {
    success: boolean;
    projectPath?: string;
    generatedProjects?: string[];
    error?: string;
}

export interface NovaOrynProjectService {
    listOperatingSystems(): Promise<NovaOrynOperatingSystem[]>;
    createProject(configuration: NovaOrynProjectConfiguration): Promise<NovaOrynProjectResult>;
    readProjectConfiguration(projectPath: string): Promise<NovaOrynConfigurationResult>;
    reconfigureProject(projectPath: string, configuration: NovaOrynProjectConfiguration): Promise<NovaOrynProjectResult>;
    runOperatingSystem(projectPath: string, mode: NovaOrynRunMode, breakpoints?: NovaOrynBreakpointRequest[], exceptionBreakpoints?: NovaOrynExceptionBreakpointSettings): Promise<NovaOrynRunResult>;
    readRunOutput(sessionId: string, offset: number): Promise<NovaOrynRunOutput>;
    debugState(sessionId: string): Promise<NovaOrynDebugState>;
    debugCommand(sessionId: string, command: NovaOrynDebugCommand): Promise<NovaOrynDebugState>;
    toggleBreakpoint(sessionId: string, sourcePath: string, line: number, condition?: string, hitCondition?: string): Promise<NovaOrynBreakpointResult>;
    updateBreakpoint(sessionId: string, breakpoint: NovaOrynBreakpointRequest): Promise<NovaOrynBreakpointResult>;
    evaluateExpression(sessionId: string, expression: string): Promise<NovaOrynExpressionResult>;
    readMemoryRange(sessionId: string, addressExpression: string, length: number): Promise<NovaOrynMemoryReadResult>;
    inspectPageTable(sessionId: string, addressExpression: string): Promise<NovaOrynPageTableInspection>;
    inspectHeap(sessionId: string): Promise<NovaOrynHeapSnapshot>;
    captureCrashDump(sessionId: string, reason?: string): Promise<NovaOrynCrashDumpResult>;
    listCrashDumps(projectPath: string): Promise<NovaOrynCrashDumpSummary[]>;
    loadCrashDump(dumpPath: string): Promise<NovaOrynCrashDumpResult>;
    configureExceptionBreakpoints(sessionId: string, settings: NovaOrynExceptionBreakpointSettings): Promise<NovaOrynDebugState>;
    selectExecutionContext(sessionId: string, threadId: string): Promise<NovaOrynDebugState>;
    listTests(projectPath: string): Promise<NovaOrynTestDescriptor[]>;
    runTest(projectPath: string, testId: string): Promise<NovaOrynTestRunResult>;
    readTestOutput(runId: string, offset: number): Promise<NovaOrynTestOutput>;
}

