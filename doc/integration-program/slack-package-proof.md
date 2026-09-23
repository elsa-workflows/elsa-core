# Elsa.Slack local package proof

This run is local only. It packs `Elsa.Slack` from the pinned 3.8.4 Extensions source into a temporary feed and never publishes it. Design details are in [ADR: Bounded connector release unit and package proof](../adr/2026-09-23-bounded-connector-release-unit.md).

## Unit and compatibility boundary

- Release unit: `Elsa.Slack` only.
- Package project: `src/modules/communication/Elsa.Slack/Elsa.Slack.csproj` (Extensions).
- Focused tests: `test/modules/slack/Elsa.Slack.Tests/Elsa.Slack.Tests.csproj`, target `net10.0`.
- Package target frameworks: `net8.0`, `net9.0`, `net10.0`.
- Dependencies: `Elsa` 3.8.4 and `SlackNet` 0.17.7.
- Source: Extensions tag commit `154ba15fb4da85b4bebecfbe43639579cbda1d0d`; Core tag commit `33181ae3048f628f591a0155b5665a8e4d1bcea2`.
- Comparison artifact: public `Elsa.Slack` 3.8.4 from NuGet, SHA-256 `6df6fd1c3e7558c7c1124fa232879cfa539196fa35a3edd9055cfcc2e0a178e6`.
- Local proof version: `3.8.5-proof.154ba15`, a local prerelease identity only.
- Local package SHA-256: `ea632344c911f07a5968111df9abd9f9291152ac9cf1f528854d5e374f8bfa8a`.

The package consumers restore and build both the public artifact and local proof version on all three target frameworks. Each starts an Elsa service provider, registers `CreateChannel`, and records `TypeName` and `Version` from `IActivityRegistry`. A separate `net10.0` consumer uses `ProjectReference` with `UseProjectReferences=true` to build the pinned Extensions/Core source-debug graph. Its receipt is compared to the public package's `net10.0` descriptor.

At `net10.0`, a separate offline activity consumer runs `CreateChannel` with a deterministic fake `ISlackApiClient` and `IConversationsApi`, then asserts the request arguments and returned channel output for both public and local package versions. The pinned `SlackClientFactory` owns a private token-keyed cache, so this isolated consumer seeds that cache via reflection after checking the exact field and generic type, and preflights `GetClient(token)` to confirm the returned instance is the fake before workflow execution. Unexpected proxy calls throw. Its process also uses a loopback-only proxy trap. This harness is intentionally brittle to `SlackClientFactory` internals and fails closed if that implementation changes; it adds no production hook or test credential. The temporary consumer references `Elsa.Testing.Shared.Integration` 3.8.4 as test harness support. That additional harness dependency is not part of the `Elsa.Slack` package.

The focused source test project is built and run on `net10.0`; at the pinned tag its only test is skipped with `Not implemented yet`, so it contributes zero executed assertions. The offline action smoke supplies one narrow behavior check without unskipping or altering upstream tests. The inventory-backed selector test verifies that a Core `Elsa.csproj` change reaches the Slack tests and 50 other affected test projects, while explicit Slack release-unit selection yields only the `Elsa.Slack` package ID. The selector's unit tests run in the integration-tools workflow, but the graph does not yet drive the CI test matrix. This local run does not execute the broader 50-project closure.

## Reproduce

Use clean disposable detached worktrees at the source commits above and a new empty output directory outside all repositories. `dotnet restore` and `dotnet build` write ignored `bin/` and `obj/` outputs inside the source worktrees, so do not use shared developer checkouts:

```sh
python3 scripts/integration-program/run_slack_package_proof.py \
  --core-source /tmp/elsa-integration-8251/elsa-core \
  --extensions-source /tmp/elsa-integration-8251/extensions-3.8.4 \
  --output-dir /tmp/elsa-slack-proof-2026-09-23 \
  --sourcelink-tool /tmp/codex-sourcelink-8259/sourcelink
```

