# Testing Patterns

## Test Stack and Commands

- Runner and framework: Microsoft Testing Platform with TUnit 1.66.27.
- Assertion/mocking: TUnit assertions, NSubstitute 5.3.0, ASP.NET Core TestServer, SQLite in-memory EF Core.

```bash
dotnet test --solution Elsa.sln
dotnet test --project test/unit/Elsa.ExternalAuthentication.UnitTests/Elsa.ExternalAuthentication.UnitTests.csproj
dotnet test --project test/integration/Elsa.ExternalAuthentication.IntegrationTests/Elsa.ExternalAuthentication.IntegrationTests.csproj
```

The root `global.json` selects MTP. Outer `dotnet test` options, including
solution/project selection, belong before the literal `--`. Options after `--`
are forwarded to each TUnit test application.

## Test Layout

- TUnit projects are separated under `test/unit`, `test/integration`, and
  `test/component`; performance benchmarks live under `test/performance`.
- Test files use `*Tests.cs`; tests use `[Test]`, parameterized cases use
  `[Arguments(...)]`, and lifecycle work uses TUnit lifecycle attributes.
- TUnit assertions are asynchronous; use patterns such as
  `await Assert.That(actual).IsEqualTo(expected)`.
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

- The Microsoft Testing Platform coverage extension produces Cobertura output in
  the packages workflow.
- `test/coverage.settings.xml` supplies the shared collector configuration and
  excludes test applications and shared test-support assemblies.
- Coverage is merged and evaluated as a repository aggregate after the test run;
  individual projects do not define Coverlet thresholds or output settings.

## Evidence

- `test/Directory.Build.props`
- `test/coverage.settings.xml`
- `.github/workflows/packages.yml`
- `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Broker/BrokerSecurityTests.cs`
- `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Persistence/ExternalAuthenticationPersistenceTests.cs`
- `test/integration/Elsa.ExternalAuthentication.IntegrationTests/Links/ExternalIdentityLinkTests.cs`
