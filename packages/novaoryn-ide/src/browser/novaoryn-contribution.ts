import { inject, injectable } from '@theia/core/shared/inversify';
import { Command, CommandContribution, CommandRegistry, MenuContribution, MenuModelRegistry } from '@theia/core/lib/common';
import { AbstractViewContribution, FrontendApplication, FrontendApplicationContribution } from '@theia/core/lib/browser';
import { NovaOrynWidget } from './novaoryn-widget';

export namespace NovaOrynCommands {
    export const OPEN: Command = {
        id: 'novaoryn.openConfigurator',
        label: 'NovaOryn: Create Operating System'
    };
}

@injectable()
export class NovaOrynContribution extends AbstractViewContribution<NovaOrynWidget>
    implements CommandContribution, MenuContribution, FrontendApplicationContribution {

    constructor() {
        super({
            widgetId: NovaOrynWidget.ID,
            widgetName: NovaOrynWidget.LABEL,
            defaultWidgetOptions: { area: 'main' },
            toggleCommandId: NovaOrynCommands.OPEN.id
        });
    }

    registerCommands(commands: CommandRegistry): void {
        commands.registerCommand(NovaOrynCommands.OPEN, {
            execute: () => this.openView({ activate: true, reveal: true })
        });
    }

    registerMenus(menus: MenuModelRegistry): void {
        menus.registerMenuAction(['1_file', '1_new'], {
            commandId: NovaOrynCommands.OPEN.id,
            label: 'NovaOryn Operating System'
        });
    }

    async onStart(_app: FrontendApplication): Promise<void> {
        await this.openView({ activate: true, reveal: true });
    }
}
