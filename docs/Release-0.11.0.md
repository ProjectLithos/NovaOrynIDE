# NovaOryn IDE 0.11.0

NovaOryn IDE 0.11.0 implements the proper NovaOryn SDK test framework.

The existing kernel panic framework was verified as already complete, including structured reason/code, message telemetry, CPU/thread/process context, call stack, registers, optional crash dump, debugger break, and controlled halt/reboot policy.

0.11.0 adds one executable SDK contract for kernel, unit, integration, boot, driver, stress, fault-injection, and hardware-simulation tests; an allocation-free freestanding kernel test runtime; deterministic fault injection; hardware simulation callbacks; test manifests and schema; a command-line test runner with filtering/timeouts/fail-fast; and machine-readable reports.
