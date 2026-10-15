# Core development

Core owns the workflow engine, backend modules, clients and application hosts under
[`core/src`](../src), with tests under [`core/test`](../test). Product documentation
and feature plans live here; shared architecture decisions live in
[`docs/adr`](../../docs/adr) and consolidation evidence in
[`docs/integration-program`](../../docs/integration-program).

Run commands from the repository root. Open the root [`Elsa.sln`](../../Elsa.sln)
for integrated development, or `Elsa.Core.slnf` for the Core-only development
selection. Functional filters such as `Elsa.Runtime.slnf` select smaller subsets:

```sh
dotnet build Elsa.Core.slnf
dotnet test core/test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj
dotnet run --project core/src/apps/Elsa.Server.Web/Elsa.Server.Web.csproj
```

The Core filter excludes `Elsa.Secrets.DefaultHost.IntegrationTests`, whose
fixture references the Extensions workbench. That fixture remains in the full
`Elsa.sln` and integrated test coverage; the focused filter does not certify it.

The full shared build remains `dotnet build Elsa.sln` or `./build.sh`. Select the
relevant test project while iterating; integration/component tests may require
external services as documented by their fixtures. Library framework policy is
centralized in `core/src/Directory.Build.props`; hosts and tests can select a
specific framework. A focused filter is not a complete solution or package proof.

For the reference backend's configuration and developer prerequisites, read
[`Elsa.Server.Web`](../src/apps/Elsa.Server.Web/README.md). To develop its UI in the
same checkout, use the [Studio entrypoint](../../studio/docs/README.md). Optional
connector modules belong to [Extensions](../../extensions/docs/README.md).

The [repository wiki](wiki/README.md), [feature specifications](specs),
[contribution guide](../../CONTRIBUTING.md) and [agent guidance](../../AGENTS.md)
provide further context. Path relocation preserves package, activity and schema
identities. Successful source builds do not authorize package publication or
source-repository archival; the [first consolidated release policy](../../docs/adr/2026-09-28-lockstep-consolidated-release-and-publisher-cutover.md)
and [artifact proof](../../docs/integration-program/consolidated-package-proof.md)
retain their separate gates.
