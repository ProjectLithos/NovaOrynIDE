# NovaOryn IDE 0.10.4

## Bottom panel control bar restored

The Chromium paint-containment workaround introduced to prevent stale Output text from painting over the editor was too broad. It clipped the standard Theia/Lumino bottom-panel tab/control strip.

0.10.4 confines `contain: paint` and `overflow: hidden` to the bottom **content** panel only. The outer bottom dock is allowed to display its tab bar normally, and both `lm-TabBar` and legacy `p-TabBar` class names are explicitly restored.

This keeps the stale-output paint isolation while restoring the normal Problems/Output selector and top-right panel controls.
