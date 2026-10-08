# Bounded connector release unit and package proof

- Status: Proposed
- Date: 2026-09-23
- Related: #8259, #8260

## Decision

Use `Elsa.Slack` as the first bounded independent NuGet release unit. The release unit consists of the single `Elsa.Slack` package and its source project, the focused Slack test project, and the package-consumer proof. Do not fold Studio into this unit: no Slack Studio companion package is present in the inventory.

The project identity is `src/modules/communication/Elsa.Slack/Elsa.Slack.csproj` in Extensions. It targets `net8.0`, `net9.0`, and `net10.0`; its direct Elsa dependency is `Elsa` 3.8.4 and its direct connector dependency is `SlackNet` 0.17.7. The focused tests live at `test/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj`; that test project currently targets `net10.0`. Invoke the project directly rather than the all-package `Elsa.Extensions.sln` pack job. No dedicated solution filter is recorded for Slack.

The bounded compatibility statement for the proof is “the `Elsa.Slack` package built from the pinned Extensions 3.8.4 source can be consumed with the public Elsa 3.8.4 package family on each of its declared target frameworks.” This includes package restore/build/start, ActivityDescriptor registration, and an offline `CreateChannel` request/output check against a deterministic fake client. It does not claim Slack API calls work against a real workspace, that all 36 declared activities are behaviorally verified, or that the six `Watch*` triggers are implemented. Those triggers currently throw `NotImplementedException` and remain outside this proof.

## Source and artifact baselines

The source baseline is the Extensions `3.8.4` tag at `154ba15fb4da85b4bebecfbe43639579cbda1d0d` and the Core `3.8.4` tag at `33181ae3048f628f591a0155b5665a8e4d1bcea2`. The official `Elsa.Slack` 3.8.4 package downloaded from NuGet has SHA-256 `6df6fd1c3e7558c7c1124fa232879cfa539196fa35a3edd9055cfcc2e0a178e6`; its nuspec declares the Extensions repository commit above, the three target frameworks, and the two dependencies. This artifact is the released baseline, not a build from current `main`.

The local proof package uses `3.8.5-proof.154ba15`. The prerelease label is deliberately local and immutable to the source commit; it is not a proposed public release version. The proof directly packs only the Slack project, checks the nuspec and source revision, and asserts that the temporary local feed contains exactly one `.nupkg` identity (`Elsa.Slack`). Symbol output may accompany it as `.snupkg`. Consumers use isolated NuGet package caches and the temporary feed. No Feedz/NuGet/npm source is configured as a publication destination.

## Per-package version policy

Give each NuGet package ID its own monotonically increasing SemVer 2 version stream; a selected connector release must not stamp or version unchanged packages. For `Elsa.Slack`, the observed public baseline is 3.8.4. The next compatible patch cut is 3.8.5; an additive backwards-compatible feature advances the minor component to 3.9.0, and an incompatible public or serialized-activity identity change advances the major component to 4.0.0. A stable cut must be greater than the last published stable version for that package ID, and a version already published is never reused.

For future automated previews, derive each prerelease from the next stable base and append monotonically increasing CI run and attempt identifiers plus the source short SHA, for example `3.8.5-preview.1234.1.154ba15`. The CI run number is monotonic per publisher; the attempt disambiguates reruns. Prerelease artifacts are retained as immutable evidence and never replace a stable version. The current `3.8.5-proof.154ba15` is a local-only proof identifier and must not be published. The package declares `Elsa` 3.8.4 as its dependency baseline; this proof validates exactly Core 3.8.4, and later Core compatibility must be demonstrated in a separate consumer matrix rather than inferred from NuGet's dependency range.

## Released compatibility and source-debug graphs

Keep two checks distinct:

1. **Released package graph:** clean consumer projects reference `Elsa.Slack` 3.8.4 from NuGet, then the locally packed proof version from the temporary local feed. Both resolve the corresponding `Elsa` 3.8.4 dependency from NuGet and register `CreateChannel` through an Elsa service provider. Record each descriptor's `TypeName` and `Version` for `net8.0`, `net9.0`, and `net10.0`.
2. **Source-debug graph:** a separate consumer project references the pinned `Elsa.Slack.csproj` with `UseProjectReferences=true`, resolving the pinned Core `Elsa.csproj` by project reference. It records the same descriptor identity on `net10.0`. This checks source integration at the release tag; it cannot substitute for the package consumer checks.

The extension test project is run directly for `net10.0`. A static dependency-closure selector test uses the merged inventory to prove that a change to Core's `src/modules/Elsa/Elsa.csproj` selects the Slack tests among 51 affected test projects, while the explicit Slack release unit selects only `Elsa.Slack` for packing. When the inventory contains duplicate package IDs, the selector includes every candidate source project so impact analysis errs toward extra tests rather than missing a dependency. The selector's unit tests run in the integration-tools workflow, but the graph does not yet drive which project builds or tests CI executes. This proof does not execute the other 50 tests. A full impact-aware CI gate and broader compatibility matrix remain follow-up criteria for #8260.

## Publisher and change selection

Extensions remains the current owner and sole publisher for `Elsa.Slack` until an explicit publisher cutover. During a future repository consolidation, the new release-unit manifest must name this project, package ID, test project, TFMs, internal Elsa dependency, external dependency, and one publisher. Change selection must compute reverse dependency closure for tests while package selection remains bounded by the changed release unit and its required internal package closure. A shared dependency change can expand tests across multiple packages; it must not cause unrelated packages to be repacked by default.

Do not trigger the existing all-solution preview workflow for a selected Slack proof. Do not enable a second publisher for `Elsa.Slack`; disable the old publisher as part of a separately reviewed cutover before any monorepo publication. Stable package version assignment, channel policy, publication credentials, and release automation are out of scope here.

## Backend/Studio pair candidate

Keep Slack standalone. `Elsa.WorkflowContexts` and `Elsa.Studio.WorkflowContexts` are a reasonable separate backend/UI pair to evaluate because their names and inventory placement indicate a clear runtime/Studio feature seam. Their exact project paths are `src/modules/workflows/Elsa.WorkflowContexts/Elsa.WorkflowContexts.csproj` and `src/modules/workflows/Elsa.Studio.WorkflowContexts/Elsa.Studio.WorkflowContexts.csproj` in Extensions. The inventory shows no dedicated test project for either. Do not bundle or co-version them until API/protocol or package-graph evidence establishes coupling and a backend-plus-Studio host check verifies that contract.

## Acceptance and limits

The local proof must restore, build, start its minimal consumers, register the expected Slack runtime ActivityDescriptor, execute the focused existing Slack test project while accurately reporting its current skipped test, run the offline action check, verify nuspec dependencies/TFMs/repository commit, check SourceLink where symbol data permits, and prove the selected package set has no unrelated `.nupkg`. Keep the JSON evidence and command logs outside the repositories.

The inventory closure currently selects 51 test projects for the Core `Elsa` dependency change; only the focused Slack tests and proof consumers are in scope for execution here. This is enough to exercise the dependency-closure selector and a real Core-to-Slack edge, but it does not prove a general CI implementation, closure completeness for conditionally evaluated package references, or readiness for publisher cutover. Keep those criteria open until a manifest-driven CI gate tests the full selected closure without unrelated repacks.
