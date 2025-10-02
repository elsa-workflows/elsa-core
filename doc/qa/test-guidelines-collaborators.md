# Elsa Core — Test Strategy Guidelines for Collaborators

**Purpose:**

This document describes recommended testing strategies for the Elsa engine. 

It is written for collaborators and aims to provide concrete, repeatable patterns you can adopt in unit, integration and component tests to make test execution deterministic, fast, and resilient.

Additionally, it provides a consistent and resilient set of places to assert behavior (journal, activity execution endpoints, DB queries, events) and guidelines for choosing between them.

---

## Summary
The philosophy of elsa test strategy can be summarized as:

***Whenever a test fails, it should provide a clear direction towards the cause of the problem.***
--

This means that if it does not point directly to the source of the issue, it should take the fewest possible amount of steps to get there.

---

## Goals / Non‑functional requirements

1. **Deterministic tests** — tests should not be flaky and should produce the same results independent of circumstance.
2. **Fast feedback** — unit and integration tests should run quickly to support local development workflows and CI.
3. **Minimal reliance on real delays** — avoid `Task.Delay`, `Thread.Sleep` or real clocks except where unavoidable; prefer event-driven assertions.
4. **Version alignment** — workflow definition versions and test artifacts must be explicitly linked so tests refer to a specific workflow definition version.
5. **Failure simulation** — deterministic ways to simulate activity or host failures and assert correct recovery/compensation.

---

## High‑level testing pyramid for Elsa

- **Unit tests**: Activity logic, expression evaluators, small helpers, services and providers. In‑process, mocking storage and scheduler. Fast and numerous.
  - Use **xUnit** with [Moq / NSubstitute] for mocking.
  - Test activities and small components in isolation.
  - Use in‑memory stores.
