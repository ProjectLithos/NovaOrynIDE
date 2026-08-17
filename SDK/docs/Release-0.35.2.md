# NovaOryn 0.35.2

NovaOryn 0.35.2 is a Visual Studio responsiveness and Userland integration release based on 0.35.1.

- Project creation no longer loads/copies every SDK project synchronously on the Visual Studio UI thread.
- Userland projects are queued after project creation and loaded beneath a dedicated `Userland` solution folder.
- Generated projects contain `Userland/Commands`, `Userland/Settings`, `Userland/Fonts`, `Userland/Images`, `Userland/Drivers`, and the aggregate `NovaOryn.Userland` project.
- Boot, HAL, Kernel, and Userland project source is preserved during selected-project refresh.
- Interactive commands now include `clear`/`cls`, `info`/`system`, `uptime`, `memory`, `drivers`, and `devices` in addition to the existing command set.
