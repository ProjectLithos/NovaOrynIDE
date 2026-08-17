import { inject, injectable, postConstruct } from 'inversify';
import { MessageService } from '@theia/core/lib/common/message-service';
import URI from '@theia/core/lib/common/uri';
import { BaseWidget } from '@theia/core/lib/browser/widgets/widget';
import { WorkspaceService } from '@theia/workspace/lib/browser/workspace-service';
import { Message } from '@lumino/messaging';
import {
    AudioModel,
    NOVAORYN_OS_ROOT,
    NovaOrynOperatingSystem,
    BootArchitecture,
    FilesystemModel,
    GuiModel,
    InterruptModel,
    KernelArchitecture,
    MemorySystem,
    NetworkStack,
    NovaOrynProjectConfiguration,
    NovaOrynProjectService,
    ProcessSupport,
    SafetyProfile,
    SchedulerModel,
    ShellModel,
    SyscallModel,
    TargetArchitecture,
    VirtualisationModel
} from '../common/novaoryn-protocol';

export const NOVAORYN_WIDGET_ID = 'novaoryn.project.configurator';
export const NOVAORYN_EXPLICIT_WORKSPACE_OPEN = 'novaoryn.explicitWorkspaceOpen';

type Page = 'startup' | 'configuration';
type Option = readonly [value: string, label: string];

@injectable()
export class NovaOrynWidget extends BaseWidget {
    static readonly ID = NOVAORYN_WIDGET_ID;
    static readonly LABEL = 'NovaOryn OS';

    @inject(NovaOrynProjectService)
    protected readonly projectService!: NovaOrynProjectService;

    @inject(MessageService)
    protected readonly messages!: MessageService;

    @inject(WorkspaceService)
    protected readonly workspaceService!: WorkspaceService;

    protected configuration: NovaOrynProjectConfiguration = this.createDefaultConfiguration();
    protected creating = false;
    protected reconfiguringProjectPath: string | undefined;
    protected page: Page = 'startup';
    protected operatingSystems: NovaOrynOperatingSystem[] = [];
    protected loadingSystems = false;
    protected startupScanStarted = false;

    @postConstruct()
    protected init(): void {
        this.id = NovaOrynWidget.ID;
        this.title.label = NovaOrynWidget.LABEL;
        this.title.caption = 'Create, load and configure a NovaOryn operating system';
        this.title.closable = true;
        this.addClass('novaoryn-widget');
        this.node.tabIndex = 0;
        this.renderContent();
    }

    protected override onAfterAttach(msg: Message): void {
        super.onAfterAttach(msg);
        this.renderContent();
        if (!this.startupScanStarted) {
            this.startupScanStarted = true;
            window.setTimeout(() => void this.refreshOperatingSystems(), 0);
        }
    }

    protected createDefaultConfiguration(): NovaOrynProjectConfiguration {
        return {
            schemaVersion: 2,
            name: 'MyNovaOrynOS',
            location: NOVAORYN_OS_ROOT,
            kernelArchitecture: 'monolithic',
            targetArchitecture: 'x86_64',
            bootArchitecture: 'uefi',
            memorySystem: 'paged',
            scheduler: 'preemptive',
            processSupport: 'processes',
            syscallModel: 'multi',
            smp: true,
            interruptModel: 'apic',
            timers: ['tsc', 'hpet', 'local-apic', 'rtc'],
            drivers: ['pci', 'acpi', 'serial-16550', 'virtio-console', 'virtio-rng'],
            storageControllers: ['virtio-block', 'nvme', 'ahci'],
            filesystem: 'fatfs',
            networkStack: 'dual-stack',
            networkDrivers: ['virtio-net', 'e1000', 'rtl8168'],
            input: ['ps2-keyboard', 'ps2-mouse'],
            graphics: ['uefi-gop', 'generic-framebuffer', 'virtio-gpu'],
            audio: 'none',
            userland: true,
            shell: 'novaoryn-shell',
            gui: 'none',
            debugging: ['serial-log', 'kernel-diagnostics'],
            testing: ['boot-smoke', 'memory', 'interrupts'],
            virtualisation: 'guest',
            safetyProfile: 'general',
            safetyOptions: []
        };
    }

    protected renderContent(): void {
        this.node.replaceChildren();
        if (this.page === 'startup') {
            this.renderStartup();
        } else {
            this.renderConfiguration();
        }
    }

