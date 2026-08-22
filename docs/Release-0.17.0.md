# NovaOryn IDE 0.17.0

NovaOryn IDE 0.17.0 adds professional SDK-side memory diagnostics.

The embedded SDK now exposes normalized physical allocator statistics, extended heap diagnostics, read-only x64 page-table inspection, checkpoint-based leak detection, guarded/canary allocations, double-free detection, and allocation tags. The implementation extends the existing physical allocator, page-backed heap, page-table manager and debugger ABI instead of introducing a parallel allocator.

The memory-diagnostics project and modified memory projects are synchronized into both generated NovaOryn kernel templates and the Visual Studio template.
