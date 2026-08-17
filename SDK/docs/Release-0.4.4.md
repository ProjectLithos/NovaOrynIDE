# NovaOryn 0.4.4

## Purpose

Correct the early x64 ownership boundary used by the calculated direct-map bootstrap and make the generated offline SDK reference enumerate the complete public source surface.

## Changes

- x64 UEFI entry switches to a 64 KiB, page-aligned NovaOryn-image bootstrap stack immediately after `ExitBootServices` and before managed runtime entry.
- Direct-map bootstrap accepts only identity-translated **and writable** physical pages for new page-table storage.
- NovaOryn allocates a private PML4 from that calculated bootstrap set, copies the inherited firmware PML4, switches CR3 to the NovaOryn-owned root, and only then extends the high-half mapping hierarchy.
- Existing page-table pages modified before the direct map exists are checked for identity-writable access before use.
- The PMM-derived direct-map calculation remains dynamic; no fixed page-table pool is introduced.
- Documentation collection now scans every authoritative top-level project under `src` instead of silently omitting unlisted projects that contain public declarations or double-counting VSIX template copies.
- `index.html` contains an exhaustive **All public items** table with relative links to API pages.
- Source browsing includes every source project that actually exposes public declarations, including SDK implementation and tooling projects.
- Offline documentation remains entirely relative-path based and `file://` safe.
- Source-policy tests protect the bootstrap-stack, writable-bootstrap-table, full-public-surface, and relative documentation requirements.

## Runtime acceptance

The x64 boot should progress beyond `Virtual memory manager attached to active x64 page tables.`, report the kernel address-space status, initialize the early allocator and heap, print `CPU halted.`, and remain open in QEMU.
