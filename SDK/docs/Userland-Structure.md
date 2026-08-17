# Userland project structure

User-facing SDK functionality is organized under `src/Userland`. `NovaOryn.Userland` is the aggregate project.

- `Commands` contains canonical userland command implementations and grammar.
- `Settings` contains user-configurable settings contracts.
- `Fonts` contains font catalogs and font-management surface.
- `Images` contains image-resource contracts.
- `Drivers` contains userland-visible driver contracts; privileged device drivers remain in HAL/kernel assemblies.

The older `NovaOryn.Userland.Font`, `NovaOryn.Userland.Buffering`, and `NovaOryn.Userland.Keyboard` assemblies are retained as compatibility facades that forward to `NovaOryn.Userland.Commands`.
