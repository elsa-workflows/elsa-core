# Selected maintenance artifact controls

Task [#8693](https://github.com/elsa-workflows/elsa-core/issues/8693) consumes the exact bytes of an eligible [product release plan](product-release-plans.md). This is a nonpublishing adapter, with no independent product, line, source or version selector. Bounded producer and consumer adapters cover all six Core/Studio/Extensions × 3.8/3.9 source interfaces. Interface availability is source-only evidence: successful actual Studio 3.8 then generator-bearing Extensions 3.8 vertical controls remain required before the full six-cell proof. Actual live compatibility and hosted archive sealing remain pending.

Generate a fresh plan at the reviewed artifact controller, inspect its complete eligible receipt and record its SHA-256. A plan older than one hour, unavailable prerequisites/history, malformed selection or the wrong reviewed hash fails before product work. The planner and artifact controller identities remain separate. Each local invocation generates one UUID and UTC start time in an explicit `local-control` execution envelope shared with the npm receipt. No GitHub run ID or attempt is claimed. The former numeric run CLI flags remain unsupported. The bounded hosted identity seam below accepts only its exact trusted future workflow context; provider-verified hosted proof remains pending. Admission rebinds the registered source and original ownership policy; setup compares original solution/workflow/npm intents and refreshes prerequisite/history eligibility using GET-only requests and the original feed mapping. A changed prerequisite metadata hash requires a new reviewed plan.

```sh
python3 scripts/integration-program/prove_product_release_artifacts.py \
  --plan /absolute/reviewed/plan.json --plan-sha256 EXACT_REVIEWED_SHA256 \
  --output /absolute/new/control
```

`--setup-only` stops after source setup, cheap tool/recipe preflight and fresh prerequisite checks, before product commands. It returns `setup_complete: true` and `artifact_proof: false`; it never reports a successful artifact control. Tool preflight requires installed and selected SDK 10.0.300. Studio additionally requires Node 22, npm 9 or newer and an original host recipe framework supported by read-only MSBuild evaluation. Extensions and Core do not evaluate a Studio host or require Node/npm. It performs no restore or product compilation. On this workstation, prefix the command with `PATH=/opt/homebrew/opt/node@22/bin:$PATH` because the default Node is newer. The same command without this flag runs the selected original product build/test/pack recipe privately, verifies its original SDK metadata, symbols, source documents and satellites, and retains only the selected package archives. Every retained NuGet dependency group must equal the reviewed plan's group. Full original recipes can produce excluded packages privately; the receipt accounts for those archives explicitly. Publication selection does not determine the source test closure.

The Studio-only npm control publishes the historical CustomElements host privately with its original net10.0 recipe, creates the original WASM package, and installs that exact local same-run archive into the original wrapper workspace. It preserves the inline copy lifecycle, Vite build and dist-only package selection. Execution staging changes only package versions and the local producer dependency locator; source corrections must already be admitted registered continuations. Complete tarball inventories join the original WASM bytes to the wrapper's dist assets. A downstream consumer uses exact local archive locators, denies scoped Elsa registry resolution, installs from a fresh npm cache with normal lifecycle scripts, then checks installed bytes, ESM/CommonJS imports and Vite compilation. The installed React inventory must equal its exact tarball inventory plus only `public/_content/**`, `public/_framework/**` and `public/appsettings.json`, each joined by size and SHA-256 to the same-run WASM archive. Missing, extra, changed or symlinked files fail; this is not a blanket public-directory exception. It retains original third-party lock selections. Historical scripts do not acquire the current-main helper or ownership ledger.

The first actual Studio 3.8 control at source `41ed15db7bfce7af5be2d362001518e8e692c22e` failed during normal clean-consumer postinstall: the dist-only tarball had no `public` directory, and the original `cp ... public/` command failed. Its workspace-relative dependency path also points at the wrong directory in a scoped installed package. The failed producer and npm receipts remain immutable failures. Studio 3.9 has the identical source script; it has source evidence, not an executed failure.

Two exact registered source continuations correct only `scripts.copy:elsa-studio-wasm`: Studio 3.8 `da2dec10ba36c65e376138ee45e1c34525e45e49` descends from that failed source, and Studio 3.9 `98a3f23d9c3c67080f3926eeb584036c49855b72` descends from `e7cf096bc117d970dc0bbc11e26dabd2ea2e1b00`. Both change package.json blob `47510f2cb2a18596dcf6e383a2a95ccba192c692` to `8ee363cd0af1242a2152bf8399b3bd4c2a20e973`. The inline Node command resolves the nearest installed WASM package, creates `public`, and copies the three families into explicit destinations. Original postinstall, package files/exports/dependencies, locks, Vite and inert workflows remain unchanged. The protected-path exception binds exact product, line, commit, tree, parent, path, modes, blobs and script-only bytes. All previous candidate records remain; only the two Studio maintenance tips in the explicit eight-cell rehearsal selection advance. These eight historical rehearsal cells remain distinct from the pending selected-product six-cell matrix.

Offline Node 22 fixtures exercise hoisted workspace, scoped and nested installs, paths with spaces, absent public, idempotence and missing dependency/payload failures. They certify the bounded copy interface only. The npm receipt distinguishes `inline_postinstall_preserved` from `original_lifecycle_preserved=false` and records the exact correction when selected. No current helper or ownership ledger is transplanted. New controller and source identities require fresh reviewed plans and original planning-assets snapshots, plus new actual product/archive/consumer controls; old archives cannot certify new SourceLink/source identities. Neither corrected line has completed that control yet.

The Extensions 3.8 adapter is pinned to registered source `92c27a3dd2c7f1dc107cba34749dd293a5d77743`. It runs the unchanged whole-solution recipe and tests, including private excluded recipe outputs, then retains only selected archives. Genuine generated manifest/build assets are joined to their concrete SDK nuspec mappings and emitted byte hashes. The existing metadata verifier requires the source-pinned `Elsa.IO.Http` manifest: schema 1.0, Server runtime, `HttpIOShellFeature`, display “HTTP I/O”, its original description and qualified `Elsa.IO.Http.I/O` feature dependency. The qualification comes from the original preview.50 generator's FeatureDiscoveryService, which prefixes CShells dependency names with the package ID. Actual generator/archive integrity remains subject to the original compiler/archive inspector joins; a cached README is not producer proof.

The requested NuGet version and original DLL policy remain distinct: Extensions assemblies must retain `1.0.0+<full source SHA>`. The manifest still undergoes the existing requested package-version check. Original NUKE CompileSettings does not pass the pack Version, and the generator can reuse an earlier manifest during no-build pack; any resulting actual manifest-version mismatch must remain a failure for source/output-policy review, never a rewritten manifest or silent exception. No Extensions recipe or consumer has been executed for this adapter candidate.

A genuine original lifecycle/package failure stays a failure. Source corrections require a separately reviewed registered maintenance continuation. The runner does not transplant helpers, disable lifecycle scripts, substitute registry Elsa packages, execute historical workflows or claim browser/deployed certification. Existing current/main npm proof is historical tooling evidence only.

The output's `private/` directory contains source checkouts, original recipe outputs, caches and raw diagnostic logs. Share only allowlisted receipts and reviewed archives from `retained/`; producer receipts/raw logs remain private. Failed stage receipts do not disclose arbitrary exception or command output. Preserve failures when iterating with a new output directory.

Cheap contracts run without compiling products. Select Node 22 on PATH for the tiny offline copy fixtures:

```sh
PYTHONDONTWRITEBYTECODE=1 PYTHONPATH=scripts/integration-program \
  python3 -m unittest test_prove_product_release_artifacts test_selected_extensions_contract test_historical_studio_npm_continuation
PYTHONDONTWRITEBYTECODE=1 PYTHONPATH=scripts/integration-program \
  python3 -O -m unittest test_prove_product_release_artifacts test_selected_extensions_contract test_historical_studio_npm_continuation
```

The planner PR path filter covers its consumed runtime/tests/native helpers, release archive verifier and all governed integration-program documents, including containment/register/ownership inputs. Publisher/recovery must later consume these same verified original bytes without rebuilding. This control grants no registry upload, credentials, OIDC, protected approval, permanent ref/tag/version allocation, deployment, authority retirement or archival permission.

## Original Core producer boundary

Core admission accepts only `observed-core-release-branch` at the exact 3.8/3.9 commit and tree in `scripts/integration-program/selected_core_contract.json`, joined to the eligible plan. It creates no maintenance register row and preserves the original solution, imports, NUKE components, source generators and 3.9 ELSA0001/ELSA0002 guards. The existing artifact command is the sole interface; no independent Core version/source selector is added. This is an offline-reviewed interface, not a live Core proof or permission to start the six-cell matrix.

The original `Compile+Pack --version VERSION --analyseCode true` recipe runs with exported `VERSION` from the plan. Local execution explicitly supplies `--configuration Release`, preserving the original server configuration without pretending to be GitHub/CI. The CLI parameter alone only logs the version; original NUKE child processes inherit `VERSION`, which MSBuild reads case-insensitively as `Version`. Core metadata evaluation preserves this environment and does not inject a global `/p:Version` override. Every genuine selected nuspec must still contain the exact requested NuGet version. `Elsa.SamplePackage` explicitly sets `Version=1.0.1`; that precedence risk remains a real future failure, with no preemptive source correction or identity waiver.

Both original test lanes build and test every source-pinned unit/integration/component project in Release/net10.0 (44 projects in 3.8; 61 in 3.9). The component lane retains coverage and the original two-minute hang diagnostics. Local TRX naming/output adds retained evidence without a provider identity claim. Both original solutions omit `Elsa.Mediator.UnitTests`; the workflow includes it through directory discovery, so Core evaluates it for test proof without adding it to the package recipe. The performance project remains excluded by its original `IsTestProject=false`; ordinary concurrent component tests remain included. This original net10 test lane is separate from future all-supported-TFM package consumer coverage.

Only Core admits the three literal 3.8 skips or two literal 3.9 skips, with exact source-bound class, method, display identity and reason. The 3.9 SQL Server/PostgreSQL/Oracle conformance gates preserve configured `ELSA_USERTASKS_TEST_*` values privately. An unavailable original gate admits exactly 47 facts and one unexpanded skipped theory per provider; configured providers must run. Empty-but-present configuration still reaches the original always-run configuration test, which may fail. Unlisted skips, missing expected skips, changed reasons, duplicate executions, failures and service errors fail closed. Passed cases and expected skips are reported separately; no skipped case counts as passed. The strict Studio/Extensions policies remain unchanged. Actual xUnit/TRX reason serialization has not yet been observed: this adapter accepts an exact reason in the TRX ErrorInfo Message or StdOut field and rejects an unsupported representation pending diagnosis.

The App-backed component lane still requires original Docker SQL Server/RabbitMQ fixtures; Core 3.9 also requires its unconditional PostgreSQL fixture. Optional configured external DB providers and native SQLite remain real prerequisites. The controller starts no services. Missing services remain failures.

Native archive verification reuses evaluated dependency/framework groups, generated/compiler/content/manifest/satellite and SourceLink checks. Core assembly version and informational version must match source-supported values captured by original SDK `GetAssemblyAttributes` evaluation and the exact source SHA, rather than a global Studio/Extensions version policy. Only the exact original `Elsa.SamplePackage` project may omit a symbol archive, matching the plan and evaluated SDK policy; its genuine packaged DLL must byte-match the original compiled DLL and pair with the original private compiled PDB for the same native source/symbol verification. That PDB is not published or invented as a snupkg. All other selected symbol requirements remain strict.

Offline Core contracts (no native helper builds):

```sh
PYTHONDONTWRITEBYTECODE=1 PYTHONPATH=scripts/integration-program python3 -m unittest test_selected_core_producer
PYTHONDONTWRITEBYTECODE=1 PYTHONPATH=scripts/integration-program python3 -O -m unittest test_selected_core_producer
```

Studio 3.9 is bound to registered continuation `98a3f23d9c3c67080f3926eeb584036c49855b72`, tree `de330a0c1284c41a85c5fb0dd7c4af2ab0b1168e`. The exact original solution/workflow, client scripts, compiler/central policy, all sixteen test project files and backend runtime APIs are pinned. The existing recipe retains designer restore, generated-code check and npm tests before its client build, DomInterop client build, and whole-solution build/test/pack. Its original test inventory is sixteen projects/eighteen TFM executions: designer tests target net8.0/net9.0/net10.0; the other fifteen target net10.0. Historical planning expects 51 selected NuGet IDs/153 selected package-TFM cells, but only a fresh plan and genuine producer bytes authorize those identities. The existing historical npm adapter retains the registered inline-copy correction, original workflow, same-run local WASM archive and normal clean lifecycle/import/Vite checks for this line.

Extensions 3.9 is bound to `bd7b846efae7e93676c6c3a594e682a5a958b766`, tree `f86b6eef467b947864f43c2a56f4a65e65e1d22e`. Its original `Compile+Test+Pack --version VERSION --analyseCode true` recipe, build-component/compiler policy, module imports, preview.50 generator reference, manifest hints, HTTP API/marker source files and seventeen original net10.0 test projects are pinned. Full-recipe outputs remain private; the five canonical Core/Studio Secrets copies must be excluded from retained selection. Historical planning expects 76 selected IDs/228 package-TFM cells. Requested NuGet and generated manifest versions remain exact, while original compiled DLL policy remains `1.0.0+sourceSHA`. A genuine original manifest compile-version mismatch remains a failed control; no source fix or waiver is introduced.

The 3.9 adapter contract binds the source/test/TFM/recipe/npm/exclusion policies before tool bootstrap. Its new offline contracts and source pin checks are not native build, archive, generator execution or live consumer evidence. The existing source-bound legitimate skip policy remains unchanged; product test failures and unlisted skips still fail.

## Hosted identity and private snapshot stage

The selected controllers now have a bounded hosted execution identity seam for
`elsa-workflows/elsa-core` (repository ID `151148482`), push events on
`refs/heads/codex/selected-product-hosted-controls-8693`, and the future workflow
`.github/workflows/selected-product-release-control.yml`. The exact controller,
workflow/head SHA, plan hash, source, role, job, run and attempt must agree.
These are runner-environment assertions labeled provider-unverified. The local
UUID/UTC schema and local hosted-environment rejection remain unchanged; neither
local receipts nor arbitrary run numbers become hosted proof. Manual-main hosted
policy is not implemented.

This is source-only stage 1. No hosted workflow, upload seal, provider ZIP retrieval
or independent readback is implemented yet, and this seam supplies no actual
hosted proof. Actual controls remain Studio3.8, then generator-bearing Extensions3.8,
then the full six-cell matrix. Historical producer admission remains bound to its
validated actual start; current complete consumer prerequisite checks still run
before consumer restore/build. Existing full producer SourceLink checks remain.

Preserve original planning assets immediately after a fresh eligible plan:

```sh
PYTHONDONTWRITEBYTECODE=1 python3 -B scripts/integration-program/snapshot_product_planning_assets.py \
  --plan "$PLAN/plan.json" --plan-sha256 "$PLAN_SHA256" \
  --output "$PRIVATE/planning-assets"
```

The snapshot uses the existing `private-original-planning-assets-snapshot` schema,
exact raw asset hashes and original absolute source/cache paths. It validates the
selected project/framework partition before output and rejects missing/ambiguous
assets, symlink paths and overlapping/existing destinations. Keep its raw JSON,
receipt and original planning package cache private and available until the consumer
freezes/verifies its original external archive catalog. Derived `resolved_targets`
are advisory; consumers parse the original hash-bound bytes. Nothing from this
snapshot is a public artifact or consumer execution receipt.

The Extensions Compile PackageVersion continuations are registered and byte-bound as documented in [maintenance build](maintenance-build.md). Their new source/controller identities require fresh eligible plans, immediate original planning-assets snapshots and new actual producer/consumer controls. Neither the prior 4966 archives nor old plan receipts certify these children. This source correction does not change the original DLL version policy (1.0.0 plus the selected source SHA), waive requested NuGet/manifest equality, or rewrite an archive.

For the exact admitted Extensions HTTP source and pinned generator `0.0.1-preview.50`, schema 1.0's feature dependency is precisely `{"featureId":"Elsa.IO.Http.I/O","optional":false,"extensions":{}}`. The generator's NULL-only serializer omits packageId/versionRange but retains false and empty objects. The source-bound producer and pure seal accept only the exact dot-qualified dependency, boolean false and empty dictionary, with no extra keys. Wrong optional values, nonempty extensions, version/package claims and unbound generators/sources fail. Other manifest verifier modes retain their existing schema. These are source/schema contracts; actual corrected generation, pack/archive byte joins and cold runtime proof remain required.
