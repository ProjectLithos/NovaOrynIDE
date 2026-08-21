# NovaOryn IDE 0.14.14

This release corrects the Engineering-window release verifier introduced during the hardware-abstraction-boundary work.

Engineering tools continue to open in the main document area when they are first opened. After a user moves a tool to another dock area, NovaOryn does not force it back. The previous verifier incorrectly rejected the valid implementation because it did not allow the `await` used before `shell.addWidget(...)`.

The final verification orchestrator now runs the corrected 0.14.14 verifier before the Theia production build.
