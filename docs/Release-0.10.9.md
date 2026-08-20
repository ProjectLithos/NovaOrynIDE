# NovaOryn IDE 0.10.9

## Bottom-panel Close control TypeScript fix

0.10.8 correctly mounts NovaOryn's control strip on the real
`ApplicationShell.bottomPanel.node`, but its Close button called
`ApplicationShell.collapseBottomPanel()`. That member is protected and therefore
cannot be called from `NovaOrynContribution`.

0.10.9 uses the public Lumino widget API:

`this.shell.bottomPanel.hide()`

The direct shell-node mount, Problems/Output switching, Clear control, and
maximize/restore behaviour from 0.10.8 remain unchanged.
