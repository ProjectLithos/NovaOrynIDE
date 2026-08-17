# NovaOryn 0.34.0

NovaOryn 0.34.0 adds an interactive keyboard-driven QEMU/framebuffer console on top of 0.33.0.

- Adds `NovaOryn.Kernel.CommandLine`, a freestanding line editor and command dispatcher.
- Connects both PS/2 and USB HID keyboard events to the same command input path.
- Adds a `NovaOryn> ` prompt, Backspace editing, Enter submission, command parsing, `help`, `echo`, and built-in font/buffering/keyboard control commands.
- Stops bare 1/2/3 keys from being consumed as font-size shortcuts. Ctrl+1/2/3 now force buffering and Alt+1/2/3 force font size.
- Extends USB HID boot-keyboard translation with Backspace, Tab, digits with Shift, and common punctuation.
- Adds framebuffer-history-aware Backspace support so editing removes the prior glyph instead of printing a control glyph.
- Mirrors the command-line subsystem into both external-kernel SDK templates and includes it in the Visual Studio project template.
