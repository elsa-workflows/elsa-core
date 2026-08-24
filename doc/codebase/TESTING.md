# Testing Patterns

## Test Stack and Commands

- Framework: xUnit 2.9.3.
- Assertion/mocking: xUnit assertions, NSubstitute 5.3.0, ASP.NET Core TestServer, SQLite in-memory EF Core.

```bash
dotnet test Elsa.sln
dotnet test test/unit/Elsa.ExternalAuthentication.UnitTests/Elsa.ExternalAuthentication.UnitTests.csproj
dotnet test test/integration/Elsa.ExternalAuthentication.IntegrationTests/Elsa.ExternalAuthentication.IntegrationTests.csproj
```

## Test Layout

- Test projects are separated under `test/unit`, `test/integration`, `test/component`, and `test/performance`.
- Test files use `*Tests.cs`; xUnit setup uses constructors or `IAsyncLifetime`.
- Shared helpers live under `src/common/Elsa.Testing.Shared*`.

## Test Scope Matrix

| Scope | Covered | Typical target | Notes |
|---|---|---|---|
| Unit | Yes | Validators, in-memory stores, isolated services | Fast and dependency-light |
| Integration | Yes | Broker flow, REST endpoints, SQLite persistence | Used for external-auth tracking regression |
| Component | Yes | Workflow runtime behavior | Not needed for this identity defect |
| E2E browser | `[TODO]` | Studio rendering | Studio repository is separate |

## Mocking and Isolation Strategy

- Prefer real in-memory provisioners and stores for state assertions.
- Substitute adapters, role providers, and notification senders at boundaries.
- SQLite `Data Source=:memory:` provides isolated durable-store tests.

## Coverage and Quality Signals

- Coverlet generates Cobertura, LCOV, and OpenCover output.
- Default project threshold is 10% total line coverage.
- Focused filtered runs may disable coverage; the full project run enforces the threshold.

## Evidence

- `test/Directory.Build.props`
- `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Broker/BrokerSecurityTests.cs`
- `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Persistence/ExternalAuthenticationPersistenceTests.cs`
- `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Links/ExternalIdentityLinkTests.cs`

