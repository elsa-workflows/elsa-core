# Give Core, Extensions and Studio separate root directories

- Status: Accepted layout decision; implementation and final-main verification are tracked in [#8667](https://github.com/elsa-workflows/elsa-core/issues/8667).
- Date: 2026-10-08
- Program: [#8194](https://github.com/elsa-workflows/elsa-core/issues/8194)
- Supersedes only the folder-location decision in [Preserve original upstream histories during repository consolidation](2026-09-23-preserve-upstream-history-during-consolidation.md).

## Decision

Use sibling product directories at the repository root:

```text
core/          src/  test/  docs/  samples/  gen/
extensions/    src/  test/  docs/  samples/
studio/        src/  test/  docs/  samples/
docs/          adr/  integration-program/  announcements/
build/  scripts/  .github/  docker/  design/
Elsa.sln  Directory.Build.props  Directory.Packages.props
```

Each product owns its source, tests, documentation and product-specific samples. Preserve useful internal groupings and scoped MSBuild/package policies. Root tooling, solution filters, release orchestration, shared branding, architecture records and cross-product evidence remain shared. The central ADR index remains generated; historical record names do not change.

Ownership follows the existing imported source and maintained implementation. Namespaces, solution roles and cross-product references do not by themselves transfer ownership. For example, Extensions-owned Studio modules and workbench samples stay with Extensions. Core integration tests may consume Extensions hosts while remaining Core-owned fixtures. Preserve the existing dependency graph and enforced Foundation boundary; this move adds no Core dependency on optional Extensions or Studio.

Directory boundaries do not change package IDs, assembly names, activity identities, serialized contracts, supported frameworks or release units. The [first-release lockstep decision](2026-09-28-lockstep-consolidated-release-and-publisher-cutover.md) remains in force. Later independent release capability remains a separate decision. Source maintenance, npm publisher ownership and actual archival require their own verified cutover; no operational change is implied by a file move.

## Migration and evidence

The [ordered relocation map](../../scripts/integration-program/product-layout.json) binds the pre-move commit/tree, exact overrides, product prefixes and explicit shared root entries. Apply exact overrides first, then the first matching prefix. It accounts for every tracked baseline file, including generated-looking assets and historical provenance. The pure relocation commit preserves every original blob and mode. Subsequent commits update only path consumers, documentation and verification needed for this layout.

The baseline includes the accepted shared program branch and current main's release-policy changes. The deferred, unmerged Socket implementation is preserved separately and is not part of the relocation baseline.

Keep old source/history identities and immutable evidence bytes intact. Resolve historical mapped paths to current locations at active tooling boundaries instead of rewriting sealed receipts or reviving retained `.source` workflows. Preserve the baseline and pure relocation commits as ancestors when merging this migration, so the map remains reproducible from a normal checkout.

Run `python3 scripts/integration-program/verify_product_layout.py` to verify baseline coverage, modes, collision freedom, retained move provenance and the absence of retired active roots. This check does not establish build or runtime compatibility. Final acceptance also requires updated solution/project imports, effective dependency and package metadata, hosted builds/tests, current-head package/source/symbol/consumer proof, affected developer/container workflows, review gates and verification after the merge into main.

## Consequences

The repository exposes three clear product entry points while retaining one integrated development and release workspace. Existing relative paths and discovery rules must change together: project references, inherited props/targets, generated assets, CI selectors, Docker contexts, dependency-updater configuration, proof scripts and documentation links. Pre-move evidence remains historical evidence and cannot stand in for verification of the relocated head.
