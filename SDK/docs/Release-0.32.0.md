# NovaOryn 0.32.0

NovaOryn 0.32.0 is a framebuffer-console performance and buffering-policy release based on 0.31.1.

- Adds automatic framebuffer buffering; the text-console policy resolves Auto to double buffering.
- Keeps explicit single, double, and triple buffering for diagnostics and testing.
- Batches framebuffer presentation by completed logical write/line/dirty region instead of presenting every character.
- Keeps serial output immediate and independent from framebuffer batching.
- Scrolls live framebuffer output by moving exactly one active text-line-height of pixel rows and clearing only the newly exposed strip.
- Keeps scrollback Up/Down as one viewport redraw followed by one presentation.
- Adds `buffering set auto` (`0` is also accepted by the parser).
