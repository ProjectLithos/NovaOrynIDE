# NovaOryn IDE 0.1.42

NovaOryn IDE 0.1.42 adds mixed NativeAOT C#/x64 disassembly, configurable CPU exception and fatal/panic breakpoints, and the NovaOryn cat logo in the top-left title/menu area.

## Debugging

- Mixed C# / x64 disassembly is populated whenever the kernel pauses.
- Runtime addresses are translated through the existing EFI relocation delta.
- Source annotations come from `NovaOryn.DebugSymbols.json`.
- Exception breakpoints support vectors 0, 2, 6, 8, 12, 13, 14 and 18 by default.
- The fatal/panic breakpoint stops at `NovaOrynX64StopProcessor` before the terminal halt loop.
- Exception selections persist in IDE local storage and are passed into the debugger before KMain is released.

## Branding

- The existing transparent NovaOryn cat asset is displayed at the far left of the custom Electron title/menu row.
- The standard Theia `ApplicationShell` remains in use; branding is added without reintroducing the prior shell dependency cycle.
