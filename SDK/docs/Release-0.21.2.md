# NovaOryn 0.21.2

NovaOryn 0.21.2 is a build-script correction for the 0.21.1 opaque APIC/interrupt-broker release.

- Builds `NovaOryn.InterruptBroker.Tests` explicitly with `Platform="Any CPU"`, matching the output path used by the runner.
- Verifies that the expected interrupt-broker test DLL exists before attempting to execute it.
- Does not change interrupt routing policy, APIC behaviour, public driver contracts, or the opaque interrupt-broker API.
