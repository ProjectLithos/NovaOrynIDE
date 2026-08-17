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
}

export interface NovaOrynDebugVariable {
    name: string;
    value: string;
    kind: 'local' | 'argument' | 'stack';
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
    configureExceptionBreakpoints(sessionId: string, settings: NovaOrynExceptionBreakpointSettings): Promise<NovaOrynDebugState>;
}
