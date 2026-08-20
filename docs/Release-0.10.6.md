# NovaOryn IDE 0.10.6

## Bottom panel

NovaOryn now creates its own guaranteed-visible control strip inside the actual bottom dock. It includes Problems, Output, the NovaOryn Build channel, Clear, maximise/restore, and Close controls. This no longer depends on Lumino rendering its hidden native TabBar.

## Driver binding

The VirtIO GPU was being rejected during capability binding before its Start callback ran. Capability declarations are permission ceilings, but the framework incorrectly required all declared capabilities to be pre-granted.

Automatic capability grants are now best-effort. Concrete requests remain constrained by the driver's declaration and the device resources.

The 0.10.5 PCI Memory Space/Bus Master enablement and device matching/start reconciliation remain active.
