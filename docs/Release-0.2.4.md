# NovaOryn IDE 0.2.4

## Item 15 correction: analyzer active-OS path hand-off

- Fixes the OS-specific Static Analyzers view incorrectly reporting that no NovaOryn OS is open while an OS workspace is active.
- The NovaOryn frontend contribution now explicitly passes the active OS root into the analyzer widget when the view opens and after workspace startup.
- The analyzer still reads `WorkspaceService.workspace` first and uses the explicit path as a reliable fallback.
- Static-analyzer verification now checks the path hand-off contract.
