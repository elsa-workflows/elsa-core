# Disposition ledger for retained source assets

Program #8194, Feature #8214, Story #8286, Task #8287. This proposal records what must happen to the original build, workflow, policy, documentation, and colliding package files that the history rehearsal retains under `doc/integration-program/legacy/**/*.source`.

The [machine-readable ledger](legacy-asset-dispositions.json) has one entry per retained file. It carries the source repository/path, mapped path, exact source commit, Git blob and mode, category, owning workstream, proposed disposition, evidence or gate, and current status. Its pins are the rehearsal inputs—Core `076f022cc174d497af26fc8e26414970e61a79b1`, Extensions `33fa0bfd28c7585240e3d4f665058c067b17e287`, and Studio `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822`—not a claim that these are current upstream tips. Six rows now have structured completion evidence for an active Core representation or explicit retirement; the other 157 remain pending their own gates. A candidate represented in the disposable build patch is still not integrated into the history-bearing source tree.

The [E96 current-tip refresh](current-tip-e96-legacy-assets.md) compares the same 163 assets with the newer history-import receipt and verifies the materialized draft import without changing this frozen ledger. Subsequent reviewed decisions are recorded separately for [Studio Spec Kit assets](studio-spec-asset-representation.md), [Studio-scoped guidance](studio-scoped-policy.md), and the [Studio design baseline and historical release note](studio-document-assets.md). These supplements update those assets' disposition without rewriting the original source evidence.

The current inventory contains **163 assets**: 80 from Extensions and 83 from Studio. The 47 Extensions `src/modules/secrets/**` files are the five colliding package projects. The remaining asset families are Extensions build/NUKE inputs (13), GitHub automation (6), root docs/configuration (14), and Studio agent skills (14), GitHub assets (19), Spec Kit workflows (36), and root docs/configuration (14). The JSON ledger is the authoritative path-by-path classification.

Inspection resolved several routine choices without treating them as completed integration:

- The two upstream `CONTRIBUTING.md` files are byte-identical at the recorded pins, so one consolidated guide can replace both after repository instructions are updated. The MIT notices carry different copyright years, 2025 for Extensions and 2023 for Studio; preserve both notices/attributions in the consolidated notice after review.
- The Extensions and Studio icons are byte-identical to Core's root `icon.png` (SHA-256 `82fd76d734d59efc6132af0b0b999146254fa5a296ea5d64f85597bb1cda524e`). Scoped package metadata can use the shared asset only after the packed packages prove the icon is present.
- `SHELL_FEATURES_MIGRATION.md` is zero bytes and has no content to port. Record its explicit retirement after confirming no tooling or links depend on the path.
- Keep Extensions and Studio package-version baselines scoped. The Studio `BpmnModelVersion` also feeds its `PackageDownload` and generated BPMN types. Do not union central package versions without target-framework and package-consumer evidence.
- The Extensions target sets `UseProjectReferences=false` by default; the disposable build opts into local project references. Preserve that separation so a source build does not silently become the package-consumer or publisher configuration. The `.build/ElsaStudio.ProjectReferences.targets` source conditionally supplies `Elsa.Studio.Workflows` to `Elsa.Studio.Agents`; carry it into an active scoped target only if final evaluated references still require it.
- The Extensions Nuke tool project is intentionally isolated: it disables artifacts output, blocks parent `Directory.Build.targets` imports, and pins its own Nuke tool packages including explicit security overrides. Reconcile those build-tool settings with the root tool project, rather than applying them repository-wide or copying old pins unreviewed.
- Core already ignores common `bin/obj`, `packages`, and `node_modules` outputs; merge only the unique Extensions/Studio ignore patterns with paths updated. Keep Studio's Docker exclusions scoped to its actual image context: its source `.dockerignore` also excludes `LICENSE` and `README`, and the Core root has no `.dockerignore`.
- Studio's pinned NuGet config declares `webhooks-core.feedz.io`, while its package-source mapping names `webhooks-coreo.feedz.io`. Mapped source contains a `WebhooksCore` workbench package reference and use in `Elsa.OrchardCore`; resolve the feed dependency, correct the key if retained, and test a clean restore before incorporating the mapping. Do not copy the config wholesale.
- The approved Studio design system is product guidance worth preserving at a named Studio documentation path. The Extensions status README and Studio performance note contain old repository paths or time-sensitive claims; refresh them before presenting them as current. Keep the dated Studio release note as history, not active release guidance.

## Build and CI acceptance

The main import readiness gap is solution and workflow coverage. The root [PR workflow](../../../.github/workflows/pr.yml) runs `./build.cmd Compile Test`; root NUKE selects test projects from `Elsa.sln`. The combined rehearsal proof builds a generated `Consolidated.sln`. That local result does not show that the imported projects or their tests are present in canonical CI.

The next implementation slice belongs in existing #8214/#8215 work; this proposal creates no issue. It should define the full-build solution (or explicit solution set), add the intended imported projects and tests to root NUKE/PR CI, and retain product-focused developer entrypoints where maintainers still need them. Acceptance requires:

1. A fresh, history-preserving rehearsal at refreshed, reviewed pins passes ancestry and mapping-receipt checks before reviewed transformations are applied.
2. Root PR CI and NUKE build/test the documented imported project set using each project's declared target frameworks. The run records the selected solution(s), project count, and enumerated tests; it does not infer coverage from `Consolidated.sln` alone.
3. Both local project-reference builds and package-reference consumer/pack restores pass. Product-specific package versions and feeds remain scoped and validated.
4. The PR path is non-publishing. The retained Extensions and Studio publisher workflows remain inert until one publisher per package ID, cutover ordering, upgrade, and rollback are separately accepted.
5. Each ledger row is changed to implemented or retired only with a reviewable evidence link/commit and exact-head validation. Remove a `.source` copy only when its disposition is complete; otherwise preserve it with the recorded rationale.

The validator checks the ledger against a frozen projection of the actual rehearsal receipt. The projection records the exact SHA-256 of the full receipt (`5716731dfff80733dfd1e9ca1aaa814c237ceec672e9fa60ab8c9af080611a75`) and retains its 163 inert-asset source/destination/blob/mode rows. The validator pins both the projection hash and source-receipt hash, compares the full mapping, and requires normalized relative Git paths and supported regular-file modes (`100644` or `100755`). Schema 2 permits `represented_in_core` only with a reviewed decision path, PR URL, merge commit, active path, and verified active Git blob/mode. The active check compares the index identity with Git's normalized working-file blob and rejects a real working-file diff, so clean CRLF checkouts retain the committed identity. An `identical` representation must match the frozen source blob; an `expanded` representation pins the reviewed active file and its decision document. `retired_from_active_tree` requires the same review references, an explicit reason, and absence of the original active path. Pending rows cannot carry completion evidence. The offline validator checks local files and evidence syntax; it does not authenticate GitHub merge status or assert final import/release acceptance.

Validate the ledger against that frozen source evidence with:

```sh
python3 scripts/integration-program/validate_legacy_asset_dispositions.py
```

For exact validation against a freshly materialized rehearsal, pass the full live receipt path instead:

```sh
python3 scripts/integration-program/validate_legacy_asset_dispositions.py \
  --receipt /path/to/import-receipt.json
```

This ledger does not authorize source import, package publishing, production cutover, or repository archival. It closes no Secrets compatibility, full-test, package-consumer, final-layout debugging, or publisher-ownership gate.
