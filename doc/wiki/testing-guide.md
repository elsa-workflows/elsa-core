# Testing Guide

Elsa uses unit, integration, component, and performance tests. The best test choice depends on what boundary you are changing.

The detailed internal testing strategy is [doc/qa/test-guidelines.md](../qa/test-guidelines.md). This page is a wiki-sized map.

## Test Folders

| Folder | Purpose |
| --- | --- |
| [test/unit](../../test/unit) | Isolated services, activities, converters, stores, validators, descriptors, and small logic. |
| [test/integration](../../test/integration) | In-process composition of workflow engine services, expressions, activities, runtime behavior, and module integration. |
| [test/component](../../test/component) | Host-level workflows, persistence-backed scenarios, HTTP workflows, clustered behavior, and lifecycle behavior. |
| [test/performance](../../test/performance) | Benchmark and throughput scenarios. |

Shared helpers:

- [Elsa.Testing.Shared](../../src/common/Elsa.Testing.Shared)
- [Elsa.Testing.Shared.Integration](../../src/common/Elsa.Testing.Shared.Integration)
- [Elsa.Testing.Shared.Component](../../src/common/Elsa.Testing.Shared.Component)

## Choosing A Test Type

| Change | Preferred test |
| --- | --- |
| Activity logic without persistence or scheduler | Unit test with `ActivityTestFixture`. |
| Expression evaluator/parser behavior | Unit test or language-specific integration test. |
| Workflow execution semantics | Integration test with workflow runner/test fixture. |
| Bookmarks, triggers, runtime dispatch, recovery | Integration test; component test if host lifecycle or persistence matters. |
| API endpoint shape or authorization | Unit/integration endpoint test if existing pattern exists; component test for host-level behavior. |
| EF Core store or migration | Provider-specific integration/component test. |
| HTTP workflows | Component test under HTTP workflow scenarios. |
| Structured log SQLite persistence | SQLite integration test project. |

## Useful Commands

Build the solution:

```bash
dotnet build Elsa.sln
```

Run all tests:

```bash
dotnet test Elsa.sln
```

Run the NUKE test target:

```bash
./build.sh Test
```

Run targeted projects:

```bash
dotnet test test/unit/Elsa.Workflows.Core.UnitTests/Elsa.Workflows.Core.UnitTests.csproj
dotnet test test/integration/Elsa.Workflows.IntegrationTests/Elsa.Workflows.IntegrationTests.csproj
dotnet test test/component/Elsa.Workflows.ComponentTests/Elsa.Workflows.ComponentTests.csproj
dotnet test test/integration/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests/Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.IntegrationTests.csproj
```

Run ElsaScript DSL tests:

```bash
./run-dsl-tests.sh
```

## Activity Tests

Use `ActivityTestFixture` from [Elsa.Testing.Shared](../../src/common/Elsa.Testing.Shared). Activity tests should assert activity outputs, variables, scheduled child activities, outcomes, and fault behavior without needing a host.

Examples live in:

- [test/unit/Elsa.Activities.UnitTests](../../test/unit/Elsa.Activities.UnitTests)
- [test/unit/Elsa.Workflows.Core.UnitTests](../../test/unit/Elsa.Workflows.Core.UnitTests)

## Workflow Integration Tests

Use integration helpers when activity behavior depends on workflow runner behavior, variables, activity outputs, expressions, or bookmarks.

Examples:

- [test/integration/Elsa.Workflows.IntegrationTests](../../test/integration/Elsa.Workflows.IntegrationTests)
- [test/integration/Elsa.Activities.IntegrationTests](../../test/integration/Elsa.Activities.IntegrationTests)
- [test/integration/Elsa.JavaScript.IntegrationTests](../../test/integration/Elsa.JavaScript.IntegrationTests)

## Component Tests

Component tests use host fixtures. The base class is [AppComponentTest](../../test/component/Elsa.Workflows.ComponentTests/Helpers/Abstractions/AppComponentTest.cs). It creates a scope, pushes the default tenant context, tracks in-flight workflows, and waits for them to idle on dispose.

Fixture landmarks:

- [App](../../test/component/Elsa.Workflows.ComponentTests/Helpers/Fixtures/App.cs)
- [Cluster](../../test/component/Elsa.Workflows.ComponentTests/Helpers/Fixtures/Cluster.cs)
- [WorkflowServer](../../test/component/Elsa.Workflows.ComponentTests/Helpers/Fixtures/WorkflowServer.cs)
- [Infrastructure](../../test/component/Elsa.Workflows.ComponentTests/Helpers/Fixtures/Infrastructure.cs)

Use component tests when host lifecycle, HTTP server behavior, actual persistence, distributed runtime behavior, or multiple services working together are essential to the assertion.

## Structured Log Tests

Structured log tests are split by layer:

- core module unit tests
- core module integration tests
- relational unit tests
- SQLite integration tests

This mirrors the architecture: core capture and API behavior should not require SQLite; SQLite tests should prove durability, migrations, retention, timestamp storage, queue overflow, and filtering.

## Good Test Hygiene

- Prefer targeted project tests while iterating.
- Add a regression test for a bug before or alongside the fix.
- Keep arrange/setup in constructors or small helpers when it repeats.
- Use `IAsyncDisposable` or xUnit async lifetime patterns for async teardown.
- Avoid sleeps when a deterministic signal or store assertion is available.
- For multi-targeting issues, consider whether all target frameworks need coverage.
- When changing shared runtime or persistence behavior, finish with a broader build/test run if feasible.
