# NovaOryn 0.6.2

NovaOryn 0.6.2 is a corrective release for roadmap item 13, timers and clocks.

The 0.6.1 build correctly forced standalone policy and timer-test projects to the `Any CPU` platform, and the Windows build log confirms those assemblies were emitted under `bin\Any CPU\Release\net10.0`. However, the runner still resolved the old `bin\Release\net10.0` paths. Policy programs appeared to pass only because matching assemblies from the earlier solution build were already present at those old paths; the newly introduced timer test exposed the mismatch because no stale fallback assembly existed there.

This release fixes the resolved executable paths for both standalone policy programs and `NovaOryn.Time.Tests` so the build executes the exact `Any CPU` assemblies it just produced. `NovaOryn.BuildPolicy.Tests` now asserts both the standalone build commands and their corresponding runner paths.

There are no changes to timer or clock APIs, HPET/TSC calibration, Local APIC timer behaviour, ACPI discovery, memory management, kernel address-space design, or heap behaviour.
