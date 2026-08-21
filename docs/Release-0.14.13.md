# NovaOryn IDE 0.14.13

## Build verification control-flow repair

NovaOryn IDE 0.14.13 replaces the fragile final batch-file verifier chain with a single Node.js verifier orchestrator. The orchestrator runs the comprehensive kernel preset verifier, SDK test-framework verifier, and current release verifier sequentially with inherited output and explicit exit-code handling.

A successful final verification now returns one result to `Build-NovaOrynIDE.bat`, after which the Theia production build must begin. If a verifier fails, its exact filename and exit code are printed.
