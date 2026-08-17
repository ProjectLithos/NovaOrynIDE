# NovaOryn 0.36.3

## VirtIO GPU / BootStartup contract correction

- Treats VirtIO GPU as a driver-owned graphics device rather than a GUI dependency.
- References `NovaOryn.Kernel.Virtio.Gpu` whenever the Drivers kernel area is enabled.
- Initializes VirtIO GPU from `HardwareAbstractionLayer.Initialize()` under the Drivers contract.
- Reports VirtIO GPU controller/display counts and the total registered graphics-display count during HAL startup.
- Removes optional driver, storage, networking, USB, input, polling, serial, and command-line namespace dependencies from `BootStartup.cs`; core boot now depends only on assemblies that are unconditionally referenced by the generated kernel project.
- Applies the same changes to both SDK template authorities: `templates/NovaOrynKernel` and the Visual Studio project-template payload.

This fixes the default GUI-off / Drivers-on configuration without hiding the problem in NovaOryn IDE.
