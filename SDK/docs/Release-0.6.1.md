# NovaOryn 0.6.1

NovaOryn 0.6.1 is a corrective release for roadmap item 13, timers and clocks.

The 0.6.0 timer test project compiled successfully, but `Build-NovaOryn.ps1` rebuilt independent test projects without explicitly selecting the solution's `Any CPU` platform. On the Windows build host those standalone builds emitted into `bin\x64\Release\net10.0`, while the runner intentionally looked in `bin\Release\net10.0`. The timers-and-clocks test therefore stopped after a successful compilation because the expected executable path did not exist.

This release makes the test build platform deterministic. Standalone policy-test builds and the standalone `NovaOryn.Time.Tests` build now pass `--property:Platform="Any CPU"`, matching the main solution build and the paths subsequently executed. `NovaOryn.BuildPolicy.Tests` also asserts both commands retain that platform selection.

There are no changes to timer or clock APIs, HPET/TSC calibration, Local APIC timer behaviour, ACPI discovery, memory management, kernel address-space design, or heap behaviour.
