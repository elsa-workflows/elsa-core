# Elsa 3 coding instructions

Follow the repository [AGENTS.md](../AGENTS.md) for scope, code style, tests, package versions, and ADR rules. Check the current issue and source before changing behavior. This file is a short map for the consolidated product layout; it does not authorize publishing packages or archiving source repositories.

## Source and development workflow

- `core/src/modules/`, `core/src/common/`, `core/src/clients/`, and `core/src/apps/` contain the Core backend and hosts.
- `extensions/src/` contains imported Extensions modules and connectors. Keep public package IDs and activity identities stable during consolidation.
- `studio/src/` contains Blazor Studio framework, modules, hosts, browser bundles, and tests. Use the [Studio development guide](../studio/docs/README.md) for current paths, local backend binding, and paired debugging. The former standalone Studio instructions remain only as [import provenance](../docs/integration-program/legacy/studio/.github/copilot-instructions.md.source).
- Open the canonical [`Elsa.sln`](../Elsa.sln) for backend and Studio changes together. Use the targeted project or test when iterating; run broader checks when a shared dependency or build integration changes.

The Studio Designer and DomInterop ClientLib projects require Node 22. From the repository root, build their browser assets with:

```sh
./scripts/integration-program/build_studio_clientlibs.sh
```

Then build the affected .NET projects or `dotnet build Elsa.sln`. Production projects target net8.0, net9.0, and net10.0 unless a project explicitly overrides the shared target frameworks. Run the relevant tests with `dotnet test <project> --framework net10.0`; include the other target frameworks when the change affects multi-target behavior. Studio has tests, so do not treat an empty `dotnet test` run as proof of coverage.

For a local backend/Blazor session, follow the Studio guide's separate backend and `Elsa.Studio.Host.Server` hosts and its ignored `appsettings.Local.json` URL binding. Keep local credentials and client secrets out of tracked files. Do not assume a backend route exists merely because a Studio client declares it; verify the actual host configuration and permissions.

## Build, compatibility, and releases

Central package versions live in `Directory.Packages.props`; framework defaults live in each product’s `src/Directory.Build.props`. Prefer the repository's existing build and test commands in `AGENTS.md`. Investigate restore or build failures rather than changing package versions to a guessed "nearest" version or masking a required source failure.

The [integration program](../docs/integration-program/README.md) records import evidence, package boundaries, and pending gates. Imported Extensions/Studio publishers are retained as inert provenance. The [first consolidated release policy](../docs/adr/2026-09-28-lockstep-consolidated-release-and-publisher-cutover.md) governs lockstep 3.10 and the single Core publisher. The earlier [release-unit manifest](../docs/integration-program/release-units.json) and [publisher handoff](../docs/integration-program/publisher-handoff.md) retain their bounded proof scope; they do not establish live publisher cutover. Do not enable a publisher, push a package to a feed, or treat a local proof version as a release allocation without the program's explicit cutover approval.

The history-bearing import landed through [PR #8409](https://github.com/elsa-workflows/elsa-core/pull/8409), with later source/release catch-up through [PR #8624](https://github.com/elsa-workflows/elsa-core/pull/8624). Preserve that history and check current source pins and exact-head CI before claiming acceptance of a relocated tree. Do not replace imported source with a local checkout.
