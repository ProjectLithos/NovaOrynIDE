# NovaOryn IDE 0.9.0

## Versioned NovaOryn Crash Dump format

Crash/debug dumps are now a documented SDK format (`NOCD` 1.0), not an IDE-private snapshot.

The standard v1 format contains:
- CPU state
- registers
- stack
- page tables
- processes
- modules
- heap
- memory ranges
- panic reason
- driver state

Format major/minor and per-section versions are explicit. Unknown future sections and fields are forward-compatible within major version 1. NovaOryn IDE 0.9.0 continues to open pre-0.9.0 `schemaVersion: 1` dumps through a legacy adapter.