    protected element<K extends keyof HTMLElementTagNameMap>(tag: K, className?: string, text?: string): HTMLElementTagNameMap[K] {
        const element = document.createElement(tag);
        if (className) {
            element.className = className;
        }
        if (text !== undefined) {
            element.textContent = text;
        }
        return element;
    }

    protected renderStartup(): void {
        const page = this.element('div', 'novaoryn-page novaoryn-start-page');
        const card = this.element('div', 'novaoryn-start-card');

        const art = this.element('div', 'novaoryn-start-art');
        art.setAttribute('role', 'img');
        art.setAttribute('aria-label', 'NovaOryn IDE logo');
        card.appendChild(art);

        card.appendChild(this.element('h1', undefined, 'NovaOryn IDE 0.2.3'));
        const intro = this.element('p');
        intro.append('All NovaOryn operating systems are stored beneath ');
        const root = this.element('strong', undefined, NOVAORYN_OS_ROOT);
        intro.appendChild(root);
        intro.append('.');
        card.appendChild(intro);

        const actions = this.element('div', 'novaoryn-start-actions');
        const create = this.button('Create New OS', 'theia-button main', () => this.beginNewOperatingSystem());
        actions.appendChild(create);
        card.appendChild(actions);

        const existing = this.element('section', 'novaoryn-existing');
        existing.appendChild(this.element('h2', undefined, 'Open Existing OS'));
        existing.appendChild(this.element('p', 'novaoryn-existing-help', 'Select a previously created NovaOryn operating system from C:\\NovaOrynOSes\\.'));
        if (this.loadingSystems) {
            existing.appendChild(this.element('p', undefined, 'Looking for NovaOryn operating systems…'));
        } else if (this.operatingSystems.length === 0) {
            existing.appendChild(this.element('p', undefined, 'No previously created NovaOryn operating systems were found.'));
        } else {
            const list = this.element('div', 'novaoryn-os-list');
            for (const os of this.operatingSystems) {
                const entry = this.button('', 'novaoryn-os-entry', () => void this.openOperatingSystem(os));
                entry.appendChild(this.element('strong', undefined, os.name));
                entry.appendChild(this.element('span', undefined, os.path));
                list.appendChild(entry);
            }
            existing.appendChild(list);
        }
        existing.appendChild(this.button('Refresh list', 'theia-button secondary', () => void this.refreshOperatingSystems()));
        card.appendChild(existing);
        page.appendChild(card);
        this.node.appendChild(page);
    }

