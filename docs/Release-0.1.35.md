# NovaOryn IDE 0.1.35

NovaOryn OS launchers now bootstrap the bundled SDK toolchain on first use rather than during the IDE build. Generated Build.bat and Run.bat set embedded SDK mode, install/verify the SDK toolchain only when required, write a readiness marker under SDK\.toolchain, and reuse the existing installation on subsequent OS builds/runs.
