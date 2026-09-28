# Publisher, CI and scoped build configuration dispositions

Decision record, 2026-09-28, for twelve rows of the [retained-asset ledger](legacy-asset-dispositions.json). Their
evidence was already merged, and they waited on one of two things. Some needed a reviewed record citing that
evidence. Others waited on a publisher and release-unit decision, which the maintainer has now made in the
[lockstep release ADR](../../adr/2026-09-28-lockstep-consolidated-release-and-publisher-cutover.md):

- the first consolidated release packs every packable `Elsa.sln` project at one version, 3.10.0;
- the root `packages.yml` is the sole publisher from 3.10.0;
- the Extensions and Studio repositories publish only 3.8/3.9 patches.

Each row closes as `represented_in_core`, with `expanded` representation: the active Core file now covers the
imported product as well as Core. The pinned `.source` copies stay as provenance.

## Publishers

| Pinned asset | Active path | Why it is represented |
|---|---|---|
| Extensions `.github/workflows/packages.yml` | `.github/workflows/packages.yml` | The root workflow runs `./build.sh Compile+Pack` over `Elsa.sln`, which contains the imported Extensions projects. It publishes only on an approved `workflow_dispatch` (#8497, [publisher gates](package-publisher-gates.md)). The ADR makes it the one publisher for these package IDs from 3.10.0. The pinned workflow stays inert; nothing re-enables a push- or release-triggered publisher. |
| Studio `.github/workflows/packages.yml` | `.github/workflows/packages.yml` | The same, for the imported Studio projects. |
| Extensions `build/Build.CI.GitHubActions.cs` | `build/Build.CI.GitHubActions.cs` | The pinned partial's compile/test job is covered by the root `pr` definition, which invokes `Compile` and `Test` on `Elsa.sln`. Its pack and publish job waited on a publisher decision. The ADR gives that job to the root `packages.yml`, so the legacy packaging job is never activated. |

## Pull request CI and NUKE

| Pinned asset | Active path | Why it is represented |
|---|---|---|
| Extensions `.github/workflows/pr.yml` | `.github/workflows/pr.yml` | Root NUKE `TestProjects` selects every `*Tests` project in `Elsa.sln`, including the 17 Extensions test projects the pinned workflow built. Root `pr` run [36272102515](https://github.com/elsa-workflows/elsa-core/actions/runs/36272102515) (attempt 2, `1976dd2d`) ran `./build.cmd Compile Test` on `Elsa.sln` and passed all of them. |
| Studio `.github/workflows/pr.yml` | `.github/workflows/pr.yml` | The same run passed the 16 Studio test projects the pinned workflow built through `Elsa.Studio.sln`. It passed `Elsa.Studio.Workflows.Designer.Tests` on each of its three target frameworks. |
| Extensions `build/Build.cs` | `build/Build.cs` | Core's NUKE entrypoint builds, tests and packs the imported projects through `Elsa.sln`, so a second entrypoint is not needed. The pinned tagged-version calculation is replaced by the lockstep `--version` the root workflow passes to `Pack`. |

## Scoped build properties and central package versions

| Pinned asset | Active path | Why it is represented |
|---|---|---|
| Extensions `Directory.Build.props` | `src/extensions/Directory.Build.props` | A product-scoped file that keeps the Extensions package metadata, `ElsaVersion`/`ElsaStudioVersion`, NuGet audit and documentation settings, and the Extensions trim-warning suppressions. Nothing from it is applied globally. The Slack pack proof ([run 36285398409](https://github.com/elsa-workflows/elsa-core/actions/runs/36285398409)) packs under it. |
| Studio `Directory.Build.props` | `src/studio/Directory.Build.props` | A product-scoped file that keeps the Studio metadata, the net8.0/net9.0/net10.0 targets, the icon item and the trim-warning allowlist. The Studio Core pack proof ([run 36285398358](https://github.com/elsa-workflows/elsa-core/actions/runs/36285398358)) packs under it and checks the icon bytes. |
| Extensions `Directory.Packages.props` | `src/extensions/Directory.Packages.props` | Extensions central versions stay scoped: `Elsa.*` pins follow `$(ElsaVersion)`, and nothing is merged with Core's or Studio's versions. |
| Studio `Directory.Packages.props` | `src/studio/Directory.Packages.props` | Studio central versions stay scoped, including `BpmnModelVersion` for the Bpmn.Model download and the target-framework-conditioned versions. |

Package-mode restores (`UseProjectReferences=false`) for connectors other than Slack remain part of the per-package
release policy the ADR defers. The lockstep 3.10.0 pack uses project references, so it does not depend on them.

## NuGet sources

| Pinned asset | Active path | Why it is represented |
|---|---|---|
| Extensions `NuGet.Config` | `NuGet.Config` | Core's root config is the monorepo source policy. It sends the wildcard to NuGet.org and maps only the two PackageManifest IDs to Elsa preview Feedz. The pinned wildcard `Elsa.*` → Feedz mapping is not carried over. Imported projects resolve Elsa by project reference. The one package-mode proof, Slack at `ElsaVersion` 3.8.4, restored from NuGet.org alone ([run 36285398409](https://github.com/elsa-workflows/elsa-core/actions/runs/36285398409)). |
| Studio `NuGet.Config` | `NuGet.Config` | The same root policy. The pinned WebhooksCore Feedz source, and its misspelled mapping key, are not carried over. A clean restore with an empty package cache resolved `WebhooksCore` and `WebhooksCore.Shared` 0.0.1 from NuGet.org for every current consumer. |

## Still pending

The two `.github/dependabot.yml` rows stay pending. The root configuration from #8495 represents them, but its
`/src/extensions` and `/src/studio` jobs read the `FEEDZ_API_KEY` Dependabot secret. Dependabot reads its
configuration from the default branch, so those jobs first run after #8409 lands. A maintainer then confirms
that the secret is available to Dependabot and that the first scheduled runs succeed. That check needs organization
admin access and cannot be answered from repository content.
