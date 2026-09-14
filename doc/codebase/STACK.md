# Technology Stack

## Runtime Summary

| Area | Value | Evidence |
|---|---|---|
| Primary language | C# (`LangVersion` latest) | `Directory.Build.props` |
| Runtime | .NET 8, 9, and 10 for source projects | `src/Directory.Build.props` |
| Package manager | NuGet with central package management | `Directory.Packages.props` |
| Build system | MSBuild solution with NUKE wrappers | `Elsa.sln`, `build/Build.cs`, `build.sh` |
| Test runner | Microsoft Testing Platform | `global.json` |

## Production Frameworks and Dependencies

| Dependency | Version | Role | Evidence |
|---|---:|---|---|
| FastEndpoints | target-dependent 7.1.1/7.2.0 | HTTP endpoint framework | `Directory.Packages.props` |
| Entity Framework Core | target-dependent 9.0.17/10.0.9 | Durable relational persistence | `Directory.Packages.props` |
| Microsoft.Extensions.Logging | target-dependent 9.0.17/10.0.9 | Application logging | `Directory.Packages.props` |

## Development Toolchain

| Tool | Purpose | Evidence |
|---|---|---|
| TUnit 1.66.27 | Test framework and Microsoft Testing Platform integration | `Directory.Packages.props`, `test/Directory.Build.props` |
| NSubstitute 5.3.0 | Test doubles | `Directory.Packages.props` |
| Microsoft code coverage extension | Cobertura coverage collected by MTP and evaluated in aggregate | `test/coverage.settings.xml`, `.github/workflows/packages.yml` |

## Key Commands

```bash
dotnet build Elsa.sln
dotnet test --solution Elsa.sln
dotnet test --project test/integration/Elsa.ExternalAuthentication.IntegrationTests/Elsa.ExternalAuthentication.IntegrationTests.csproj
```

The root `global.json` selects Microsoft Testing Platform. Keep `dotnet test`
options before the literal `--`; place TUnit/MTP application options such as
`--coverage` and `--report-trx` after it.

## Environment and Config

- ASP.NET Core applications use `appsettings*.json` and environment-variable overrides.
- Production source enables nullable reference types and implicit usings.
- External Authentication supports in-memory defaults and optional EF Core providers.

## Evidence

- `Directory.Build.props`
- `src/Directory.Build.props`
- `test/Directory.Build.props`
- `test/coverage.settings.xml`
- `Directory.Packages.props`
