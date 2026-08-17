# NovaOryn IDE 0.2.9

Corrective release for the Syscall Explorer build verifier introduced in 0.2.8.

## Fixed

- `Build-NovaOrynIDE.bat` now invokes `Verify-NovaOrynIDESyscalls.cjs` with the repository-pinned `%NOVAORYN_NODE%` executable, matching the other IDE verifier stages.
- Removed the undefined `%NODE_EXE%` invocation that expanded to an empty quoted command on Windows.
- The Syscall Explorer contract verifier now checks its own build-wrapper integration to prevent this regression.

The Syscall Explorer implementation and its NovaOryn Get/Set/Event, Linux and Windows/NT runtime inspection remain unchanged from 0.2.8.
