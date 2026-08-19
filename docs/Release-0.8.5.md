# NovaOryn IDE 0.8.5

## Delete-source list synchronization

Deleting an OS source tree now removes that OS from the visible IDE list immediately after the backend confirms deletion.

The IDE then performs a backend refresh to reconcile the list with disk state. The backend also deletes the source tree before clearing per-instance list metadata, while preserving the next instance counter so OS numbering never goes backwards.
