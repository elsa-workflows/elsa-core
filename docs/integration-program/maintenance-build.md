# Preserved maintenance build rehearsal

Task [#8677](https://github.com/elsa-workflows/elsa-core/issues/8677) prepares Core-controlled build/test/NuGet-pack proofs for original Studio and Extensions 3.8.4/3.9.0 trees. It does not activate a writable maintenance home, publish packages or retire source repositories. Individually selectable future Core/Studio/Extensions releases remain a separate deliverable under #8217.

The [source register](maintenance-source-register.json) admits exactly four product/line/original-commit bindings. Earlier advertised 3.8.1/3.8.2 refs remain historical provenance only. Existing Core `release/3.8.4` and `release/3.9.0` refs are untouched. Source trees remain rooted at their original `src/`, solution and build paths; consolidated `core/`, `studio/` and `extensions/` layouts are not backported. Original source histories and source-pinned Elsa/ElsaStudio 3.8.4/3.9.0 dependencies remain intact.

## Run a nonpublishing proof

Use a reviewed, clean Core checkout with all history and SDKs 8/9/10, Node 22 and Python 3.12 on an isolated hosted runner. All four source commits are ancestors of the reviewed Core baseline `8802a883d6`; `fetch-depth: 0` obtains them without source repository fetches or new remote refs. The controller rejects missing objects, changed parent/tree/workflow inventories and unregistered selections. Source checkout uses a local Git fetch without tags or credentials and sets the original public repository URL solely for truthful original-source metadata.

```sh
python scripts/integration-program/prepare_maintenance_build.py \
  --product studio --line 3.8 \
  --commit 9bff3f785fd13bd80a3a7ecf88fec4aec8eef7ae \
  --version 3.8.4-proof.12345.1 \
  --output /absolute/new/directory/maintenance-proof
```

The output must be new and outside the controller checkout. Only `MAJOR.MINOR.PATCH-proof.RUN.ATTEMPT` versions on the selected 3.8 or 3.9 line are allowed. Released versions, tags, arbitrary refs, cross-product commits, 3.10 versions and registry/token/command inputs are not accepted. This rehearsal has no version allocation or publisher capability.

The [workflow](../../.github/workflows/prepare-maintenance-build.yml) runs four independent jobs on the temporary reviewed `codex/maintenance-builds-8677` push branch. After registration on main, manual dispatch accepts one explicit product, line, original commit and proof version; dispatch from other refs is skipped. Registration on main is routine nonpublishing verification, not publisher activation. Each job has `contents: read`, `actions: none`, checkout credentials disabled, no OIDC, environment, registry secret or publisher action. Source subprocesses receive a small environment allowlist excluding GitHub/registry credentials and Actions output/authority handles. Historical workflow YAML and composite publisher actions are never invoked. Only Actions evidence artifacts are uploaded.

Studio 3.8 builds both original ClientLibs, then builds, tests and packs `Elsa.Studio.sln`. Studio 3.9 first restores the Designer project and checks generated BPMN types and ClientLib tests before building those assets. Extensions invokes its original `./build.sh Compile+Test+Pack --version VERSION --analyseCode true`. There is no CustomElements publish or wasm/react npm packing in this task. Dependency builds required by these recipes are allowed; other products are not silently published.

## Evidence and current completion boundary

`receipt.json` is written on success and failure. `success:true` is assigned only after the original build/test/pack commands, positive test execution, complete evaluated package inventory, exact named assembly payload for every source-evaluated TFM and build-output policy, actual archive metadata/dependencies/frameworks, real DLL/PDB association and checksum verification, and SourceLink document checks pass. Each package and symbol archive has its original byte hash and size recorded. `published` and `maintenance_refs_activated` remain false. Evaluated inventory, sanitized structured counts/failure stages, verified package provenance and original archives are retained for 30 days under product/line/source/run/attempt artifact identities. Actions records the uploaded archive digest independently. A green workflow flag alone does not establish these checks; read the receipt and retained files. Raw command logs and TRX stay private on the runner and are not uploaded. Receipts contain closed diagnostic codes, relative source/project locators and counts; no raw exception text, machine identifiers or absolute runner paths are retained.

PDB maps must name the original source repository and exact original commit. Tracked document checksums are compared with immutable original Git blobs, not workspace files regenerated during the build. Unmapped documents require verified embedded content. Unknown generated-source or dependency provenance fails explicitly rather than being silently accepted. Assembly identity must match evaluated AssemblyName. Only explicitly evaluated IncludeBuildOutput=false permits an assembly-free package. Assembly versions and informational versions are recorded as actually built; consolidated 3.10 assembly assertions are not applied. The real accepted `VerifyPackageSymbolPair` helper verifies the Portable PDB identity and normalized checksum and reports the unchanged PDB hash.

The four required hosted proof receipts remain **pending** until actual runs are reviewed:

| Product | Original release | Required evidence |
| --- | --- | --- |
| Studio | 3.8.4 | original-layout build/test/NuGet+symbol pack and verified receipt |
| Studio | 3.9.0 | generated BPMN + ClientLib tests, original-layout build/test/NuGet+symbol pack and verified receipt |
| Extensions | 3.8.4 | original NUKE compile/test/pack and verified receipt |
| Extensions | 3.9.0 | original NUKE compile/test/pack and verified receipt |

Inherited build, restore, test or provenance failures remain blockers. Retain the sanitized failed receipt, inspect private runner logs when authorized, identify the exact failing command/package, and propose a separate corrective slice if release-code changes become necessary. Do not modify dependency pins, weaken checks or rebuild already accepted publishing bytes to hide a failure. A new proof attempt uses a new output directory/version and retains the earlier sanitized failure.

## Locally prepared containment descendants

The [containment ledger](maintenance-containment.json) records deterministic local-only descendant commit/tree identities, their exact original parents, and every workflow move with unchanged blob/mode. Review the four small patches in [maintenance-containment](maintenance-containment). Only `.github/workflows/*` paths are moved to `.github/maintenance-inert-workflows/*.source`; release code and original ancestry are unchanged. No branch/tag refs are created by preparation, and no whole-history bundles are needed. Recreate the objects and patches without moving refs:

```sh
python scripts/integration-program/prepare_maintenance_build.py \
  --prepare-containment --output /absolute/new/directory/containment
```

These commit objects are not assumed to survive Git garbage collection. The exact deterministic inputs and patches recreate them; compare resulting parent/tree/commit/blob identities with the ledger before use. Do not push these descendants or their raw historical parents as maintenance branches during this rehearsal.

A contained new tip does not disable historical workflow dispatch/release/rerun paths or revoke broad Core repository/organization Feedz aliases. Remote maintenance ref creation requires the separate reviewed authority handoff, historical-path review, protection configuration and accounting for credentials already loaded into queued/running jobs. Stop on unknown authority or changed evidence. Local cleanup requires no rollback of remote refs because none were created. Publication recovery and source archival are outside this rehearsal.

Future Core-created maintenance commits require a separately verified admission/provenance adaptation: accurate Core repository metadata, remotely reachable Core SourceLink and dependency compatibility. Original-source proof does not establish that adaptation, a completed publisher handoff, npm ownership implementation, or full 3.10 acceptance.

## Focused contracts

```sh
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s scripts/integration-program -p test_prepare_maintenance_build.py
PYTHONDONTWRITEBYTECODE=1 python3 -O -m unittest discover -s scripts/integration-program -p test_prepare_maintenance_build.py
```

These contracts exercise admission failures, environment authority removal, original tree/parent/workflow binding, deterministic workflow-only containment, recipe order, failure receipts, positive test evidence, package dependency/identity completeness and immutable source checksums. They do not run local SDK/Node/browser/Docker workloads. Four real hosted proofs and independent current-head review, repository CI and Greptile remain required before task acceptance.
