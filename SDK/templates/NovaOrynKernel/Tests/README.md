# NovaOryn SDK Tests

NovaOryn 0.11.0 uses one SDK test contract for kernel, unit, integration, boot, driver, stress, fault-injection, and hardware-simulation tests.

Project tests are declared in `NovaOryn.Tests.json` and executed with `SDK\Run-NovaOrynTests.bat` or `SDK\novaoryn.cmd test`. The runner supports kind/tag filtering, per-test timeouts, fail-fast execution, expected exit codes, environment variables, and a machine-readable JSON report.

Freestanding kernel tests use `KernelTestRuntime`; deterministic failure paths use `KernelFaultInjection`; simulated MMIO/PIO/interrupt/time devices use `KernelHardwareSimulation`. These runtime paths do not require GC, reflection, or exception handling.
