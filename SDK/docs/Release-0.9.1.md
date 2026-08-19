# NovaOryn 0.9.1

NovaOryn 0.9.1 is a corrective release for roadmap item 16, user/kernel separation.

## .NET compatibility correction

The protection implementation intentionally uses the normal .NET `System.UInt64.MaxValue` member for overflow-safe user-range validation. The freestanding CoreLib supplied in 0.10.1 did not yet expose that standard primitive constant, causing the solution build to fail before NativeAOT compilation.

0.9.1 fixes the compatibility layer rather than replacing normal .NET code with a NovaOryn-specific or magic constant. `NovaOryn.Freestanding.CoreLib` now exposes `MinValue` and `MaxValue` on `SByte`, `Byte`, `Int16`, `UInt16`, `Int32`, `UInt32`, `Int64`, and `UInt64`, and both generated-kernel template copies remain byte-for-byte aligned with the authoritative CoreLib.

The build-policy program also now executes the protection and CoreLib compatibility assertions before its final success result.

No user/kernel-separation API, page-table policy, selector ABI, or native protection mechanism changed in this corrective release.
