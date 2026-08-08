# Versioning Policy

IdemShield follows Semantic Versioning.

## Preview releases

During `0.x`, minor versions may introduce breaking API or persistence changes. Breaking changes will be documented in `CHANGELOG.md` with migration guidance.

## Stable releases

Version `1.0.0` will mark the first stable public API and persistence contract. After `1.0.0`:

- Patch releases contain compatible fixes.
- Minor releases add backward-compatible functionality.
- Major releases may contain documented breaking changes.

Package versions in the IdemShield family are released together so provider dependencies remain unambiguous.

## Support window

Until a formal long-term-support policy is announced, security and correctness fixes target the latest release line. Consumers should stay current within the documented compatibility constraints.
