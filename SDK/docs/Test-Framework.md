# NovaOryn SDK Test Framework

NovaOryn 0.11.0 replaces the previous thin test-type declarations with a single executable SDK test contract.

## Test kinds

The contract has eight stable categories: **kernel**, **unit**, **integration**, **boot**, **driver**, **stress**, **fault-injection**, and **hardware-simulation**. A test manifest can contain any mixture of them and the SDK runner can select by kind or tag.

## Hosted runner

`Run-NovaOrynTests.ps1` reads `NovaOryn.Tests.json`, validates unique IDs and test kinds, executes each declared command, enforces a timeout, compares the exit code, optionally stops on the first failure, and writes `novaoryn-test-report-v1` JSON. `novaoryn test` now invokes this runner rather than merely running the SDK validator.

## Freestanding kernel runtime

`KernelTestRuntime` executes a test through a function-pointer ABI and records result, duration, assertion totals, assertion failures, and injected-fault totals. The runtime is deliberately allocation-free and does not require reflection, exceptions, GC, or interface dispatch.

## Fault injection

`KernelFaultInjection` provides eight deterministic rules for allocation failure, I/O timeout, dropped interrupt, device reset, bad DMA, corrupt packet, page fault, CPU offline, and filesystem failure. Rules can trigger after a selected observation count and repeat a fixed number of times.

## Hardware simulation

`KernelHardwareSimulation` provides deterministic read, write, interrupt, and virtual-time callbacks. Driver and hardware tests can therefore exercise a device model without requiring the corresponding physical device.

## Generated projects

New NovaOryn OS projects include a `NovaOryn.Tests.json` manifest and a `Tests` directory describing the shared framework. Tests are not separate ad-hoc programs: they participate in the same SDK contract and report format.
