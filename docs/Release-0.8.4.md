# NovaOryn IDE 0.8.4

## Kernel console input
The boot/debug shell now calls the idempotent `KernelPs2.Initialize()` directly. This removes the generated-template `IsInitialized()` API mismatch.

## Previously created OS list
Each OS entry now has **Remove from list** and **Delete source code** controls in its top-right corner. Removing from the list preserves all source. Deleting source requires confirmation and removes only the selected OS directory.

## Stable OS instance numbers
Creating the same OS name repeatedly no longer collides. The configured and displayed OS name remains unchanged, while each creation gets a persistent instance number (OS #1, OS #2, OS #3, ...). Physical directories use `<name>`, `<name>-2`, `<name>-3`, etc. Instance numbers are never renumbered after deletion.
