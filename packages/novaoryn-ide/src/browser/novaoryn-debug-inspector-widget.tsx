import * as React from 'react';
import { inject, injectable, postConstruct } from 'inversify';
import { ReactWidget } from '@theia/core/lib/browser/widgets/react-widget';
import { FileUri } from '@theia/core/lib/common/file-uri';
import { EditorManager } from '@theia/editor/lib/browser';
import { NovaOrynDebugState } from '../common/novaoryn-protocol';

@injectable()
export class NovaOrynDebugInspectorWidget extends ReactWidget {
    static readonly ID = 'novaoryn.debug.inspector';
    static readonly LABEL = 'NovaOryn Debug';

    @inject(EditorManager)
    protected readonly editorManager!: EditorManager;

    protected state: NovaOrynDebugState = { active: false, paused: false, sourceSymbols: false };

    @postConstruct()
    protected init(): void {
        this.id = NovaOrynDebugInspectorWidget.ID;
        this.title.label = NovaOrynDebugInspectorWidget.LABEL;
        this.title.caption = 'NovaOryn call stack, locals/frame values and registers';
        this.title.closable = true;
        this.addClass('novaoryn-debug-inspector-widget');
        this.update();
    }

    setState(state: NovaOrynDebugState): void {
        this.state = state;
        this.update();
    }

    protected async openFrame(sourcePath: string | undefined, line: number | undefined): Promise<void> {
        if (!sourcePath || !line) { return; }
        const editor = await this.editorManager.open(FileUri.create(sourcePath));
        const position = { line: Math.max(0, line - 1), character: 0 };
        editor.editor.cursor = position;
        editor.editor.revealPosition(position);
    }

    protected render(): React.ReactNode {
        if (!this.state.active) {
            return <div className='novaoryn-debug-inspector-empty'>Start a NovaOryn Debug session to inspect the kernel.</div>;
        }
        if (!this.state.paused) {
            return <div className='novaoryn-debug-inspector-empty'>Kernel running. Pause or stop at a breakpoint to inspect state.</div>;
        }

        return <div className='novaoryn-debug-inspector'>
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
