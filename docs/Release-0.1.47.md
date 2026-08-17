# NovaOryn IDE 0.1.47

NovaOryn IDE 0.1.47 adds the first professional operating-system engineering workspace on top of the existing source debugger.

- **OS Dashboard**: opens after an OS workspace is selected and summarizes kernel model, target architecture, scheduler, syscalls, driver/test counts, target details, kernel policy, hardware and diagnostics.
- **Dedicated Kernel Console**: a persistent bottom panel receives the same live build/QEMU/serial stream as the NovaOryn build channel, with filtering, pause/resume, clear and auto-scroll controls.
- **Hardware / Device Tree**: presents configured CPU/platform, timer, bus, storage, networking, input, graphics and audio devices in an OS-specific tree instead of a generic file view.
- **Test Explorer**: discovers individual C# test executables in the OS and the bundled SDK, runs one test program at a time with the pinned NovaOryn .NET SDK, and reports live output and PASS/FAIL status.

All four tools are available from the NovaOryn menu. The Dashboard is the default engineering view after explicitly opening an existing NovaOryn OS.
