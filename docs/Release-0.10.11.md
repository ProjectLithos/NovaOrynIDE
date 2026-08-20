# NovaOryn IDE 0.10.11

## Problems / Output command activation build fix

Theia 1.74 `CommandService` exposes `executeCommand(...)` but does not expose
`getCommand(...)`. The 0.10.10 Problems/Output switcher therefore failed to
compile.

0.10.11 tries the compatible Problems/Output command IDs directly with
`executeCommand(...)`; unknown IDs are caught and the next compatible ID is
tried. If none works, the existing `ApplicationShell.activateWidget(...)`
fallback activates the real bottom widget.

Problems and Output remain separate views, and the custom NovaOryn selector
still updates only after a real view activation succeeds.
