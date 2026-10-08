# main Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-24

## Active Technologies

- C# latest (`<LangVersion>latest</LangVersion>`), nullable reference types enabled, implicit usings enabled — per `core/src/Directory.Build.props`. + `Elsa.Workflows.Runtime`, `Elsa.Workflows.Runtime.Distributed`, `Elsa.Hosting.Management` (existing heartbeat), `Elsa.Http` and `Elsa.Scheduling` (first ingress-source adapters), `Elsa.Api.Common` (`ElsaEndpoint<TRequest, TResponse>` on FastEndpoints), `Elsa.Features` (`IShellFeature`), `Microsoft.Extensions.DependencyInjection`, `Elsa.Mediator`. (002-graceful-shutdown)

## Project Structure

```text
core/{src,test,docs,samples}/
extensions/{src,test,docs,samples}/
studio/{src,test,docs,samples}/
docs/{adr,integration-program}/
build/
scripts/
```

Use root `Elsa.sln` and the existing root solution filters. Follow [AGENTS.md](AGENTS.md) and the [contribution guide](CONTRIBUTING.md) for product ownership and checks.

## Commands

Run from the repository root:

```sh
dotnet build Elsa.sln
dotnet test core/test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj
```

Use the owning product's documentation for focused selections: [Core](core/docs/README.md), [Extensions](extensions/docs/README.md), [Studio](studio/docs/README.md).

## Code Style

C# latest (`<LangVersion>latest</LangVersion>`), nullable reference types enabled, implicit usings enabled — per `core/src/Directory.Build.props`.: Follow standard conventions

## Recent Changes

- 002-graceful-shutdown: Added C# latest (`<LangVersion>latest</LangVersion>`), nullable reference types enabled, implicit usings enabled — per `src/Directory.Build.props`. + `Elsa.Workflows.Runtime`, `Elsa.Workflows.Runtime.Distributed`, `Elsa.Hosting.Management` (existing heartbeat), `Elsa.Http` and `Elsa.Scheduling` (first ingress-source adapters), `Elsa.Api.Common` (`ElsaEndpoint<TRequest, TResponse>` on FastEndpoints), `Elsa.Features` (`IShellFeature`), `Microsoft.Extensions.DependencyInjection`, `Elsa.Mediator`.

<!-- MANUAL ADDITIONS START -->
## Agent Operating Principles

- Do not assume, hide confusion, or flatten uncertainty; surface questions, constraints, and tradeoffs explicitly.
- Write the minimum code that solves the defined problem; do not add speculative abstractions, features, or cleanup.
- Touch only the files and behavior required for the task; clean up only issues introduced by your own changes.
- Define success criteria before implementation, then iterate until the criteria are verified or clearly state what could not be verified.

<!-- MANUAL ADDITIONS END -->
