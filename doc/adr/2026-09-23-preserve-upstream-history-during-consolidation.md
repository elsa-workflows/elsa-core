# Preserve original upstream histories during repository consolidation

- Status: Proposed; executable rehearsal only, not import or publisher cutover approval
- Program: [#8194](https://github.com/elsa-workflows/elsa-core/issues/8194)
- Feature: [#8213](https://github.com/elsa-workflows/elsa-core/issues/8213)
- Evidence: [pinned inventory](../integration-program/inventory/README.md)

## Context

Core, Extensions and Studio must become one source workspace while retaining public package IDs, activity identities and independent release units. A squash copy would preserve current contents but lose ancestor history. Rewriting every upstream commit to new paths would improve path-oriented history but change commit IDs, breaking the direct relationship to published package repository commits and existing PR evidence.

The inventory recommends filtered histories as one approach. The alternative below retains original commit identities and tests every relocated blob. This decision is independent of release-unit versioning and does not switch package publishers.

## Decision

Retain each complete upstream history as an original merge parent. Construct the import tree from the current Core tree and an explicit, validated path map of every tracked upstream file. Record original and resulting commit IDs plus every source/destination path, blob ID and mode. Use a normal merge preserving the import commit's ancestry when the real import is ready; **never squash or rebase that history-bearing import**. Ordinary subsequent implementation PRs may still use squash merges.

The rehearsal script creates only a new disposable repository. It refuses shallow source histories, refuses an existing output directory, rejects exact and file/directory destination collisions, compares the complete resulting tree with the intended tree, verifies all three original tips are ancestors, and runs `git fsck --full`. It neither checks out the imported tree nor changes source repository refs. Its synthetic merge commit is not a candidate production branch.

The initial map retains Core paths and uses:

| Source | Destination |
|---|---|
| Extensions `src/modules/` | `src/extensions/` |
| Extensions `src/workbench/` | `samples/extensions/workbench/` |
| Extensions `test/`, `doc/` | `test/extensions/`, `doc/extensions/` |
| Studio `src/` | `src/studio/` (including framework, modules, hosts, bundles and wrappers) |
| Studio `tests/`, `samples/` | `test/studio/`, `samples/studio/` |
| Studio `doc/`, `docs/`, `specs/` | `doc/studio/`, `doc/studio/docs/`, `specs/studio/` |
| Studio artwork and Postman assets | Named subdirectories of `doc/studio/` |
| Remaining root/tool/build/workflow assets | `doc/integration-program/legacy/<repository>/<original-path>.source` |

The `.source` suffix makes retained build projects and workflows inert. They remain byte-identical evidence; their functionality must be deliberately integrated or explicitly retired before #8214 can finish. Imported licensing and contribution information also remains in the receipt and must be surfaced in consolidated notices and contributor documentation before cutover. Merely retaining files is not operational migration completion.

For the five known package-ID collisions, retain the Extensions copies under the inert legacy path. The rehearsal leaves Core's Secrets EF family and Studio's Secrets UI as candidate canonical sources because the source audit shows these align with the current platform implementation. This is **not a verified compatibility decision**: the implementations have materially different types and contracts. Before real import, compare published APIs, stored schema/migrations, Studio HTTP contracts and runtime behavior; port any required capability or provide an explicit supported migration. Never silently replace a public package because its ID matches.

## Release and source boundaries

Importing source must not activate an upstream publishing workflow. Existing Core publishing remains untouched by the rehearsal. A real import requires a reviewed, non-publishing validation path and a separate explicit publication gate. Until approved cutover, the existing source repositories remain package publishers; imported projects are excluded from automated packing. Define one eventual publisher per package ID, including npm units. Source co-location does not authorize republishing all packages or matching every version number.

The packaging proof in #8259–#8260 remains separate. Backend/Blazor co-debugging in #8215 must resolve imported project references, central package/SDK settings and host composition. A successful tree/history rehearsal proves neither compilation nor serialization compatibility.

## Required implementation ledger

Before merging real history-bearing imports, #8214 must demonstrate:

1. Refresh source tips and active PRs; reconcile new commits without rewriting or silently abandoning upstream work. Save final pinned inputs and a fresh receipt.
2. Resolve all five package collisions with public API, persistence and UI compatibility evidence; name the sole publisher and cutover ordering.
3. Integrate central versions, props/targets, Fody/icon paths, generator inputs and local project references; classify every retained build/document/sample asset as integrated or explicitly retired with rationale.
4. Build the consolidated solution and representative supported target frameworks; export and compare runtime activity descriptors against the old hosts, including type/version identities and serialized workflow round trips.
5. Verify independently packed artifacts, clean consumers, and a real backend/Blazor debug session. Test shared-dependency impact without republishing unrelated units.
6. Preserve original ancestors on the final merge. Verify source commit reachability and receipt after GitHub integration; do not infer preservation merely from a local branch.
7. Prepare explicit publisher cutover, upgrade and rollback artifacts for approval. Do not archive source repositories or modify production in the rehearsal.

## Alternatives and consequences

- **Squash copy:** simple review, but fails original-history ancestry. Rejected for the import itself.
- **Filtered rewritten histories:** path history is easier to follow, but every rewritten commit requires mapping back to its source identity. Useful if repository policy forbids unrelated-history merges; do not silently fall back.
- **Original histories plus relocation receipt:** preserves original SHA references and all parent history with a small, verifiable mechanism. Selected for rehearsal. New path logs may require consulting the receipt and original source path rather than relying on rename detection. Full upstream object history increases clone size; the rehearsal measures this before final adoption.

No package IDs, activity identities, credentials or production state are changed by this ADR or rehearsal.
