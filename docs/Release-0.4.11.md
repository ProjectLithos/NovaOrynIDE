# NovaOryn IDE 0.4.11

NovaOryn IDE 0.4.11 fixes the SDK interactive-console build regression introduced by the 0.4.10 shell input/copy bridge. `KernelConsole.RunInteractive()` now executes in an unsafe context because it invokes the unmanaged function-pointer input service. The fix is mirrored into the SDK kernel template and Visual Studio template so refreshed and newly generated projects compile consistently.
