# NovaOryn 0.35.20

NovaOryn 0.35.20 makes keyboard auto-repeat release-aware and software controlled.

Previously the PS/2 keyboard's own typematic engine generated repeated make scan codes while a key was held. Expensive actions such as framebuffer scrolling could make those repeated make codes queue ahead of the key's break code. The visible result was that scrolling could continue after the physical key had already been released.

The PS/2 input contract is now version 3.

`KernelPs2` tracks the pressed state of every logical key. A make code for an already-held key is treated as a hardware typematic duplicate and is discarded. A break code changes the state and is delivered immediately as a real key-up transition. This also prevents toggle keys such as Caps Lock from being toggled repeatedly by the hardware repeat stream.

The bootstrap and generated kernel HAL now own repeat policy for both PS/2 and USB HID keyboards:

- the initial key-down action occurs immediately;
- repeat begins after 300 milliseconds;
- repeat runs every 40 milliseconds (25 Hz);
- the matching key-up cancels repeat immediately;
- physical PS/2/USB input is serviced before software repeat on each 1 ms console-service tick;
- repeat scheduling never catches up missed periods, so slow rendering cannot build a repeat backlog.

Repeat applies to Up/Down scrolling and normal command-line characters/backspace. Control/Alt shortcuts such as font and framebuffer-buffering selection remain one-shot operations.

`KernelPs2.IsKeyPressed(Ps2Key)` is also available to other kernel components that need current key state.
