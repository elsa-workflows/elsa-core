# Technology Stack

## Runtime Summary

| Area | Value | Evidence |
|---|---|---|
| Primary language | C# (`LangVersion` latest) | `Directory.Build.props` |
| Runtime | .NET 8, 9, and 10 for source projects | `src/Directory.Build.props` |
| Package manager | NuGet with central package management | `Directory.Packages.props` |
| Build system | MSBuild solution with NUKE wrappers | `Elsa.sln`, `build/Build.cs`, `build.sh` |

## Production Frameworks and Dependencies

| Dependency | Version | Role | Evidence |
|---|---:|---|---|
| FastEndpoints | target-dependent 7.1.1/7.2.0 | HTTP endpoint framework | `Directory.Packages.props` |
| Entity Framework Core | target-dependent 9.0.17/10.0.9 | Durable relational persistence | `Directory.Packages.props` |
| Microsoft.Extensions.Logging | target-dependent 9.0.17/10.0.9 | Application logging | `Directory.Packages.props` |

## Development Toolchain

| Tool | Purpose | Evidence |
|---|---|---|
| xUnit 2.9.3 | Test framework | `Directory.Packages.props`, `test/Directory.Build.props` |
| NSubstitute 5.3.0 | Test doubles | `Directory.Packages.props` |
| coverlet | Coverage with a 10% project threshold | `test/Directory.Build.props` |

## Key Commands

```bash
dotnet build Elsa.sln
dotnet test Elsa.sln
dotnet test test/integration/Elsa.ExternalAuthentication.IntegrationTests/Elsa.ExternalAuthentication.IntegrationTests.csproj
./build.sh Test
```

## Environment and Config

- ASP.NET Core applications use `appsettings*.json` and environment-variable overrides.
- Production source enables nullable reference types and implicit usings.
- External Authentication supports in-memory defaults and optional EF Core providers.

## Evidence

- `Directory.Build.props`
- `src/Directory.Build.props`
- `test/Directory.Build.props`
- `Directory.Packages.props`