    protected renderConfiguration(): void {
        const c = this.configuration;
        const page = this.element('div', 'novaoryn-page');
        const card = this.element('div', 'novaoryn-card novaoryn-card-wide');
        card.appendChild(this.element('div', 'novaoryn-brand', 'NOVAORYN'));
        card.appendChild(this.element('h1', undefined, this.reconfiguringProjectPath ? `Reconfigure ${c.name}` : 'NovaOryn OS 0.2.3'));
        card.appendChild(this.element('p', 'novaoryn-version', 'Authoritative operating-system configuration'));
        card.appendChild(this.element('p', undefined, this.reconfiguringProjectPath
            ? 'Change the generated kernel/OS structure below. User-owned source files, including Kernel\\Kernel.cs, are preserved.'
            : 'Every selection below determines which projects and starter source are generated.'));

        const operatingSystem = this.fieldset('Operating system');
        if (this.reconfiguringProjectPath) {
            operatingSystem.appendChild(this.readonlyPath('Operating system name', c.name));
            operatingSystem.appendChild(this.readonlyPath('Operating system folder', this.reconfiguringProjectPath));
        } else {
            operatingSystem.appendChild(this.textInput('Operating system name', c.name, value => c.name = value));
            operatingSystem.appendChild(this.readonlyPath('Operating systems folder', `${NOVAORYN_OS_ROOT}\\<OS name>`));
        }
        operatingSystem.appendChild(this.selectInput('Kernel architecture', c.kernelArchitecture, [
            ['monolithic', 'Monolithic'], ['microkernel', 'Microkernel'], ['hybrid', 'Hybrid']
        ], value => c.kernelArchitecture = value as KernelArchitecture));
        operatingSystem.appendChild(this.selectInput('CPU architecture', c.targetArchitecture, [
            ['x86_64', 'x86-64'], ['arm64', 'ARM64'], ['riscv64', 'RISC-V 64']
        ], value => this.setArchitecture(value as TargetArchitecture), true));
        operatingSystem.appendChild(this.selectInput('Boot architecture', c.bootArchitecture, [
            ['uefi', 'UEFI'], ['multiboot2', 'Multiboot2 (x86-64)'], ['direct', 'Direct / platform boot']
        ], value => c.bootArchitecture = value as BootArchitecture));
        card.appendChild(operatingSystem);

        const kernel = this.fieldset('Kernel core');
        kernel.appendChild(this.selectInput('Memory system', c.memorySystem, [
            ['paged', 'Paged virtual memory'], ['identity-mapped', 'Identity-mapped'], ['minimal', 'Minimal / RTOS memory']
        ], value => c.memorySystem = value as MemorySystem));
        kernel.appendChild(this.selectInput('Scheduler', c.scheduler, [
            ['none', 'None'], ['cooperative', 'Cooperative'], ['preemptive', 'Preemptive'], ['realtime', 'Real-time']
        ], value => c.scheduler = value as SchedulerModel));
        kernel.appendChild(this.selectInput('Process support', c.processSupport, [
            ['none', 'None'], ['kernel-threads', 'Kernel threads only'], ['processes', 'User processes']
        ], value => c.processSupport = value as ProcessSupport));
        kernel.appendChild(this.selectInput('System-call model', c.syscallModel, [
            ['novaoryn', 'NovaOryn Get / Set / Event'], ['linux', 'Linux-compatible model'], ['windows-nt', 'Windows / NT-compatible model'], ['multi', 'All three models']
        ], value => c.syscallModel = value as SyscallModel));
        kernel.appendChild(this.booleanInput('SMP / per-CPU support', c.smp, value => c.smp = value));
        kernel.appendChild(this.selectInput('Interrupt controller', c.interruptModel, this.interruptOptions(), value => c.interruptModel = value as InterruptModel));
        kernel.appendChild(this.multiInput('Timers and clocks', c.timers, [
            ['tsc', 'Invariant TSC'], ['hpet', 'HPET'], ['local-apic', 'Local APIC timer'], ['rtc', 'RTC / CMOS']
        ]));
        card.appendChild(kernel);

        const hardware = this.fieldset('Hardware and drivers');
        hardware.appendChild(this.multiInput('Core drivers', c.drivers, [
            ['pci', 'PCI / PCIe'], ['acpi', 'ACPI'], ['serial-16550', '16550 UART'], ['virtio-console', 'VirtIO console'],
            ['virtio-rng', 'VirtIO RNG'], ['usb-xhci', 'USB xHCI'], ['usb-ehci', 'USB EHCI']
        ]));
        hardware.appendChild(this.multiInput('Storage controllers', c.storageControllers, [
            ['virtio-block', 'VirtIO block'], ['nvme', 'NVMe'], ['ahci', 'AHCI / SATA']
        ]));
        hardware.appendChild(this.selectInput('Filesystem', c.filesystem, [
            ['none', 'None'], ['fatfs', 'FatFS port'], ['fat32', 'Native FAT32']
        ], value => c.filesystem = value as FilesystemModel));
        hardware.appendChild(this.multiInput('Input', c.input, [
            ['ps2-keyboard', 'PS/2 keyboard'], ['ps2-mouse', 'PS/2 mouse'], ['usb-hid-keyboard', 'USB HID keyboard'], ['usb-hid-mouse', 'USB HID mouse']
        ]));
        hardware.appendChild(this.multiInput('Graphics', c.graphics, [
            ['uefi-gop', 'UEFI GOP'], ['generic-framebuffer', 'Generic framebuffer'], ['virtio-gpu', 'VirtIO GPU']
        ]));
        hardware.appendChild(this.selectInput('Audio', c.audio, [
            ['none', 'None'], ['hda', 'Intel HDA'], ['ac97', 'AC97']
        ], value => c.audio = value as AudioModel));
        card.appendChild(hardware);

        const networking = this.fieldset('Networking');
        networking.appendChild(this.selectInput('Network stack', c.networkStack, [
            ['none', 'None'], ['ipv4', 'IPv4'], ['dual-stack', 'IPv4 + IPv6']
        ], value => this.setNetworkStack(value as NetworkStack), true));
        networking.appendChild(this.multiInput('Network adapters', c.networkDrivers, [
            ['virtio-net', 'VirtIO net'], ['e1000', 'Intel E1000/E1000e'], ['rtl8168', 'Realtek RTL8168/RTL8111']
        ], c.networkStack === 'none'));
        card.appendChild(networking);

        const userland = this.fieldset('Userland');
        userland.appendChild(this.booleanInput('Generate userland', c.userland, value => this.setUserland(value), true));
        userland.appendChild(this.selectInput('Shell', c.shell, [
            ['none', 'None'], ['novaoryn-shell', 'NovaOryn shell']
        ], value => c.shell = value as ShellModel, false, !c.userland));
        userland.appendChild(this.selectInput('GUI', c.gui, [
            ['none', 'None'], ['framebuffer', 'Framebuffer UI'], ['desktop', 'Desktop GUI']
        ], value => c.gui = value as GuiModel, false, !c.userland));
        card.appendChild(userland);

        const diagnostics = this.fieldset('Diagnostics, testing and virtualisation');
        diagnostics.appendChild(this.multiInput('Debugging', c.debugging, [
            ['serial-log', 'Serial logging'], ['kernel-diagnostics', 'Kernel diagnostics'], ['symbols', 'Debug symbols'], ['panic-dump', 'Panic dump']
        ]));
        diagnostics.appendChild(this.multiInput('Individual test programs', c.testing, [
            ['boot-smoke', 'Boot smoke'], ['memory', 'Memory'], ['interrupts', 'Interrupts'], ['scheduler', 'Scheduler'], ['drivers', 'Drivers'], ['network', 'Networking']
        ]));
        diagnostics.appendChild(this.selectInput('Virtualisation', c.virtualisation, [
            ['none', 'None'], ['guest', 'Virtual-machine guest support'], ['hypervisor', 'NovaOryn hypervisor']
        ], value => c.virtualisation = value as VirtualisationModel));
        card.appendChild(diagnostics);

        const safety = this.fieldset('RTOS and safety');
        safety.appendChild(this.selectInput('Safety profile', c.safetyProfile, [
            ['general', 'General purpose'], ['rtos', 'RTOS'], ['safety-critical', 'Safety critical']
        ], value => this.setSafetyProfile(value as SafetyProfile), true));
        safety.appendChild(this.multiInput('Safety options', c.safetyOptions, [
            ['deterministic-scheduling', 'Deterministic scheduling'], ['watchdog', 'Watchdog'], ['memory-protection', 'Memory protection'],
            ['redundant-checks', 'Redundant consistency checks'], ['fail-stop', 'Fail-stop policy'], ['no-dynamic-allocation', 'No dynamic allocation after startup']
        ]));
        card.appendChild(safety);

        const actions = this.element('div', 'novaoryn-actions');
        const back = this.button(this.reconfiguringProjectPath ? 'Cancel' : 'Back', 'theia-button secondary', () => {
            if (this.reconfiguringProjectPath) {
                this.close();
            } else {
                this.page = 'startup';
                this.renderContent();
            }
        });
        const actionLabel = this.reconfiguringProjectPath ? 'Apply Configuration' : 'Generate NovaOryn OS';
        const busyLabel = this.reconfiguringProjectPath ? 'Applying…' : 'Generating…';
        const generate = this.button(this.creating ? busyLabel : actionLabel, 'theia-button main', () => void this.createProject());
        generate.disabled = this.creating;
        actions.append(back, generate);
        card.appendChild(actions);

        const note = this.element('div', 'novaoryn-note');
        note.textContent = this.reconfiguringProjectPath
            ? 'Reconfiguration rewrites NovaOryn-owned configuration/project artifacts and removes obsolete generated component files. User-owned source is preserved.'
            : 'The generator writes NovaOryn.json and NovaOryn.ProjectGraph.json and emits only the projects/source selected above. Monolithic, microkernel and hybrid selections produce materially different project layouts.';
        card.appendChild(note);

        page.appendChild(card);
        this.node.appendChild(page);
    }

