# NovaOryn 0.20.0

This release separates background polling from normal kernel execution.

- Added `NovaOryn.Kernel.Polling` as an explicit optional fallback methodology.
- Added `NovaOryn.Kernel.InterruptDispatch` for managed x64 interrupt dispatch.
- Added `NovaOryn.Kernel.TimerDispatch` for Local-APIC-timer-driven deferred/periodic work.
- Generated kernels do not initialize the polling methodology.
- PS/2 console input is serviced by timer dispatch and the idle loop sleeps with HLT between interrupts.
- VirtIO-net, E1000/E1000e and RTL8168/RTL8111 expose service routines used by timer/interrupt dispatch instead of public Poll/PollAll APIs.
- Fixed the stale framebuffer TemplatePolicy assertion: retained-history scrollback/reflow is now the required behavior.
