# NovaOryn 0.4.1

Corrective release for the 0.4.0 high-half heap bootstrap and offline SDK documentation navigation.

- Captures the PMM-managed physical ranges immediately after PMM initialization, before inherited page-table pages are excluded from allocation.
- Adds bounded PMM free-extent inspection and exact-range allocation for early bootstrap consumers.
- Calculates bootstrap page-table storage from PMM-free physical pages that are also identity-reachable through the active x64 hierarchy.
- Uses only those reachable pages while constructing the first high-half/direct-map page-table levels.
- Builds the direct physical map across the calculated PMM-managed ranges using 1 GiB, 2 MiB, and 4 KiB leaves as alignment/capability permit.
- Switches subsequently created page-table access to `DirectMapBase + physicalAddress`; inherited tables outside the managed direct map remain safely reachable through their already-validated identity mappings.
- Makes `KernelAddressSpace.Initialize()` establish the direct map before reporting success, so the page-backed heap no longer attempts to create high-half mappings with an inaccessible page-table frame.
- Adds public SDK source browsing to the generated documentation site.
- Generates source pages and API-to-source links entirely within the documentation site using relative URLs.
- Replaces `fetch(search-index.json)` with a generated relative `assets/search-index.js`, allowing search to work when `index.html` is opened directly with `file://`.
- Keeps the 0.4.0 allocator/heap APIs and 16-pixel default framebuffer text size unchanged.
