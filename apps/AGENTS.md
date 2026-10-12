# Apps contributor guidance

Follow the root [AGENTS.md](../AGENTS.md). Official Apps hosts are separate from `core/src/apps/` samples.

- Preserve released-package consumption and the scoped props/CPM/NuGet configuration. Do not substitute unpublished source references or change package/image identities during source migration.
- All nine projects target net10.0. The local targets boundary prevents root source/proof inheritance and Core NuGet packing; these are application/image release units.
- Build from the root with `dotnet build Elsa.Apps.slnf`, or build `apps/ElsaApps.sln`. Run helper/policy tests with `(cd apps && python3 -m unittest discover -s scripts/tests -v)`.
- Build Dockerfiles with `apps/` as context. Preserve existing six profile, authentication, backend, storage and port contracts; MySQL remains outside the verified SQLite/default smoke scope.
- Nested `apps/.github/` metadata is retained inert source. Do not copy its publisher/Dependabot jobs to root or activate registry, trust, secret, release or retirement authority through source changes.
- Read the [source import record](../docs/integration-program/apps-source-import.md) and preserve original contributor attribution. Root owns integration and independent review gates.
