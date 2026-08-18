# NovaOryn IDE 0.4.10

NovaOryn IDE 0.4.10 moves interactive PS/2 shell input ownership into the SDK command-line subsystem so refreshed existing projects receive the input bridge without regenerating their HAL. The interactive loop services input directly as well as through the timer dispatcher. Ctrl+A followed by Ctrl+C requests a complete shell transcript copy through the serial control channel; the IDE copies that transcript to the Windows clipboard. Plain Ctrl+C cancels the current command.
