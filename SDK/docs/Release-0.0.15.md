# NovaOryn 0.0.16

## Purpose

Corrects build-tool path resolution when one or more optional candidate paths are empty.

## Changes

- `Find-Executable` now accepts null and empty candidate arrays.
- Blank candidate paths are filtered before filesystem checks.
- Missing-tool diagnostics name the tool and list only usable paths.
- Existing repository-local and recorded tool paths remain supported.

## Build

```bat
Build-NovaOryn.bat
```
