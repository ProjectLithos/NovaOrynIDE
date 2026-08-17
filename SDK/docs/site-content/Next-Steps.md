# Next steps

NovaOryn 0.23.0 adds built-in physical Ethernet drivers on top of the PCI and networking layers. VirtIO-net remains the first virtual NIC, while Intel E1000/E1000e and Realtek RTL8168/RTL8111-class controllers now use PMM-backed DMA descriptor rings and register independent adapter-neutral network interfaces. Intel I219/I225 remains a later dedicated family.

## Next — Debugging, testing and diagnostics

The next roadmap stage should add structured kernel diagnostics, assertions, tracing, crash/fault capture, debug transports, test harnesses and runtime inspection facilities.

## Interactive console in 0.23.0

The generated QEMU kernel remains interactive after boot: Up/Down navigate framebuffer scrollback, number keys 1/2/3 choose 8/16/24-pixel fonts, and Ctrl+1/2/3 select single/double/triple framebuffer buffering. See `docs/Framebuffer-Console.md`.
