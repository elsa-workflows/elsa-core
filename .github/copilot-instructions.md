# Elsa 3 coding instructions

Follow the repository [AGENTS.md](../AGENTS.md) for scope, code style, tests, package versions, and ADR rules. Check the current issue and source before changing behavior. This file is a short map for the consolidated source in the draft integration branch; it does not authorize merging the import or publishing packages.

## Source and development workflow

- `src/modules/`, `src/common/`, `src/clients/`, and `src/apps/` contain the Core backend and hosts.
- `src/extensions/` contains imported Extensions modules and connectors. Keep public package IDs and activity identities stable during consolidation.
- `src/studio/` contains Blazor Studio framework, modules, hosts, browser bundles, and tests. Use the [Studio development guide](../doc/studio/README.md) for current paths, local backend binding, and paired debugging. The former standalone Studio instructions remain only as [import provenance](../doc/integration-program/legacy/studio/.github/copilot-instructions.md.source).
- Open the canonical [`Elsa.sln`](../Elsa.sln) for backend and Studio changes together. Use the targeted project or test when iterating; run broader checks when a shared dependency or build integration changes.

The Studio Designer and DomInterop ClientLib projects require Node 22. From the repository root, build their browser assets with:

```sh
./scripts/integration-program/build_studio_clientlibs.sh
```

Then build the affected .NET projects or `dotnet build Elsa.sln`. Production projects target net8.0, net9.0, and net10.0 unless a project explicitly overrides the shared target frameworks. Run the relevant tests with `dotnet test <project> --framework net10.0`; include the other target frameworks when the change affects multi-target behavior. Studio has tests, so do not treat an empty `dotnet test` run as proof of coverage.

For a local backend/Blazor session, follow the Studio guide's separate backend and `Elsa.Studio.Host.Server` hosts and its ignored `appsettings.Local.json` URL binding. Keep local credentials and client secrets out of tracked files. Do not assume a backend route exists merely because a Studio client declares it; verify the actual host configuration and permissions.

## Build, compatibility, and releases

Central package versions live in `Directory.Packages.props`; framework defaults live in `src/Directory.Build.props`. Prefer the repository's existing build and test commands in `AGENTS.md`. Investigate restore or build failures rather than changing package versions to a guessed "nearest" version or masking a required source failure.

The [integration program](../doc/integration-program/README.md) records import evidence, package boundaries, and pending gates. Imported Extensions/Studio publishers are retained as inert provenance. Source co-location does not make every package share one version or release train. The [release-unit manifest](../doc/integration-program/release-units.json) and [publisher handoff](../doc/integration-program/publisher-handoff.md) define the bounded package proof and current publishing ownership. Do not enable a publisher, push a package to a feed, or treat a local proof version as a release allocation without the program's explicit cutover approval.

The history-bearing import is [draft PR #8409](https://github.com/elsa-workflows/elsa-core/pull/8409). Check its current head, open child PRs, source pins, and CI before claiming consolidated behavior on `main`. Preserve the upstream history merge commit and avoid silently replacing imported source with a local checkout.
