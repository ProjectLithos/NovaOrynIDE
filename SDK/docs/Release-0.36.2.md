# NovaOryn 0.36.2

## Configuration semantics correction

- **What do you want to work on?** now means exactly that: checked areas are deliberately missing from the supplied OS and are the areas the end user intends to implement.
- Every unchecked area is supplied by NovaOryn.
- `NovaOryn.Configuration.json` is authoritative during project refresh; `NovaOrynProject.json` can no longer silently revert Microkernel/Hybrid selections to Monolithic.
- Added a configuration-window load guard so control initialization cannot overwrite saved selections.
- Generated props distinguish `NovaOrynWorkAreas` (supplied) from `NovaOrynDevelopmentAreas` (user-selected missing areas).
- The manifest records both `WorkAreas` (resolved supplied areas) and `DevelopmentAreas` (missing areas).

Example: selecting only **GUI** means NovaOryn supplies every other area and GUI is the only deliberately missing development area.