- **Integration tests**: Core engine components (WorkflowInvoker, Bookmark handling, persistence adapters) with in‑memory or ephemeral DB. Use fake scheduler. Run in CI and locally.
  - Use [`TestApplicationBuilder`](../../src/common/Elsa.Testing.Shared.Integration/TestApplicationBuilder.cs) to create test hosts with DI overrides.
  - Place workflow definitions in a `Workflows/` folder located in the root of the test (see [`MigrationTests`](../../test/integration/Elsa.Alterations.IntegrationTests/MigrationTests.cs)).
  - Use serialized workflow or code-first definitions depending on the amount and scope.
  - Also, possible to use external tooling like [JTest](https://github.com/nexxbiz/jtest).
- **Component tests**: Larger workflows with multiple activities, versioning, and persistence. 
  - Use real DB (local ephemeral DB). Run in CI and locally.
  - Use real components (e.g. [`IWorkflowRuntime`](../../src/modules/Elsa.Workflows.Runtime/Contracts/IWorkflowRuntime.cs), see [`ExecuteWorkflowsTests`](../../test/component/Elsa.Workflows.ComponentTests/Scenarios/ExecuteWorkflows/ExecuteWorkflowsTests.cs)).
  - Use [`AppComponentTest`](../../test/component/Elsa.Workflows.ComponentTests/Helpers/Abstractions/AppComponentTest.cs) with real DB provider. 
  - Place workflow definitions in a `Workflows/` folder located in the root of the test (see [`ExecuteWorkflowsTests`](../../test/component/Elsa.Workflows.ComponentTests/Scenarios/ExecuteWorkflows/ExecuteWorkflowsTests.cs)). 
  - Assert via DB queries or journal parsing.

---

## Elsa aspects to be tested:
- **Activities**:
  - Unit test each activity class in isolation.
    - all configurations and edge cases.
  - Integration test activities using [`TestApplicationBuilder`](../../src/common/Elsa.Testing.Shared.Integration/TestApplicationBuilder.cs) for less common, tricky scenarios (e.g. [`ForEachTests`](../../test/integration/Elsa.Activities.IntegrationTests/ForEachTests.cs)).
- **Workflow execution**: 
  - Test workflow lifecycle, input/output, bookmarks, persistence, and resumption.
  - Use `RunWorkflowUntilEndAsync` extension method in [`RunWorkflowExtensions`](../../src/common/Elsa.Testing.Shared.Integration/RunWorkflowExtensions.cs) for deterministic execution.
- **Persistence**: 
  - Test each [`IWorkflowInstanceStore`](../../src/modules/Elsa.Workflows.Management/Contracts/IWorkflowInstanceStore.cs) implementation.
  - Test different storages of variables (Workflow Instance, Memory).
- **Serialization**: 
  - Unit test JSON serialization and deserialization of workflow definitions and instances.
  - Integration test roundtrip of definitions through API.
- **Triggers:** 
  - Test triggers for correct scheduling, invocation and resuming of workflows.
- **API**: 
  - Test HTTP endpoints for workflow execution, definition management, and instance querying.


## Unit tests

Do when testing:
- Individual activity logic.
- Small components in isolation.
- Logic before/after persistence .
- Logic before/after interactions between components.
- Expression evaluation.
- Providers and services (elsa implementations).

## Integration tests

Do when:
- Testing workflow execution with multiple activities.
- Need to validate persistence, bookmarks, and resumption.
- Testing core engine components (WorkflowInvoker, Bookmark handling).

For component tests, we don't have to register code-first workflows, but we do need to do so explicitly for integration tests when creating the TestApplicationBuilder. For example:
```csharp
_services = new TestApplicationBuilder(testOutputHelper)
   .WithWorkflowsFromDirectory("Scenarios", "DependencyWorkflows", "Workflows")
   .Build();
```

## Component tests

Do when:

## Key constraints & recommended patterns

### 1. Should not be affected by execution times / Not depend on delays unless there is no other way

- **Synchronous execution mode**: Use this for most assertions instead of waiting for background workers.

- **Avoid arbitrary sleeps**: If polling is necessary (e.g. for external system integration), use exponential backoff with short upper bounds and strong invariants (correlation ids) to detect test success quickly.

- **Prefer manual triggers**: For timers or external events, design tests to call the engine's trigger API (e.g. raise signal, post message, call `ResumeBookmark`) rather than waiting for a timer to fire.

- **Async completion waiters**: When invoking workflows through the HTTP `/execute` endpoint (which returns immediately), provide a helper, such as `WaitForCompletionAsync(instanceId)` (example below) that subscribes to completion events. This replaces fragile `Thread.Sleep` patterns and ensures tests only assert once the workflow has actually finished.

- **Event-driven assertions**: Subscribe to engine events (WorkflowCompleted, ActivityExecuted, etc.) in tests. Block until the event for the specific instance arrives instead of waiting arbitrary amounts of time.

---

### Example: Event-driven completion helper

Tests can run workflows using the `RunWorkflowUntilEndAsync` extension method in [`RunWorkflowExtensions`](../../src/common/Elsa.Testing.Shared.Integration/RunWorkflowExtensions.cs) to reliably await for the execution without using `Thread.Sleep` or `Task.Delay`.


Usage in a test:

```csharp
private readonly IServiceProvider _services;

public Tests(ITestOutputHelper testOutputHelper)
{
    _services = new TestApplicationBuilder(testOutputHelper)
        .Build();
}

[Fact]
public async Task Test1()
{
    // Populate registries
    await _services.PopulateRegistriesAsync();

    // Import workflows
    await _services.ImportWorkflowDefinitionAsync("Workflows/workflow-1.json");
    await _services.ImportWorkflowDefinitionAsync("Workflows/workflow-2.json");

    // Run
    var workflowState = await _services.RunWorkflowUntilEndAsync("my-workflow");
    
    // Assert
    // .......
}
```

This extension method ensures assertions only happen once the workflow is complete, making tests both fast and deterministic.

---

###  Importing workflow definitions for testing workflows

Two main patterns:

**A. Code‑first registration (recommended for component tests)**
- Register workflows as code inside test setup. This is fast and avoids serialization roundtrips.
- Good for tests that validate runtime behavior without involving persistence or designer serialization.
- Place workflow definitions in a `Workflows/` folder located in the root of the test (see [`ExecuteWorkflowsTests`](../../test/component/Elsa.Workflows.ComponentTests/Scenarios/ExecuteWorkflows/ExecuteWorkflowsTests.cs) for an example).

**B. Serialized definitions (recommended for integration tests)**
- Place workflow definitions in a `Workflows/` folder located in the root of the test (see [`MigrationTests`](../../test/integration/Elsa.Alterations.IntegrationTests/MigrationTests.cs) for an example).

**Which to use?**
- Integration tests: use serialized JSON workflows.
- Component tests: code-first definitions or workflows created via designer and exported to JSON to validate persistence, designer output and versioning behavior.
---



### 5. Managing test and workflow definition versions

- **Source control the workflow artifacts** and apply semantic versioning to their filenames or metadata (e.g. `payment-process.v1.2.0.json`).
- **Immutable artifacts**: Do not overwrite a published artifact used by tests. If a workflow changes, publish a new version and update tests to point to the new artifact.
- **Automate snapshots**: On CI, capture the actual deployed workflow definition version (ID + version) and record it with test results for traceability.

### 6. Avoiding repetition

**Test lifecycle template (common for integration tests)**
1. **Provision** the environment (in‑process host or containerized test environment).
2. **Reset** persistence (drop / recreate DB schema or use a clean DB instance).
3. **Import/Publish** required workflow definitions.
4. **Register** test hooks (e.g. fake scheduler, activity test doubles, callback endpoints).
5. **Invoke** the workflow via the API, direct invoker or trigger.
6. **Trigger** bookmarks manually if needed.
7. **Assert** via journal/events/DB/state.
8. **Tear down** environment and collect artifacts (logs, DB snapshots) on failures.

**Reusable code artifacts**
- `TestHostFactory`: create and configure test hosts (DI container, fake services).
- `WorkflowDefinitionLoader`: loads definitions from disk, validates versions, and publishes them.
- `DeterministicScheduler`: test scheduler with `TriggerOnce(correlationId)`.
- `ActivityTestProbe`: an in‑process activity wrapper that captures inputs/outputs and emits structured events to assert against.
- JSON manifest schema for suite imports.

Include these helpers in a shared test utilities NuGet/package so all test projects can reuse them and reduce duplication.

### 7. Assertion targets and alternatives

#### Journal / Activity Execution Endpoints

**Pros**
- Journal provides a chronological, human‑readable trace of what happened and is close to production observability.
- Activity execution endpoints (if available) allow real API surface testing and validate telemetry and audit paths.

**Cons**
- Journal may be high volume and require parsing to find relevant entries; tests risk being brittle if journal format changes.
- Accessing activity endpoints over HTTP introduces network flakiness in E2E tests.

#### Alternatives / Complementary options
- **Direct DB queries**: Query the workflow instance table, bookmarks, and activity logs. Stronger for deterministic assertions about state (e.g. `WorkflowInstance.Status == Completed`).
- **Event stream / notifications**: Subscribe to internal events (in tests) via the mediator or a test `INotificationHandler` to assert lifecycle events as they happen in real time.
- **Activity test probes**: Instrument activities in tests to emit structured markers (test hooks) that are easier to assert than raw journal text.

**Recommendations:** 
- For unit and component tests, assert on in‑process events and test probes. For integration/E2E tests assert on durable state in the DB and validated events (or journal), and use correlation ids to make queries deterministic. Avoid relying solely on formatted journal lines.
- Always propagate and assert on correlation IDs attached to workflow instances and events to locate the exact instance you need.

### 8. Execute endpoint vs HTTP activity for tests

- **Testing activities in isolation**: Unit test each activity class by constructing an `ActivityContext` and invoking `ExecuteAsync()` or using an `ActivityTestProbe`. This is the fastest and most isolated option.

- **Testing activities in workflow**: Component tests should compose small workflows in code and run them through the `WorkflowInvoker` to validate end‑to‑end semantics (including variable passing, bookmarks, parallelism). Keep these tests in‑process to avoid network boundaries.

- **Testing via HTTP**: Use HTTP endpoint activities for integration and true E2E testing of the server host, middleware and serialization. These tests are slower and belong in the E2E suite. There are two possibilities: 
  - Workflow with the desired activity and a connected entry HTTP endpoint activity;
  - `/execute` endpoint that runs the workflow directly, in this case, the HTTP endpoint activity is not necessary.

**Recommendation:** Unit test activities directly. Integration test the activity inside workflows with the in‑process invoker. Reserve HTTP‑based tests for integrations and full server behavior validation.

### 9. How to test failures (fail activity, missing instance, etc.)

- **Explicit fail activities**: Unit test fail activities and assert they raise the expected error code and cause the workflow instance to transition to the appropriate state (e.g. Faulted). In integration tests, run the workflow to the failure point synchronously where possible and assert instance state.

- **No workflow instance returned / missing instance**:
    - Use correlation IDs passed at invocation time. The engine should return an `instanceId` or `correlationId` when starting. Tests should persist that id and query the instance store using it rather than using `GetLatest` semantics.
    - If there are no guaranteed instance returns, wrap invocation in a test helper that extracts and returns the created instance id from either the API response, journal event, or DB entry.

- **Correlated queries vs GetLatest**: Avoid `GetLatest` in tests because it is non‑deterministic in parallel runs. Use correlation ids, explicit instance IDs, or filters (workflow definition id + start time + unique test tag) to locate the exact instance.

- **Simulating host failures**: In integration tests, simulate crashes by killing the host process/container mid‑execution and restarting it to validate persistence and resume semantics. Use persistent DB so state survives host restart.

### 10. Avoid instance ambiguity

- **Tag instances on creation**: Allow tests to send an explicit `TestCorrelationId` or `TestTag` as part of workflow input/metadata. Persist this tag to the instance record. Use it to query the DB deterministically.

- **Return the instance id on start**: Ensure test harness captures the created instance id from the start API or in‑process invoker and uses that id for all subsequent queries.

### 11. Consistent execution environment

- **Deterministic defaults**: For tests, use known configuration values (e.g. `MaxRetries=0`, `ShortCircuitLongRunning=true`) to eliminate production variability.
- **Isolated DB per test process**: Use ephemeral DBs (unique DB name per test run) to avoid cross‑test contamination.
- **Artifact collection**: On failure, collect logs, DB snapshot and exported journal to help triage flakiness.

---



## Failure injection and resilience testing

- **Deterministic fault injection**: provide test stubs for activities that throw predictable exceptions on demand. Use configuration flags or special test input to trigger them.
- **Host process kill**: in containerized tests, kill the host midway (Docker/kill or stop container) and restart to verify persistence/resumption.
- **Network partitions**: simulate by blocking network connections to DB/broker in the test environment to ensure graceful failure handling.

---

## What to deliver in the repository

- `tests/test-utilities/` project with common helpers (TestHostFactory, DeterministicScheduler, Test probes)
- `tests/definitions/` with versioned workflow artifacts
- `tests/manifests/` for suites referencing definitions and versions
- `tests/ci/docker-compose.yml` and `tests/ci/k8s/` manifests for reproducible integration/E2E runs
- Example tests showing patterns:
    - Unit test for `HttpRequestActivity` (activity isolation)
    - Integration test for basic workflow (deterministic scheduler)
    - E2E test that deploys a host in Docker and asserts via DB queries and journal

---

## Next steps / TODOs for the team

1. **Increase** unit test coverage of existing code using these patterns.
2. **Add `IDeterministicScheduler` abstractions** and implement test doubles.
3. **Create `tests/test-utilities` project** and convert a couple of existing tests to use it as examples.
4. **Define manifest schema** and commit a couple of versioned workflow definitions to `tests/definitions/`.
5. **Add one CI integration job** that uses Testcontainers (or similar) to run the integration suite against popular db providers (MySql, Postgres and Mongo, for example).
6. **Add telemetry and event collectors** to support in‑process assertions (easier than parsing journal text).
---

## Appendix: Quick checklist for writing a new test

- [ ] Will this be a unit, integration or E2E test? Choose minimal scope.
- [ ] Can we avoid real time? If yes, use fake trigger.
- [ ] Will the test load a workflow definition artifact? Pin its version in the manifest.
- [ ] Will the test depend on DB state? Use a clean DB instance per test run.
- [ ] Use correlation ids for all invocations.
- [ ] Assert using deterministic state (instance id, DB record, or event) rather than `GetLatest`.
- [ ] On failure, capture logs and DB snapshot for diagnosis.

---