The script validates both clean source pins and checks that the Slack project's resolved `ProjectReference` is exactly the supplied Core project. It verifies the explicit SourceLink tool path and package store, then invokes the pinned 3.1.1 `sourcelink.dll` payload directly; it compares the installed DLL with the copy in the 3.1.1 package archive and records its SHA-256. This avoids trusting a replaceable command shim based only on `dotnet tool list` metadata. Before recording success, it checks both source HEADs and tracked/untracked working-tree status again. The focused-test receipt reads every TRX file and fails unless the aggregate still matches the pinned baseline: exactly `CreateChannelTests.ExecuteAsync`, skipped as `Not implemented yet`, zero executed/passed/failed, and no error or other non-zero summary counters. It uses per-check NuGet caches, downloads and hashes the official baseline artifact, runs the selector tests, runs only the Slack test project, packs only the Slack project, and asserts the temporary feed has exactly one `.nupkg`. It writes `evidence.json`, selector output, TRX results, consumer projects, and command logs under the output directory. Core source debugging needs the Core tag's `Elsa.Platform.PackageManifest.Generator` 0.0.1-preview.53 build tool; only that package ID is mapped to the configured Elsa Feedz preview source. All released/local package consumers use NuGet.org and the temporary local feed only.

Install the pinned SourceLink CLI before running the proof, using an isolated NuGet config:

```sh
mkdir -p /tmp/codex-sourcelink-8259
cat >/tmp/codex-sourcelink-8259/NuGet.Config <<'EOF'
<configuration><packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>
EOF
dotnet tool install --tool-path /tmp/codex-sourcelink-8259 sourcelink --version 3.1.1 --configfile /tmp/codex-sourcelink-8259/NuGet.Config
```

The CLI path is required. A missing or unpinned tool, incomplete package store, mismatched payload DLL, or altered install fails closed before package restore or pack, so SourceLink cannot be silently reported as passed when it was not checked. The runner invokes the verified assembly directly rather than executing the shim at that path.

Run the inventory selector and proof guard tests in both normal and optimized Python modes:

```sh
python3 -m unittest discover -s scripts/integration-program -p 'test_*.py'
python3 -O -m unittest discover -s scripts/integration-program -p 'test_*.py'
```

## Results

The run completed successfully from the pinned source trees. The Core source build emitted NU1902 for `Microsoft.Build.Tasks.Git` 8.0.0 (GHSA-23fw-v26w-5fgq); this baseline warning did not fail the build and was not changed by the proof. Do not mark #8260's general package-selection/CI acceptance complete from this bounded local result.

| Check | Result |
|---|---|
| Public 3.8.4 nuspec/hash and source commit | Passed; NuGet SHA-256 and pinned Extensions commit match |
| Slack project direct pack and exact local `.nupkg` set | Passed; exactly one `.nupkg`, `Elsa.Slack` |
| Local nuspec dependencies, TFMs, package ID/version, source commit | Passed; Elsa 3.8.4, SlackNet 0.17.7, net8/9/10, pinned Extensions SHA |
| Public 3.8.4 consumer restore/build/start + descriptor on net8/9/10 | Passed on all three TFMs |
| Local proof package consumer restore/build/start + descriptor on net8/9/10 | Passed on all three TFMs |
| Pinned source project-reference consumer + descriptor on net10 | Passed; descriptor matches public 3.8.4 consumer |
| Offline CreateChannel request/output smoke for public and local package on net10 | Passed for both; one fake `Conversations.Create` call and matching output |
| SourceLink symbols and pinned-source URL check | Passed with `sourcelink` 3.1.1 |
| Focused Slack test project on net10 | Built; 1 skipped, 0 passed (`Not implemented yet`) |
| Core→Slack impact selection and Slack-only package selection | Passed: selector unit tests; 51 impacted test projects selected; duplicate package owners expand conservatively to all candidates |
| Other 50 impacted tests / manifest-driven CI gate | Not run; remains open |

`CreateChannel` descriptor registration uses no Slack token or API request. The check proves package loading and runtime descriptor registration, not Slack workspace connectivity or connector behavior. The inventory reports six `Watch*` Slack triggers that throw `NotImplementedException`; they are explicitly outside this package proof.
