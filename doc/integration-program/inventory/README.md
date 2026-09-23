# Elsa integration program repository inventory

This is the source and release baseline for #8251 and #8252. It is an audit, not a migration, package publication, or claim that source-level discovery equals a built artifact. The machine-readable companion is [`inventory.json`](inventory.json).

## Snapshot and method

| Repository | Inspected commit | Latest release observed | Projects | In solution | Packable candidates* | Tests |
|---|---|---:|---:|---:|---:|---:|
| [elsa-core](https://github.com/elsa-workflows/elsa-core/tree/610790ec57ae9d5c334181d50c1e65f99613fd86) | `610790ec57ae9d5c334181d50c1e65f99613fd86` | 3.8.4 | 171 | 168 | 105 | 63 |
| [elsa-extensions](https://github.com/elsa-workflows/elsa-extensions/tree/33fa0bfd28c7585240e3d4f665058c067b17e287) | `33fa0bfd28c7585240e3d4f665058c067b17e287` | 3.8.4 | 101 | 101 | 82 | 16 |
| [elsa-studio](https://github.com/elsa-workflows/elsa-studio/tree/9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822) | `9afd3e36fd1bc90dfdf8ea00b40d89e4a50c8822` | 3.8.4 | 71 | 70 | 54 | 15 |

The task did not pin Core, Extensions, or Studio SHAs. These are the fetched `main` tips observed on 2026-09-23. The tag/release is recorded separately; do not assume `main`, release tags, and published nupkgs are the same source. Each project row records project/package identity candidates, classification, disposition, solution membership, target-framework declaration and source, project/package references, and test/sample references where directly declared. Asset groups record file counts and relative paths for tests, samples/workbench hosts, documentation, and build/release inputs. NuGet/npm manifests and source-declared activity identities are separate collections.

\* “Packable candidate” is not a count of published packages. Most `IsPackable` values and package identities are source-derived from project XML, inherited props, and SDK defaults, and have not been fully evaluated across all project configurations. Selected Core/Extensions/Studio examples include evaluated MSBuild properties in the JSON. A project outside its solution may not be part of that repository’s solution-wide pack job. The JSON records source basis and exceptions so the figures are useful for planning, not release authorization.

The inventory was reconciled with `dotnet sln <solution> list`: Core has three projects outside `Elsa.sln` (`Elsa.Testing.Extensions`, `TlsSmoke`, and `Elsa.Mediator.UnitTests`); Studio has `samples/BlazorApp1` outside `Elsa.Studio.sln`; Extensions has none outside `Elsa.Extensions.sln`. Studio’s outside-solution sample evaluates to `IsPackable=false`; the Core `TlsSmoke` project evaluates to `IsPackable=true` but is outside its package solution. These cases are why solution membership and packability are recorded independently. Core’s two evaluated packable projects outside `Elsa.sln` make its 105 candidate count 103 packable projects in the solution (the package workflow packs the solution).

## Existing modules and Studio pairing

The Core snapshot already contains the engine, runtime and hosting foundation, workflow APIs, common libraries, expressions, scheduling, persistence providers, diagnostics, identity/tenancy, Secrets, and other platform projects. The Extensions inventory preserves the complete provider/module tree rather than treating it as a connector-only repository. It includes:

| Extensions classification | Projects |
|---|---:|
| AI agents and model integrations | 14 |
| Secrets and credential platform | 10 |
| Messaging connectors (Slack, MQTT, Service Bus/MassTransit) | 7 |
| Connections and credential infrastructure | 6 |
| Scheduling providers | 6 |
| Logging/observability and database connectors | 5 each |
| Persistence providers | 4 |
| Distributed caching, file/network I/O, and workbench hosts | 3 each |
| Distributed actors, storage connectors, workflow contexts | 2 each |
| Other provider/platform areas | 1 each, classified in the JSON |

Other discovered integration families include email, LDAP, Telnyx, GitHub DevOps, OrchardCore, HTTP/OpenAPI, CSV, SQL, Azure storage/files, and Azure OpenAI/OpenAI. No OneDrive or Moneybird project exists in the inspected Core or Extensions snapshots. The complete project list and per-project dependencies are in `project_inventory`, not a hand-maintained shortlist.

The Extensions Slack project currently declares 36 activity identities, but the six [`Watch*` event classes](https://github.com/elsa-workflows/elsa-extensions/tree/33fa0bfd28c7585240e3d4f665058c067b17e287/src/modules/communication/Elsa.Slack/Activities/Events) (`WatchDirectMessages`, `WatchFiles`, `WatchMultipartyDirectMessages`, `WatchNewEvents`, `WatchPublicChannelMessages`, `WatchUsers`) all throw `NotImplementedException` with an event-subscription/WebSocket message from `ExecuteAsync`. Count these as declared but unimplemented triggers. Its [base activity](https://github.com/elsa-workflows/elsa-extensions/blob/33fa0bfd28c7585240e3d4f665058c067b17e287/src/modules/communication/Elsa.Slack/Activities/SlackActivity.cs) exposes the API token as a raw workflow input and passes it to `SlackClientFactory`; it does not bind to Secrets or a connection record. The dedicated test project is [`test/modules/slack/Elsa.Slack.Tests`](https://github.com/elsa-workflows/elsa-extensions/tree/33fa0bfd28c7585240e3d4f665058c067b17e287/test/modules/slack/Elsa.Slack.Tests).

Studio has 43 Blazor modules, seven framework projects, four hosts, one bundle, one sample host, and 15 test projects. Paired feature work is already split across repositories: Extensions includes `Elsa.Studio.Agents`, `Elsa.Studio.Secrets`, and `Elsa.Studio.WorkflowContexts`; Studio includes modules such as `Elsa.Studio.Alterations`, diagnostics dashboards, `Elsa.Studio.ExternalAuthentication`, `Elsa.Studio.Http.Webhooks`, `Elsa.Studio.Labels`, `Elsa.Studio.Secrets`, `Elsa.Studio.UserTasks`, and the workflow designer. Source-level project references also cross the repository boundaries: the inventory captures 81 Extensions→Core, four Extensions→Studio, and one Studio→Core explicit project edge, plus external CShells references. In particular, Studio Core references Core’s `Elsa.Api.Client`; connector Studio modules in Extensions reference the Studio workflow/shared projects. Resolve all such edges to workspace-local project references as part of migration, then exercise a combined backend/Blazor host and focused solution filters.

Do not infer that every same-named backend/Studio package must have a shared version. Couple a pair only where its API/protocol or package graph requires it; verify the pair with compile-time contracts and a backend+Studio integration check. The connection model and Secrets UI are especially sensitive to package ownership because both exist in more than one source tree.

The static activity scan found 61 Core and 116 Extensions types with `[Activity(...)]` annotations and none in Studio. It stores the CLR type, attribute strings, source path, and line. This is an **attribute scan, not a complete runtime activity descriptor export**: inherited metadata, dynamically registered descriptors, and non-attribute registrations can be missed. Before moving or renaming activities, export descriptors from a representative running host and compare the full activity type/version/identity set as a migration gate.

## Source and publisher ownership

At this snapshot all three repositories publish from separate build definitions and independent source roots:

| Repository | Build and package path | Preview publication | Stable publication | Observed version source |
|---|---|---|---|---|
| Core | NUKE `build/Build.cs`; `.github/workflows/packages.yml` invokes `./build.sh Compile+Pack` for the solution | Feedz on configured push/release events | NuGet.org only when a GitHub Release is published | Workflow `base_version` 3.10.0; shared project defaults in `Directory.Build.props` |
| Extensions | NUKE `build/Build.cs`; package workflow invokes `./build.sh Compile+Test+Pack` for `Elsa.Extensions.sln` | Feedz on push/release | NuGet.org on published release | `base_version` 3.8.0 on inspected main; open [PR #216](https://github.com/elsa-workflows/elsa-extensions/pull/216), last updated 2026-09-22, proposes 3.10 previews with Core/Studio preview dependencies and Feedz-only preview publication |
| Studio | No NUKE build project; workflow directly builds/tests/packs `Elsa.Studio.sln`, restores/generates BPMN client assets, builds DOM interop, and packs two npm wrapper artifacts | Feedz for NuGet and npm on push/release | NuGet.org and npmjs.com on published release | `base_version` 3.10.0, separate from observed latest stable 3.8.4 |

The exact workflow paths, configuration inputs, and manifests are listed in the machine inventory. The package workflows currently package solution outputs, not a selected connector release unit. Thus a branch push can publish many unrelated preview packages. Core’s `packages.yml` includes `codex/*` push branches: avoid pushing audit/implementation branches matching that pattern unless the workflow is deliberately made safe first. Preview-feed publication and stable NuGet publication are different gates; the inspected workflows gate NuGet.org on the published-release event. No package was built or published for this audit.

Studio also has two independently published npm package identities (`@elsa-workflows/elsa-studio-wasm` and `@elsa-workflows/elsa-studio-wasm-react`) plus client build/test manifests; these are separate release assets, not NuGet modules. Preserve separate npm release policy and generated-client checks.

Five cross-repository NuGet package-ID collisions need an explicit source/publisher owner before import or cutover:

| Package ID | Current source paths |
|---|---|
| `Elsa.Secrets.Persistence.EFCore` | Core `src/modules/Elsa.Secrets.Persistence.EFCore`; Extensions `src/modules/secrets/Elsa.Secrets.Persistence.EFCore` |
| `Elsa.Secrets.Persistence.EFCore.PostgreSql` | Core and Extensions provider project with the same package ID |
| `Elsa.Secrets.Persistence.EFCore.SqlServer` | Core and Extensions provider project with the same package ID |
| `Elsa.Secrets.Persistence.EFCore.Sqlite` | Core and Extensions provider project with the same package ID |
| `Elsa.Studio.Secrets` | Extensions `src/modules/secrets/Elsa.Studio.Secrets`; Studio `src/modules/Elsa.Studio.Secrets` |

These are real source ownership collisions, not presumed duplicate artifacts. The Core Secrets persistence implementations use newer feature/shell-feature and migration organization than the Extensions copies; the Studio Secrets implementation also differs across its two trees. The Secrets architecture workstream must choose the implementation lineage and publisher before any migration or package workflow change. Inventory source links for each path are present in the JSON.

Related open work is not silently folded into new tasks: Extensions [#164](https://github.com/elsa-workflows/elsa-extensions/issues/164) proposes output converters and is explicitly blocked on Core [#7770](https://github.com/elsa-workflows/elsa-core/issues/7770), both open as checked 2026-09-23. Current repository PR snapshots showed 54 open Core PRs (26 labelled stale), 17 Extensions PRs, and 14 Studio PRs. Open is not evidence of active progress. Relevant open PRs include Core #8114 (test-stack migration spans all projects), Extensions #216 (version train), #209 (MCP), #199 (Azure DevOps), #175 (NUKE/SDK compatibility), #150 (tenant IDs), #140 (Service Bus options), #132 (Dapper split), and #54 (persistence save handlers). Related Studio issues include #1053 (deprecated login package) and #668 (package/interface compatibility). Recheck current PR heads and issue state immediately before path moves.

Extensions issue [#205](https://github.com/elsa-workflows/elsa-extensions/issues/205) reported stale 3.8.1 Mongo/Dapper artifacts. It is **closed and resolved**, not a present open defect: maintainer QA reports the 3.8.2 fix and a 3.8.4 consumer gate passing with aligned three-parameter contracts and clean `AddElsa` boot. The known-negative 3.8.1 package against Core 3.8.4 still fails as expected. Separately, the public `Elsa.Slack` 3.8.4 nupkg was downloaded from [NuGet’s flat container](https://api.nuget.org/v3-flatcontainer/elsa.slack/3.8.4/elsa.slack.3.8.4.nupkg); its nuspec says repository commit `154ba15fb4da85b4bebecfbe43639579cbda1d0d`, TFMs net8/net9/net10, and dependencies `Elsa` 3.8.4 + `SlackNet` 0.17.7. Its declared repository commit matches the peeled `3.8.4` tag in the fetched Extensions source clone; the downloaded artifact SHA-256 is in `artifact_evidence`. This records package/source provenance, not a clean-consumer compatibility result for current main or a future monorepo package.

## Recommended consolidation and release architecture

Keep Core’s established `src/modules`, `src/extensions`, `src/common`, `src/clients`, `test`, and `build` locations as the anchor. A bounded destination layout that distinguishes ownership without imposing a source-wide redesign is:

```text
src/extensions/<area>/<package>/     # imported provider/connector modules
src/studio/framework/                # Elsa.Studio framework projects
src/studio/modules/                  # Elsa.Studio Blazor modules
src/studio/hosts/                    # Studio hosts and bundles
src/studio/wrappers/                 # npm/WASM and React wrappers
test/extensions/<area>/<package>/    # extension test projects
test/studio/<area>/<package>/        # Studio tests
samples/studio/                      # Studio sample hosts
```

Keep an explicit `samples/`, documentation and build-assets destination for each product, and preserve generated schema/client assets with their generators. This shape keeps Elsa engine source paths intact, prevents a nested `src/` tree under one repository, and exposes backend/Studio pairs for combined work. Before committing path maps, compare exact target-path collisions and update solution entries, central package files, CI include paths, project references, docs, and code owners together.

Preserve upstream git history: stage filtered path maps from both repositories in a disposable integration clone, retain commit metadata and a source-to-destination commit map, and merge the histories rather than squashing imports. Do not rewrite the live repositories or move files during this inventory phase. Keep each original pipeline disabled for a package ID as soon as the monorepo becomes its publisher; do not publish the same ID from two repositories.

For independently releasable NuGet packages, define a checked-in release-unit manifest (candidate: `build/release-units.yaml`) that names package IDs, project paths, owned tests/samples/docs, direct Elsa dependency constraints, target frameworks, compatibility declaration, source version, and sole publisher. CI should select changed release units plus only their declared internal dependency closure, then pack and test those units in isolation; keep engine and platform packages in their existing coordinated groups until an explicit boundary review. Connector releases must not stamp or republish unrelated package IDs. npm artifacts need their own named units. Give a tightly coupled backend/Studio pair one release unit only after the package graph and an API/protocol test demonstrate coupling; same source repository is not sufficient justification.

### Bounded proof for #8259–#8260

Use existing `Elsa.Slack` as the bounded candidate because it has a dedicated source project/test project and a real published artifact with inspectable provenance. Run this as a **local-only proof**, not a public publish:

1. **#8259 design:** specify a release-unit manifest for `Elsa.Slack` and identify exact project, focused solution/filter, test project, target frameworks, external dependencies (`SlackNet`), host compatibility, package owner and publisher. Select the aligned 3.8.4 `Elsa` baseline for the first package-consumer check. Keep source-based integration host proof separate from artifact proof.
2. **#8260 proof:** pack only `Elsa.Slack` from a recorded monorepo commit into a temporary local feed; inspect package ID/version/TFMs/dependencies/repository commit; verify no unrelated `.nupkg` appears; build a clean sample host using only the temporary package with matching Core; exercise representative action behavior and explicitly confirm current trigger classes fail as unimplemented. Do not push to Feedz or NuGet.org. Use the public 3.8.4 artifact as the baseline comparator, not as proof that current-main output works.
3. Gate completion on repeatable CI selection of Slack alone, test results from the focused test project, clean-host restore/build/start behavior, source commit attribution, and a reviewer-readable package manifest diff. Only after the proof should the team decide whether to proceed with the release-unit manifest and publisher cutover.

This proof should resolve the exact unit, not claim all connectors can be released independently. It should run on an audit/test branch that cannot trigger the current all-solution Feedz publish rules. No source migration, release workflow change, or public publication is part of #8251/#8252.

## Verification and limitations

Reconciliation commands: `dotnet sln Elsa.sln list`, `dotnet sln Elsa.Extensions.sln list`, `dotnet sln Elsa.Studio.sln list`; project/package/TFM XML scan at the recorded commits; selected `dotnet msbuild <project> -getProperty:IsPackable -getProperty:TargetFrameworks -getProperty:PackageId`; direct inspection of each `.github/workflows/packages.yml` and repo build props; and `gh` read-only snapshots for PRs/issues and release metadata. Slack nupkg nuspec was inspected from the public NuGet flat-container and hashed locally.

Limitations: projects were not all restored, built, or tested; package IDs, dependencies, and target frameworks can be conditioned or changed by imported MSBuild logic; unexpanded property expressions remain visible in JSON; full runtime activity descriptors were not exported; the npm lockfiles were inventoried but transitive trees/license obligations were not resolved; PR counts and release state are a 2026-09-23 snapshot; and no compatibility claim is made about a locally packed Slack candidate until #8260’s consumer proof runs. The survey does not establish customer demand or support commitments.
