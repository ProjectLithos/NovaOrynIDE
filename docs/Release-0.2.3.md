# NovaOryn IDE 0.2.3

Corrective release for the OS-specific Static Analyzers introduced in 0.2.2.

## Fixed

- The OS-specific Static Analyzers view now explicitly requests its initial React render when the widget is constructed.
- This prevents the Engineering -> OS-specific Static Analyzers tab from opening as a blank editor pane before any workspace-change event occurs.
- The analyzer contract verifier now checks the initial render request.

## Base

NovaOryn IDE 0.2.2.
