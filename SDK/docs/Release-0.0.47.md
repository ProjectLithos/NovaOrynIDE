# NovaOryn 0.0.47

## Public API audit and compatibility baseline

This release begins the public-API completion phase.

- Added a machine-readable public API documentation audit.
- Every documentation build now writes `Artifacts/Documentation/PublicApiAudit.json`.
- The audit records public assembly count, public item count, documented item count and individual missing fields.
- Strict documentation validation uses the same audit findings as the generated report.
- Value-returning public methods are checked for return-value documentation.
- Added an offline documentation guide explaining the API quality contract.
- Expanded the authoritative public API rules.
- Updated SDK, toolchain, template and VSIX version metadata to 0.0.47.

The audit output remains beneath the ignored `Artifacts` directory and does not dirty the Git working tree.
