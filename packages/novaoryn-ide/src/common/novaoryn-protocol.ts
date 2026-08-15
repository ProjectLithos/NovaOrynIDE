export const NOVAORYN_PROJECT_SERVICE_PATH = '/services/novaoryn-projects';
export const NovaOrynProjectService = Symbol('NovaOrynProjectService');

export type KernelArchitecture = 'monolithic' | 'microkernel';
export type TargetArchitecture = 'x86_64';

export interface NovaOrynProjectConfiguration {
    name: string;
    location: string;
    kernelArchitecture: KernelArchitecture;
    targetArchitecture: TargetArchitecture;
}

export interface NovaOrynProjectResult {
    success: boolean;
    projectPath?: string;
    error?: string;
}

export interface NovaOrynProjectService {
    createProject(configuration: NovaOrynProjectConfiguration): Promise<NovaOrynProjectResult>;
}
