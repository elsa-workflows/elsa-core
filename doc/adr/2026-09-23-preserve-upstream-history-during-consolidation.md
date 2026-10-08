# Preserve original upstream histories during repository consolidation

- Status: Accepted; the history-bearing source import merged through [#8409](https://github.com/elsa-workflows/elsa-core/pull/8409) on 2026-09-29. This records the history-preservation decision and does not approve publication or publisher cutover.
- Program: [#8194](https://github.com/elsa-workflows/elsa-core/issues/8194)
- Feature: [#8213](https://github.com/elsa-workflows/elsa-core/issues/8213)
- Evidence: [pinned inventory](../integration-program/inventory/README.md)
- Implementation: [history-bearing import #8409](https://github.com/elsa-workflows/elsa-core/pull/8409), [source/release catch-up #8624](https://github.com/elsa-workflows/elsa-core/pull/8624)

## Context

Core, Extensions and Studio must become one source workspace while retaining public package IDs, activity identities and independent release units. A squash copy would preserve current contents but lose ancestor history. Rewriting every upstream commit to new paths would improve path-oriented history but change commit IDs, breaking the direct relationship to published package repository commits and existing PR evidence.

The inventory recommends filtered histories as one approach. The alternative below retains original commit identities and tests every relocated blob. This decision is independent of release-unit versioning and does not switch package publishers.

## Decision

Retain each complete upstream history as an original merge parent. Construct the import tree from the current Core tree and an explicit, validated path map of every tracked upstream file. Record original and resulting commit IDs plus every source/destination path, blob ID and mode. Use a normal merge preserving the import commit's ancestry when the real import is ready; **never squash or rebase that history-bearing import**. Ordinary subsequent implementation PRs may still use squash merges.

The original rehearsal script creates only a new disposable repository. It refuses shallow source histories, refuses an existing output directory, rejects exact and file/directory destination collisions, compares the complete resulting tree with the intended tree, verifies all three original tips are ancestors, and runs `git fsck --full`. It neither checks out the imported tree nor changes source repository refs. Its synthetic merge commit is historical evidence, not the actual import commit.

## Landed import and current source layout

The real import landed through #8409 as merge commit `8b34ab1e71c22c853e1aa340774231c80e92e677`, preserving the Core, Extensions and Studio history in the merged tree. The later source/release catch-up #8624 merged at `6dbfc58624c28e147a90e2d2b0d1a610817012e2`; both merge commits are ancestors of that clean Core head. The import was a merge, not the rehearsal's synthetic commit. The [r10 mapped source-tip receipt](../integration-program/consolidation/source-tip-refresh-2026-09-28-r10.json) and [retained-asset ledger](../integration-program/consolidation/legacy-asset-dispositions.json) preserve later mapped-path provenance and asset dispositions.

The active source layout at the imported tree is:

| Source | Destination |
|---|---|
| Core source and project paths | Retained in the root `src/`, `test/`, `samples/`, `doc/` and `specs/` layout |
| Extensions `src/modules/` | `src/extensions/` |
| Extensions `src/workbench/` | `samples/extensions/workbench/` |
| Extensions `test/`, `doc/` | `test/extensions/`, `doc/extensions/` |
| Studio `src/` | `src/studio/` (framework, modules, hosts, bundles, testing and wrappers) |
| Studio `tests/`, `samples/` | `test/studio/`, `samples/studio/` |
| Studio `doc/`, `docs/`, `specs/` | `doc/studio/`, `doc/studio/docs/`, `specs/studio/` |
| Retained upstream-only assets | `doc/integration-program/legacy/<repository>/<original-path>.source`; later active-tree representations or retirement are recorded in the [retained-asset ledger](../integration-program/consolidation/legacy-asset-dispositions.json) and its decision supplements |

The `.source` suffix marks retained originals as provenance, not active build or workflow inputs. The ledger and its supplements record which assets have an active Core representation or are retired; the original rehearsal counts and pre-supplement checklist are historical and are not current missing-work totals. Imported license and contribution notices are tracked through their separate dispositions. Merely retaining an original file is not operational migration completion.

The one root `Elsa.sln` now uses solution folders and generated filters curated by [`scripts/solution/solution-groups.json`](../../scripts/solution/solution-groups.json), delivered in [#8547](https://github.com/elsa-workflows/elsa-core/pull/8547). Projects are grouped by role rather than source repository: Foundation, optional Extensions domains, Studio, Apps and Samples. The generator enforces that Foundation source projects reference only Foundation source projects; Liquid remains Foundation because `Elsa.Http` references it, and Alterations remains Foundation because `Elsa.Persistence.EFCore` references it. Foundation is a solution grouping and reference boundary, not a separate repository or a package-release unit. The [grouping guide](../integration-program/consolidation/product-solution-filters.md) documents the generated filters and their project-reference closure.

The five Secrets package-ID collision decisions were resolved separately in #8524/#8525 and the [Secrets package disposition](../integration-program/consolidation/secrets-legacy-package-disposition.md). The Core Secrets EF family and Studio Secrets module are the canonical sources; the Extensions 3.8.x legacy Secrets packages remain for their 3.8/3.9 maintenance line and have per-project `IsPackable=false` for the 3.10 cutover. Duplicate 3.8.1 source copies are retained as inert `.source` provenance, and no legacy ciphertext converter ships: operators re-enter or rotate values under the documented upgrade path. These choices do not prove every package/runtime compatibility gate or perform NuGet deprecation.

## Release and source boundaries

Importing source did not activate an upstream publishing workflow. The accepted [first consolidated release ADR](2026-09-28-lockstep-consolidated-release-and-publisher-cutover.md) sets the first consolidated 3.10 release to lockstep; Extensions and Studio continue publishing their 3.8/3.9 maintenance patches from their source repositories, while Core carries reviewed catch-up into the consolidated tree. The later E3 independent connector release streams remain separate program scope. The npm publisher decision also remains separate. Keep full artifact/runtime compatibility, publication, publisher cutover, deprecation, archival and the two product Dependabot operational checks as separate evidence/gates; consult their [disposition records](../integration-program/consolidation/legacy-dependabot-disposition.md) and live issue state. Source co-location authorizes neither publication nor publisher cutover, deprecation, or archival.

The package/artifact and runtime-compatibility evidence under #8259–#8260 remains separate from this history decision. The paired backend/Blazor source-debugging slice in #8215 is recorded in the [final-layout replay guide](../integration-program/consolidation/workflow-contexts-final-layout-debug.md); that synthetic host result does not establish production security or replace broader package/runtime acceptance. A successful tree/history rehearsal by itself proves neither compilation nor serialization compatibility.

## Original implementation ledger (historical acceptance checklist)

The following checklist recorded the evidence required before the real import. It is preserved as the original plan; current completion and remaining gates are determined by the linked merged evidence and live program issues, not by reading this frozen list as a current status ledger.

Before merging real history-bearing imports, #8214 must demonstrate:

1. Refresh source tips and active PRs; reconcile new commits without rewriting or silently abandoning upstream work. Save final pinned inputs and a fresh receipt.
2. Resolve all five package collisions with public API, persistence and UI compatibility evidence; name the sole publisher and cutover ordering.
3. Integrate central versions, props/targets, Fody/icon paths, generator inputs and local project references; classify every retained build/document/sample asset as integrated or explicitly retired with rationale. Track current evidence and pending decisions in the [legacy asset disposition ledger](../integration-program/consolidation/legacy-asset-dispositions.md).
4. Build the consolidated solution and representative supported target frameworks; export and compare runtime activity descriptors against the old hosts, including type/version identities and serialized workflow round trips.
5. Verify independently packed artifacts, clean consumers, and a real backend/Blazor debug session. Test shared-dependency impact without republishing unrelated units.
6. Preserve original ancestors on the final merge. Verify source commit reachability and receipt after GitHub integration; do not infer preservation merely from a local branch.
7. Prepare explicit publisher cutover, upgrade and rollback artifacts for approval. Do not archive source repositories or modify production in the rehearsal.

## Alternatives and consequences

- **Squash copy:** simple review, but fails original-history ancestry. Rejected for the import itself.
- **Filtered rewritten histories:** path history is easier to follow, but every rewritten commit requires mapping back to its source identity. Useful if repository policy forbids unrelated-history merges; do not silently fall back.
- **Original histories plus relocation receipt:** preserves original SHA references and all parent history with a small, verifiable mechanism. Selected for rehearsal. New path logs may require consulting the receipt and original source path rather than relying on rename detection. Full upstream object history increases clone size; the rehearsal measures this before final adoption.

No package IDs, activity identities, credentials or production state are changed by this ADR or rehearsal.
