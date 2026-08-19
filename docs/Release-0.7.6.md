# NovaOryn IDE 0.7.6

Patch release for the structured kernel logging bootstrap path.

- Replaces managed-object/interface based early logging with allocation-free static structured logging.
- Removes early-boot dependencies on NativeAOT `RhpNewFast`, write-barrier and dynamic interface-dispatch helpers.
- Routes generated Boot, HAL and kernel startup diagnostics through the same structured logger.
- Preserves Trace, Debug, Info, Warning, Error and Critical levels with subsystem, CPU, thread, process, timestamp and source context.
- Strengthens the logging verifier to reject managed allocation/interface dispatch in the no-GC bootstrap path.
