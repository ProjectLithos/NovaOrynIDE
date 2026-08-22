# NovaOryn IDE 0.16.0

NovaOryn IDE 0.16.0 adds the formal kernel synchronization layer requested as roadmap item 16.

The embedded SDK now contains concrete freestanding implementations of spin locks, ownership-checking adaptive mutexes, bounded semaphores, manual/auto-reset events, writer-preferring reader/writer locks, 64-bit atomic operations, reusable generation barriers and a tagged lock-free stack. The x64 backend uses `lock cmpxchg`, `xchg`, `lock xadd`, `lfence`, `mfence` and `pause` through the canonical x64 architecture boundary.

The new `NovaOryn.Kernel.Synchronization` project is part of the SDK solution and is packaged in both generated-kernel template families. The formal subsystem contract and SDK documentation are updated accordingly.