    protected fieldset(legendText: string): HTMLFieldSetElement {
        const fieldset = this.element('fieldset');
        fieldset.appendChild(this.element('legend', undefined, legendText));
        return fieldset;
    }

    protected button(text: string, className: string, click: () => void): HTMLButtonElement {
        const button = this.element('button', className, text);
        button.type = 'button';
        button.addEventListener('click', click);
        return button;
    }

    protected textInput(label: string, value: string, update: (value: string) => void): HTMLDivElement {
        const field = this.element('div', 'novaoryn-field');
        field.appendChild(this.element('label', undefined, label));
        const input = this.element('input', 'theia-input');
        input.type = 'text';
        input.value = value;
        input.addEventListener('input', () => update(input.value));
        field.appendChild(input);
        return field;
    }

    protected readonlyPath(label: string, value: string): HTMLDivElement {
        const field = this.element('div', 'novaoryn-field');
        field.appendChild(this.element('label', undefined, label));
        field.appendChild(this.element('div', 'novaoryn-readonly-path', value));
        return field;
    }

    protected selectInput(label: string, value: string, options: Option[], update: (value: string) => void, rerender = false, disabled = false): HTMLDivElement {
        const field = this.element('div', 'novaoryn-field');
        field.appendChild(this.element('label', undefined, label));
        const select = this.element('select', 'theia-select');
        select.disabled = disabled;
        for (const [optionValue, optionLabel] of options) {
            const option = this.element('option', undefined, optionLabel);
            option.value = optionValue;
            option.selected = optionValue === value;
            select.appendChild(option);
        }
        select.addEventListener('change', () => {
            update(select.value);
            if (rerender) {
                this.renderContent();
            }
        });
        field.appendChild(select);
        return field;
    }

