# NovaOryn SDK 0.41.5

NovaOryn SDK 0.41.5 fixes the interactive console build regression in 0.41.4. `KernelConsole.RunInteractive()` now explicitly uses an unsafe context when invoking the registered `delegate*<Boolean>` input service. SDK source, kernel template, and Visual Studio template copies are kept identical.
