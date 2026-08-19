# NovaOryn 0.9.0

NovaOryn 0.9.0 is a corrective runtime-validation release for roadmap item 15, Scheduler and Threads.

The Visual Studio Debug run path successfully built the complete freestanding kernel through Roslyn, direct ILC, LLD and EFI image creation, but the QEMU launcher allowed only 30 seconds for an unoptimised Debug kernel running under TCG and reported a generic timeout that did not reveal how far managed startup progressed.

The default runtime-acceptance window is now 90 seconds. Timeout diagnostics now distinguish an empty serial log, failure to reach `NovaOryn KMain started.`, and reaching KMain without reaching `CPU halted.`. The launcher also copies the latest serial text to the stable artifact location and prints up to the final 4096 characters into the invoking console/Visual Studio Output stream before returning failure.

No scheduler, SMP, memory, timing, context-switch or public SDK behaviour is changed by this corrective release.
