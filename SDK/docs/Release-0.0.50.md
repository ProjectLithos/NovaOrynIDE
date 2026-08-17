# NovaOryn 0.0.50

This release establishes the first public API compatibility baseline.

## Included

- Public architecture and boot-stage metadata contracts.
- Deterministic public API compatibility manifest generation.
- SHA-256 signature fingerprints for every discovered public API item.
- Strict public-documentation validation enabled for the baseline.
- Return-value, dependency, restriction and example requirements documented as release policy.

## Generated outputs

- `Artifacts/Documentation/PublicApiAudit.json`
- `Artifacts/Documentation/NovaOryn.ApiCompatibility.json`
- `Artifacts/Documentation/site/index.html`

The compatibility manifest is the source-compatible baseline for later releases. Deliberate breaking changes must be documented in the release notes and accompanied by a baseline-version change.