    protected booleanInput(label: string, checked: boolean, update: (value: boolean) => void, rerender = false): HTMLLabelElement {
        const wrapper = this.element('label', 'novaoryn-check');
        const input = this.element('input');
        input.type = 'checkbox';
        input.checked = checked;
        input.addEventListener('change', () => {
            update(input.checked);
            if (rerender) {
                this.renderContent();
            }
        });
        wrapper.append(input, document.createTextNode(` ${label}`));
        return wrapper;
    }

    protected multiInput(label: string, selected: string[], options: Option[], disabled = false): HTMLDivElement {
        const wrapper = this.element('div', 'novaoryn-multi');
        wrapper.appendChild(this.element('label', undefined, label));
        const grid = this.element('div', 'novaoryn-check-grid');
        for (const [value, text] of options) {
            const row = this.element('label', 'novaoryn-check');
            const input = this.element('input');
            input.type = 'checkbox';
            input.checked = selected.includes(value);
            input.disabled = disabled;
            input.addEventListener('change', () => this.toggleSelection(selected, value, input.checked));
            row.append(input, document.createTextNode(` ${text}`));
            grid.appendChild(row);
        }
        wrapper.appendChild(grid);
        return wrapper;
    }

    protected toggleSelection(selected: string[], value: string, enabled: boolean): void {
        const index = selected.indexOf(value);
        if (enabled && index < 0) {
            selected.push(value);
        } else if (!enabled && index >= 0) {
            selected.splice(index, 1);
        }
    }

    protected beginNewOperatingSystem(): void {
        this.reconfiguringProjectPath = undefined;
        this.configuration = this.createDefaultConfiguration();
        this.page = 'configuration';
        this.renderContent();
    }

    async beginReconfigureOperatingSystem(projectPath: string): Promise<boolean> {
        try {
            const result = await this.projectService.readProjectConfiguration(projectPath);
            if (!result.success || !result.configuration || !result.projectPath) {
                await this.messages.error(`Could not load NovaOryn OS configuration: ${result.error ?? 'Unknown error'}`);
                return false;
            }
            this.configuration = {
                ...result.configuration,
                timers: [...result.configuration.timers],
                drivers: [...result.configuration.drivers],
                storageControllers: [...result.configuration.storageControllers],
                networkDrivers: [...result.configuration.networkDrivers],
                input: [...result.configuration.input],
                graphics: [...result.configuration.graphics],
                debugging: [...result.configuration.debugging],
                testing: [...result.configuration.testing],
                safetyOptions: [...result.configuration.safetyOptions]
            };
            this.reconfiguringProjectPath = result.projectPath;
            this.page = 'configuration';
            this.renderContent();
            return true;
        } catch (error) {
            await this.messages.error(`Could not load NovaOryn OS configuration: ${error instanceof Error ? error.message : String(error)}`);
            return false;
        }
    }

    protected async refreshOperatingSystems(): Promise<void> {
        this.loadingSystems = true;
        this.renderContent();
        try {
            this.operatingSystems = await this.projectService.listOperatingSystems();
        } catch (error) {
            this.operatingSystems = [];
            await this.messages.error(`Could not scan ${NOVAORYN_OS_ROOT}: ${error instanceof Error ? error.message : String(error)}`);
        } finally {
            this.loadingSystems = false;
            this.renderContent();
        }
    }

