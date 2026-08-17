# NovaOryn IDE 0.1.44

NovaOryn IDE 0.1.44 implements debugger roadmap items 7 and 8 and changes the Windows/Electron IDE icon to the NovaOryn cat logo.

## 7. CPU, thread and process/inferior contexts

While QEMU is paused, the NovaOryn Debug inspector enumerates GDB execution threads. QEMU system emulation exposes virtual CPUs through these execution contexts. The selected context controls the registers, named locals/arguments, Watch evaluation, Memory expressions, disassembly and call stack shown by the inspector. If QEMU returns multiprocess thread IDs, the GDB process/inferior ID is shown alongside the thread ID.

Selecting another CPU/thread sends the GDB `Hg` thread-selection command and immediately rebuilds the paused debugger state for that execution context. Stop packets also update the selected thread so the inspector follows the CPU that actually stopped.

## 8. PE/COFF x64 call-stack unwinding

The debugger no longer constructs a call stack by scanning arbitrary stack words for values that happen to look like return addresses. It reads the x64 PE/COFF exception directory from `MinimalKernel.efi`, parses the runtime-function table and `UNWIND_INFO`, and applies the x64 unwind operations to reconstruct caller RSP and nonvolatile-register state.

The initial implementation handles the normal NativeAOT/UEFI unwind operations used for pushed nonvolatile registers, small/large stack allocation, frame-register establishment, saved nonvolatile registers, machine frames and chained unwind metadata. Leaf functions are unwound by popping their return address. Frames are marked managed when an exact NativeAOT source mapping exists; otherwise they remain native frames.

## NovaOryn application icon

`applications/electron/resources/novaoryn-ide.ico` and the root `NovaOrynIDE.ico` are generated from the transparent NovaOryn cat logo. The Electron `windowOptions.icon` setting points at the packaged ICO so the NovaOryn logo is used for the IDE window/taskbar icon on Windows.
