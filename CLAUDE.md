# main Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-24

## Active Technologies

- C# latest (`<LangVersion>latest</LangVersion>`), nullable reference types enabled, implicit usings enabled — per `src/Directory.Build.props`. + `Elsa.Workflows.Runtime`, `Elsa.Workflows.Runtime.Distributed`, `Elsa.Hosting.Management` (existing heartbeat), `Elsa.Http` and `Elsa.Scheduling` (first ingress-source adapters), `Elsa.Api.Common` (`ElsaEndpoint<TRequest, TResponse>` on FastEndpoints), `Elsa.Features` (`IShellFeature`), `Microsoft.Extensions.DependencyInjection`, `Elsa.Mediator`. (002-graceful-shutdown)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# latest (`<LangVersion>latest</LangVersion>`), nullable reference types enabled, implicit usings enabled — per `src/Directory.Build.props`.

## Code Style

C# latest (`<LangVersion>latest</LangVersion>`), nullable reference types enabled, implicit usings enabled — per `src/Directory.Build.props`.: Follow standard conventions

## Recent Changes

- 002-graceful-shutdown: Added C# latest (`<LangVersion>latest</LangVersion>`), nullable reference types enabled, implicit usings enabled — per `src/Directory.Build.props`. + `Elsa.Workflows.Runtime`, `Elsa.Workflows.Runtime.Distributed`, `Elsa.Hosting.Management` (existing heartbeat), `Elsa.Http` and `Elsa.Scheduling` (first ingress-source adapters), `Elsa.Api.Common` (`ElsaEndpoint<TRequest, TResponse>` on FastEndpoints), `Elsa.Features` (`IShellFeature`), `Microsoft.Extensions.DependencyInjection`, `Elsa.Mediator`.

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