    protected async openOperatingSystem(os: NovaOrynOperatingSystem): Promise<void> {
        window.sessionStorage.setItem(NOVAORYN_EXPLICIT_WORKSPACE_OPEN, os.uri);
        this.workspaceService.open(new URI(os.uri), { preserveWindow: true });
    }

    protected setArchitecture(architecture: TargetArchitecture): void {
        this.configuration.targetArchitecture = architecture;
        if (architecture !== 'x86_64') {
            this.configuration.interruptModel = 'architecture-default';
            if (this.configuration.bootArchitecture === 'multiboot2') {
                this.configuration.bootArchitecture = 'uefi';
            }
            this.configuration.timers = this.configuration.timers.filter(timer => timer !== 'local-apic');
        } else if (this.configuration.interruptModel === 'architecture-default') {
            this.configuration.interruptModel = 'apic';
        }
    }

    protected interruptOptions(): Option[] {
        return this.configuration.targetArchitecture === 'x86_64'
            ? [['architecture-default', 'Architecture default'], ['apic', 'Local APIC + I/O APIC + MSI/MSI-X'], ['x2apic', 'x2APIC + I/O APIC + MSI/MSI-X'], ['pic-compat', 'Legacy PIC compatibility']]
            : [['architecture-default', 'Architecture default']];
    }

    protected setNetworkStack(stack: NetworkStack): void {
        this.configuration.networkStack = stack;
        if (stack === 'none') {
            this.configuration.networkDrivers = [];
        }
    }

    protected setUserland(enabled: boolean): void {
        this.configuration.userland = enabled;
        if (!enabled) {
            this.configuration.shell = 'none';
            this.configuration.gui = 'none';
        }
    }

    protected setSafetyProfile(profile: SafetyProfile): void {
        this.configuration.safetyProfile = profile;
        if (profile === 'rtos' && this.configuration.scheduler !== 'realtime') {
            this.configuration.scheduler = 'realtime';
        }
        if (profile === 'safety-critical') {
            this.configuration.scheduler = 'realtime';
            for (const option of ['deterministic-scheduling', 'watchdog', 'memory-protection', 'redundant-checks', 'fail-stop']) {
                if (!this.configuration.safetyOptions.includes(option)) {
                    this.configuration.safetyOptions.push(option);
                }
            }
        }
    }

    protected async createProject(): Promise<void> {
        if (this.creating) {
            return;
        }
        const name = this.configuration.name.trim();
        if (!name) {
            await this.messages.error('Enter an operating system name.');
            return;
        }
        this.creating = true;
        this.renderContent();
        try {
            const updatedConfiguration = {
                ...this.configuration,
                name,
                location: NOVAORYN_OS_ROOT,
                timers: [...this.configuration.timers],
                drivers: [...this.configuration.drivers],
                storageControllers: [...this.configuration.storageControllers],
                networkDrivers: [...this.configuration.networkDrivers],
                input: [...this.configuration.input],
                graphics: [...this.configuration.graphics],
                debugging: [...this.configuration.debugging],
                testing: [...this.configuration.testing],
                safetyOptions: [...this.configuration.safetyOptions]
            };
            const reconfiguring = !!this.reconfiguringProjectPath;
            const result = reconfiguring
                ? await this.projectService.reconfigureProject(this.reconfiguringProjectPath!, updatedConfiguration)
                : await this.projectService.createProject(updatedConfiguration);
            if (result.success) {
                if (reconfiguring) {
                    await this.messages.info(`NovaOryn OS reconfigured with ${result.generatedProjects?.length ?? 0} generated component projects. User source was preserved.`);
                    this.configuration = updatedConfiguration;
                    this.renderContent();
                } else {
                    await this.messages.info(`NovaOryn OS generated at ${result.projectPath} with ${result.generatedProjects?.length ?? 0} component projects.`);
                    await this.refreshOperatingSystems();
                    if (result.projectPath) {
                        const os = this.operatingSystems.find(candidate => candidate.path.toLowerCase() === result.projectPath!.toLowerCase());
                        if (os) {
                            await this.openOperatingSystem(os);
                        }
                    }
                }
            } else {
                await this.messages.error(`Could not ${reconfiguring ? 'reconfigure' : 'generate'} NovaOryn OS: ${result.error ?? 'Unknown error'}`);
            }
        } catch (error) {
            await this.messages.error(`Could not ${this.reconfiguringProjectPath ? 'reconfigure' : 'generate'} NovaOryn OS: ${error instanceof Error ? error.message : String(error)}`);
        } finally {
            this.creating = false;
            this.renderContent();
        }
    }
}
