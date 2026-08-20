# NovaOryn IDE 0.10.8

## Bottom-panel controls mount on the actual Theia shell panel

Earlier versions searched for a guessed `#theia-bottom-panel` element. Eclipse Theia 1.74 exposes the real bottom area through `ApplicationShell.bottomPanel`.

0.10.8 mounts the NovaOryn control strip directly onto `this.shell.bottomPanel.node`, eliminating the DOM-id assumption.

Because Lumino owns the DockPanel child layout, the toolbar is an absolute overlay and the panel receives a matching 31px top inset. Problems, Output, NovaOryn Build, Clear, maximize/restore and Close remain available.

The kernel/device fixes are unchanged. The QEMU VirtIO GPU is now active and device `1AF4:1050` is bound/started.
