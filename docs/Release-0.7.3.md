# NovaOryn IDE 0.7.3

NovaOryn IDE 0.7.3 expands the freestanding CoreLib used by kernel and generated OS bootstrap code.

## Freestanding CoreLib

- Adds `String.Empty`.
- Adds allocation-free `String.IsNullOrEmpty` and `String.IsNullOrWhiteSpace`.
- Adds ordinal string equality operators and `String.CompareOrdinal`.
- Adds `IndexOf(Char)`, `Contains(Char)`, `StartsWith(String)`, and `EndsWith(String)`.
- Adds bootstrap-safe `Char.IsWhiteSpace`.
- Adds `Object.ReferenceEquals`.
- Keeps the no-GC bootstrap honest: arbitrary allocating `String.Concat` is not claimed until NovaOryn has a managed-object allocation/GC contract.
- Structured panic logging no longer requires compiler-generated string concatenation.
