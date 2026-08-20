# NovaOryn IDE 0.10.10

## Problems and Output are real separate views

The NovaOryn bottom control strip previously tried to switch views by locating and clicking tab DOM text. That could change the visible selector without activating the corresponding Theia widget, leaving Output content visible while Problems appeared selected.

0.10.10 activates the actual Theia bottom view:

- Problems invokes the installed Problems view command when available, then falls back to `ApplicationShell` widget activation.
- Output invokes the installed Output view command when available, then falls back to `ApplicationShell` widget activation.
- The NovaOryn selector styling is updated only after the actual view activation succeeds.

Problems and Output therefore remain distinct widgets, while the custom NovaOryn strip only controls which real widget is active.
