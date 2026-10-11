# Elsa Apps source import

[Task #8746](https://github.com/elsa-workflows/elsa-core/issues/8746) imports official Apps into the `apps/` product for the [Elsa 3 source integration program #8740](https://github.com/elsa-workflows/elsa-core/issues/8740). The [machine-readable register](apps-source-import.json) records every original asset, branch tip and retained contribution.

## Original source and history

Apps main is pinned at `f1b0d4b07f04265806b1ec7a6b9df85aafcc85a9`, tree `283451a525a9c0cd4e6279c72e90d9b6c611664d`. All **89 tracked files** map to `apps/<original-path>`, with original Git modes and blobs recorded. No source assets are omitted. The first import commit, `b0fa3f9e59299ea388222b774e6f28cf3e7f5262`, has these parents in order:

1. Core base `c46850de361cb84f267845ba66394c87262e2aab`.
2. Original Apps main `f1b0d4b07f04265806b1ec7a6b9df85aafcc85a9`.
3. Original Apps PR #9 `b69bc853d609c1a1ec94c2d55fb0562ace4850f5`, retained as a **non-content ancestry parent**.

That commit's Apps subtree is byte-for-byte the original main tree. The subsequent integration delta is separate. All **76 original commits** and **ten advertised branch tips** remain reachable, including `release/3.6.0` and `develop/3.6.1`; no source tags were advertised. Original commit IDs and authors remain intact, including Sipke Schoorstra, j03y-nxxbz, Copilot and Dependabot. Recording original refs does not activate replacement maintenance or publication refs.

PR #9 remains an open source proposal, not an accepted merge. Its original change proposed Studio/Extensions 3.8.4 instead of 3.6.3; current Apps main already consumes 3.9.0. Its commit is retained without applying older pins or changing the source PR. [Apps issue #3](https://github.com/elsa-workflows/elsa-apps/issues/3), Jens Willmer's (`jwillmer`) OIDC report, also remains recorded and open. This import does not implement authentication changes or transfer/close source items. Refresh source refs and open contributions again before final source acceptance or any later retirement action.

## Product and build boundaries

The [Apps guide](../../apps/README.md) and [agent instructions](../../apps/AGENTS.md) describe nine net10.0 projects and six existing image profiles. Existing `core/src/apps/` hosts/samples remain separate. App/project/image names, ports, runtime configuration, authentication wiring, backend URL handling, static assets, storage defaults and original Dockerfiles remain unchanged.

Apps retains its own `Directory.Build.props`, `src/Directory.Build.props`, `Directory.Packages.props` and `NuGet.Config`. Core/Studio/Extensions packages remain released 3.9.0 consumers with independently configurable family versions. A local `Directory.Build.targets` prevents inherited consolidated source/proof targets and sets `UseProjectReferences=false`. Only the original internal Apps project references remain. `RestoreConfigFile` explicitly selects the preserved `apps/NuGet.Config` for every Apps project, including root solution/filter restore. This prevents parent source-package mappings from redirecting released Studio dependencies to an incompatible feed; root feeds and mappings remain unchanged.

These image hosts and their private shared libraries set `IsPackable=false`, so adding them to root `Elsa.sln` does not add Core NuGet release units. Existing package IDs, central dependency versions and release-unit inventory are untouched. The root solution includes all nine projects with their original GUIDs; generated `Elsa.Apps.slnf` selects only imported Apps and its internal reference closure. The mapped `apps/ElsaApps.sln` remains usable, with stale links to deleted source workflows replaced by the retained current workflow link. Root NUKE cleanup recognizes the added source location.

Build Dockerfiles from the root with `apps/` as context, for example:

```sh
docker build -f apps/src/Elsa.Server/Dockerfile -t elsa-local-server apps
```

The local-only `verify-local --source-repository elsa-workflows/elsa-core` option verifies honest Core provenance for images built from a Core commit. Its default remains `elsa-workflows/elsa-apps`; publisher label and finalization defaults are unchanged. The focused regression exercises this CLI path while retaining the default contract.

## Nonpublishing validation and acceptance

The five source-package Dependabot batches explicitly account for all nine Apps projects as independent consumers outside their update inventory; no updater is activated. Current imported-source impact selection restores Apps but records them separately from the source-reference graph. It rejects unknown/missing Apps members and project-reference edges crossing the consumer boundary, and retains fail-closed rejection of unmapped Elsa packages for the remaining source graph. The source-impact receipt does not claim Apps runtime/test coverage; the Apps source workflow owns that proof.

The [Apps source workflow](../../.github/workflows/apps-source-validation.yml) checks the exact candidate head, source ancestry/assets, effective MSBuild policy, Python helpers, solution groups and product builds. Its six-profile native Ubuntu AMD64 image matrix uses only local image load, `push: false` and Core-source/package provenance labels. It has contents-read permission, no registry login, secrets, deployment environment or publishing input. Docker build record/summary uploads are disabled; only allowlisted structured smoke JSON is retained, without credentials or raw build logs.

| Profile | Existing port | Smoke coverage |
| --- | --- | --- |
| `server` | 8080 | HTTP, restored packages, synthetic admin login, bearer workflow API and accepting runtime/dashboard metrics |
| `studio-server` | 8080 | HTTP, restored packages and Blazor Server assets |
| `studio-wasm` | 8080 | HTTP, restored packages and WASM JS/CSS assets |
| `studio-wasm-standalone` | 80 | HTTP, restored packages and nginx-served WASM JS/CSS assets |
| `server-studio-server` | 8080 | HTTP, restored packages, Blazor Server assets, login, protected API and runtime/dashboard |
| `server-studio-wasm` | 8080 | HTTP, restored packages, WASM assets, login, protected API and runtime/dashboard |

Run focused checks from root:

```sh
python3 -m unittest discover -s scripts/integration-program -p 'test_verify_apps_source_import.py' -v
dotnet msbuild Elsa.Apps.slnf -t:GenerateRestoreGraphFile -p:RestoreGraphOutputPath=/tmp/apps-root-restore-graph.json
python3 scripts/integration-program/verify_apps_source_import.py --evaluate --restore-graph /tmp/apps-root-restore-graph.json
python3 -m unittest discover -s scripts/solution -p 'test_*.py'
python3 scripts/solution/solution_groups.py --check
(cd apps && python3 -m unittest discover -s scripts/tests -v)
dotnet restore Elsa.Apps.slnf --force-evaluate
dotnet restore apps/ElsaApps.sln
dotnet build apps/ElsaApps.sln -c Release --no-restore
dotnet build Elsa.Apps.slnf -c Release --no-restore
```

The source verifier rejects corrupted mappings and mapped symlinks under both normal Python and `python -O`, using temporary fixtures and the actual retained Git objects. The original helper suite has 50 tests; the added local-provenance regression brings it to 51. Source intake verified successful historical source runs [37747159965](https://github.com/elsa-workflows/elsa-apps/actions/runs/37747159965) and [37747164447](https://github.com/elsa-workflows/elsa-apps/actions/runs/37747164447) at the pinned Apps head. Those runs are prior source evidence, not acceptance of the imported candidate. New exact-head native CI must pass all six profiles before integration. A single representative local image preflight does not replace that matrix.

SQLite/default is the verified smoke configuration. Existing source builds report `NU1608`: Pomelo EF Core 9 requires EF Relational 9 but 10.0.9 resolves. MySQL remains unverified, with no support-policy or dependency change. Existing ApplicationInsights instrumentation-key obsolescence warnings also remain. OIDC, external databases, ARM64 manifests, registry aliases and publication are not certified by this AMD64 source gate; skipped/unexercised cases are not passing evidence.

Final acceptance additionally requires fresh author-independent Elsa 3 Code Review `APPROVE + HIGH @ <full head>`, current-head Greptile 5/5 under repository policy, advisory disposition, applicable CI, independent source-tree/merge-parent readback and a final source freshness check. The lead retains integration and public-write authority.

## Maintaining Apps after the source cutoff

The first import commit, original source refs/tree, asset modes/blobs and original
89-file mapping are immutable historical evidence. Never rewrite them or append new
Core-authored files as though they came from the donor. `adaptedSourceFiles` describes
this import's integration delta only; it is not a permanent exemption list for future
source changes. The verifier checks original blobs against the immutable import commit,
then checks current tracked Apps files against the current committed checkout, rejecting
unsafe paths and symlink traversal. Normal reviewed runtime edits do not alter the ledger.

The current consumer inventory comes from tracked `apps/**/*.csproj` files,
`Elsa.Apps.slnf` and root `Elsa.sln`; all three must agree exactly. When adding or removing
an Apps project, maintain the root solution and regenerate its filter with the existing
solution-group tooling in the same change. Keep references within Apps, scoped package
consumption and nonpackable defaults. Source-impact selection and dependency discovery
use this current inventory, so ordinary project additions need no historical ledger entry.
The effective policy checks compare package-family versions with the current declarations
in `apps/Directory.Packages.props`; reviewed package updates also require the affected
product tests. They do not activate an updater or publisher.

The consolidated candidate comparison baseline retains its original 225 package identities
and 124 exclusions, plus the nine initial Apps nonpackable exclusions. Its original hosted
artifact evidence is unchanged and does not certify the added rows. A future project
addition/removal must reconcile its current nonpackable exclusion through normal inventory
review. The existing package-proof CI supplies evaluated inventory; this source import
neither prepares a release candidate nor changes release authority.

## Deferred operational ownership

Original `.github` publisher and Dependabot metadata is retained byte-for-byte under nested `apps/.github/` and is inert in Core. It is not copied into active repository-root metadata. The original source publisher remains separately governed.

Registry/trust/publisher ownership, image publication, source authority retirement, issue/PR disposition execution and archival remain [operational program #8741](https://github.com/elsa-workflows/elsa-core/issues/8741). No secret, environment, trust, release or registry authority is activated by this source migration. No source repository, contribution state, package, tag or image is changed by importing the code.
