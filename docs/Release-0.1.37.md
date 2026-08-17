# NovaOryn IDE 0.1.37

NovaOryn IDE 0.1.37 replaces the unreliable INT3 relocation-anchor startup with a deterministic QEMU debug-console rendezvous supplied by bundled NovaOryn SDK 0.37.4. The IDE reads the actual relocated EFI anchor address, pauses only inside the private rendezvous loop, arms exact C# source breakpoints, sets RIP to the generated resume symbol, and releases the kernel. The internal preparation is not shown as a user breakpoint.
