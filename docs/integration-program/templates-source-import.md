# Elsa Templates source import

[Task #8748](https://github.com/elsa-workflows/elsa-core/issues/8748), under [Feature #8744](https://github.com/elsa-workflows/elsa-core/issues/8744) and [source program #8740](https://github.com/elsa-workflows/elsa-core/issues/8740), imports Elsa 3 .NET project/solution templates into `templates/`. The [machine-readable ledger](templates-source-import.json) records every original asset, source ref and retained contribution. This is separate from workflow templates and Elsa 4 starters.

## Original source, refs and contribution

Original main is `891759727db053a37911b616ace4a5599e1005d0`, tree `0de85b0ca906f6be6559998768378f5c9fb03bb6`. All **65 tracked assets** map to `templates/<original-path>` with identical modes and blobs. No original assets are omitted. Import commit `f1aaec368a9ad317093c71c993fb51eca2544e9e` contains that exact subtree; only `templates/` differs from its Core base. Its parents are:

1. Core base `1d5bc1e1deaba8f2d90920109061a3504a945106`.
2. Original Templates main `891759727db053a37911b616ace4a5599e1005d0`.
3. Six additional donor tips recorded in the ledger, retained as non-content ancestry parents.

All **41 original commits** reachable from **17 advertised refs** remain in ancestry, including the open contribution head, historical closed PR heads and GitHub's synthetic PR #9 merge. The synthetic merge contributes ancestry only; it does not replace original main content. Main itself has 29 commits. Original IDs and authors, Sipke Schoorstra and gpanayotov, remain intact. The ledger records main, contribution refs and tags 3.7.0, 3.7.1, 3.8.0 and 3.9.0. Tag 3.8.0 is annotated: the exact raw tag-object bytes are retained as base64 and verified by reconstructing its original Git object hash and target. This preserves tag provenance in fresh branch clones without activating new release refs or publishing tags. Historical 3.8.x coverage does not extend the current 3.9.0 runtime support claim.

[Donor PR #9](https://github.com/elsa-workflows/elsa-templates/pull/9), original commit `5dc2c5cf4eb6ed17b2228912f712993e48adb56b`, remains open. The applicable custom-element registration change is applied after original main in `79469e023b7a26b0ff6ca3a5a64104f654466ea2`; the ledger carries its exact application ID. The application preserves gpanayotov's name, email and author instant, with timezone normalized to UTC; the unmodified original commit retains the original timezone. No stale branch content or version pins replace newer main.

[Donor issue #3](https://github.com/elsa-workflows/elsa-templates/issues/3), reported by Antoine Griffard (`agriffard`), remains an unresolved attributed report of ambiguous host endpoints and a subsequent missing `StringCompression` shell feature. No runtime reproduction or resolution is claimed here. Intake found no matching Core import PR; unrelated Core #8727 is release-skill documentation. Source refs and open contributions must be refreshed again before final acceptance.

## Product/build and release boundaries

The [Templates guide](../../templates/docs/README.md) describes the independent `Elsa.Templates` 3.9.0 pack and unchanged `elsa-server`, `elsa-studio`, `elsa-combined` commands. Current generated projects target net10.0, released Elsa 3.9.0 and CShells 0.0.28. Supported feature-model, hosting, persistence, authentication and Labels options remain as on original main. MySQL and preview-only modules remain outside those options.

The valid product solution is `templates/Elsa.Templates.slnx`, containing only the pack and smoke-test projects. Root `Elsa.Templates.slnf` selects that product solution with project paths relative to `templates/`. Root `Elsa.sln` contains navigation items only. Templates therefore remains outside root NUKE/global package packing and the shared Core release version stream. No source project references or new shared release units are introduced.

All nine original project manifests have an explicit current disposition in `scripts/solution/solution-groups.json`: pack, tests, six embedded starter projects as package data, and one legacy standalone root host. The lowercase donor `elsa-templates.slnx` points at a missing `elsa_templates.csproj`; it remains retained historical input, outside supported build commands. The standalone host and its settings remain intact. The full tracked-project census rejects unknown or undispositioned manifests, build/data overlap and accidental Templates membership in the root solution. No updater selector is activated.

Local `Directory.Build.props` and `Directory.Packages.props` preserve donor configuration; a minimal local `Directory.Build.targets` prevents inherited Core source/proof targets, sets `UseProjectReferences=false` and selects scoped `templates/NuGet.Config`. That config uses official NuGet only, with its existing canonical root source name. Root NuGet configuration remains unchanged. Effective product/filter restore and MSBuild evaluation must prove the boundary; source XML checks alone do not establish NuGet behavior. The source-impact receipt explicitly records Templates as a separate product, claims no Templates smoke coverage and rejects its projects or references entering the root source graph.

Core-built nupkg metadata now uses the Core repository URL together with the Core commit SHA. The fixture verifies both. Original donor repository metadata remains unchanged in the immutable original import and history. Package identity, independent version and `templates/artifacts/package/release/` output remain unchanged.

## Validation and acceptance

The original 20 smoke cases remain: two server feature models, four static persistence providers, one shell persistence case, three Studio hosting choices, three Studio auth/Labels combinations, six combined feature/hosting pairs and one combined persistence/auth/Labels case. The shared fixture packs an actual nupkg, checks provenance, installs each case into its own hive, generates into a real system-temp workspace outside Core, validates generated configuration/composition and builds the generated solution. Child install/generate/build commands also run from that workspace. Async teardown removes each workspace and hive.

The generated combined-host assertions require `RegisterCustomElsaStudioElements` and the existing root-component limit before building. Original main lacks that call, while the separately adopted PR #9 adds it. This is static red evidence and a generated-output regression, not a claim that a native failing test or runtime designer check ran locally.

[Nonpublishing source CI](../../.github/workflows/templates-source-validation.yml) checks exact candidate identity, full retained history/assets, effective scoped MSBuild/restore policy and both product/filter build commands. It executes the real pack/install/generate/build matrix and requires a TRX receipt with all 20 cases executed and passed. It has contents-read permission, pinned actions, no upload to package registries, publisher credentials, trust activation or deployment environment. Test and restore-graph artifacts provide source-gate evidence only. Nested `templates/.github/` CI/publisher jobs remain inert donor metadata; existing Python source guards still validate that retained metadata without running it.

Run targeted local checks from root:

```sh
python3 scripts/integration-program/verify_templates_source_import.py
python3 -m unittest discover -s scripts/integration-program -p 'test_verify_templates_source_import.py' -v
python3 -O -m unittest discover -s scripts/integration-program -p 'test_verify_templates_source_import.py' -v
python3 -m unittest discover -s scripts/integration-program -p 'test_current_import_impact.py' -v
python3 -m unittest discover -s templates/test -p 'validate_packages_workflow.py' -v
python3 -m unittest discover -s scripts/solution -p 'test_*.py'
python3 scripts/solution/solution_groups.py --check
ruby scripts/integration-program/test_dependabot_discovery.rb
```

Native proof commands are in the product guide and workflow. Local .NET/Docker execution was deliberately deferred because the shared host was overloaded; no unrun gate is passing evidence. Source acceptance still requires applicable exact-head hosted CI, fresh author-independent Elsa 3 Code Review `APPROVE/HIGH`, current-head Greptile 5/5, advisory disposition, independent lead tree/parent/mapping readback and final source freshness. Import-time documentation does not assert those pending outcomes.

## Maintenance and deferred operations

The first import commit, source refs/tree, original modes/blobs and 65-file mapping are immutable historical evidence. Future reviewed source changes do not rewrite that ledger or add Core-authored assets as donor content. The verifier checks original source separately from current committed tracked content, rejecting raw-byte drift, unsafe paths and symlink traversal even under `python -O` or ambient `GIT_*` routing. The current build/data inventory and product/filter must be maintained together when project manifests change.

The historical consolidated candidate baseline remains 225 packages / 124 exclusions and its existing recovery/executor validators and artifacts remain unchanged. This separate Templates product is absent from root solution-based packing. Any later inventory/release compatibility decision remains explicit operational work under [#8741](https://github.com/elsa-workflows/elsa-core/issues/8741) and [candidate owner #8220](https://github.com/elsa-workflows/elsa-core/issues/8220).

Publisher/full release verification, package upload, tag/trust ownership, source workflow retirement, donor issue/PR disposition and archival remain [#8741](https://github.com/elsa-workflows/elsa-core/issues/8741) / [#8725](https://github.com/elsa-workflows/elsa-core/issues/8725). No source repository writes, contribution-state changes, publication, deployment or Elsa 4 completion are implied by this import. Product source validation remains useful after source acceptance; program-specific provenance tooling can be retired only through a reviewed replacement that retains the immutable evidence and current inventory guarantees.
