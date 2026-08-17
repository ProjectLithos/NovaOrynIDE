# NovaOryn IDE 0.2.8

## 19. Syscall Explorer

Adds **NovaOryn > Engineering > Syscall Explorer**. The explorer presents the configured NovaOryn syscall model and built-in contracts, and when a Debug kernel is paused reads `KernelSystemCalls` directly to inspect initialization, SMAP, the protected syscall stack, and the five live 64-entry Get/Set/Event/Linux/NT registration tables. Registered handlers include runtime addresses and source mappings where NativeAOT debug symbols resolve them.
