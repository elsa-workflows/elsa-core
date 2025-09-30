# Elsa Core — Test Strategy Guidelines for Collaborators

**Purpose:**
This document describes recommended testing strategies for the Elsa engine. 
It is written for collaborators and aims to provide concrete, repeatable patterns you can adopt in unit, integration and component tests to make test execution deterministic, fast, and resilient across local, Docker and Kubernetes CI environments.
Additionally, it provides a consistent and resilient set of places to assert behavior (journal, activity execution endpoints, DB queries, events) and guidelines for choosing between them.

---

## Goals / Non‑functional requirements

1. **Deterministic tests** — tests should not be flaky and should produce the same results independent of circumstance.
2. **Fast feedback** — unit and integration tests should run quickly to support local development workflows and CI.
3. **Minimal reliance on real delays** — avoid `Task.Delay`, `Thread.Sleep` or real clocks except where unavoidable; prefer event-driven assertions.
4. **Environment portability** — tests should run in local dev, Docker or any other container CI environment with minimal changes.
5. **Version alignment** — workflow definition versions and test artifacts must be explicitly linked so tests refer to a specific workflow definition version.
6. **Failure simulation** — deterministic ways to simulate activity or host failures and assert correct recovery/compensation.

---

## High‑level testing pyramid for Elsa

- **Unit tests**: Activity logic, expression evaluators, small helpers. In‑process, mocking storage and scheduler. Fast and numerous.
  - Use **xUnit** with [Moq / NSubstitute] for mocking. Test activities and small components in isolation. Use in‑memory stores.
- **Integration tests**: Core engine components (WorkflowInvoker, Bookmark handling, persistence adapters) with in‑memory or ephemeral DB. Use fake clock and fake scheduler. Run in CI and locally.
  - Use `TestHostFactory` to create test hosts with DI overrides. Use code-first or serialized workflow definitions depending on the amount and scope. Also, possible to use external tooling like [JTest](https://github.com/nexxbiz/jtest).
- **Component tests**: Larger workflows with multiple activities, versioning, and persistence. Use real DB (Testcontainers or local ephemeral DB). Run in CI and locally.
  - Use `TestHostFactory` with real DB provider. Import/publish workflow definitions from a `Workflows/` folder located in the root of the test (see `tests/component/*.ComponentTests/Scenarios/*` for more examples). Assert via DB queries or journal parsing.

---

## Key constraints & recommended patterns

### 1. Should not be affected by execution times / Not depend on delays unless there is no other way

- **Synchronous execution mode**: Provide a test helper that runs workflows synchronously to completion when possible (e.g. `TestWorkflowRunner.RunToCompletion(workflowDefinition, inputs)`) — this executes activities inline and returns once the workflow is idle or complete. Use this for most assertions instead of waiting for background workers.

- **Avoid arbitrary sleeps**: If polling is necessary (e.g. for external system integration), use exponential backoff with short upper bounds and strong invariants (correlation ids) to detect test success quickly.

- **Prefer manual triggers**: For timers or external events, design tests to call the engine's trigger API (e.g. raise signal, post message, call `ResumeBookmark`) rather than waiting for a timer to fire.

- **Async completion waiters**: When invoking workflows through the HTTP `/execute` endpoint (which returns immediately), provide a helper, such as `WaitForCompletionAsync(instanceId)` (example below) that subscribes to completion events. This replaces fragile `Thread.Sleep` patterns and ensures tests only assert once the workflow has actually finished.

- **Event-driven assertions**: Subscribe to engine events (WorkflowCompleted, ActivityExecuted, etc.) in tests. Block until the event for the specific instance arrives instead of waiting arbitrary amounts of time.

---

### Example: Event-driven completion helper

Tests can subscribe to Elsa's event bus and await a specific event.

```csharp
public static class WorkflowEventWaiter
{
    public static Task<WorkflowCompleted> WaitForWorkflowCompletedAsync(
        IEventPublisher publisher,
        string instanceId,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<WorkflowCompleted>();
        timeout ??= TimeSpan.FromSeconds(30);

        void Handler(WorkflowCompleted evt)
        {
            if (evt.WorkflowInstanceId == instanceId)
                tcs.TrySetResult(evt);
        }

        publisher.Subscribe<WorkflowCompleted>(Handler);

        _ = Task.Delay(timeout.Value, cancellationToken)
            .ContinueWith(_ => tcs.TrySetException(new TimeoutException($\"Workflow {instanceId} did not complete within {timeout}.\")));

        return tcs.Task;
    }
}
```

Usage in a test:

```csharp
// Start workflow via /execute
var instanceId = response.InstanceId;

// Wait for the event
var completedEvent = await WorkflowEventWaiter.WaitForWorkflowCompletedAsync(publisher, instanceId);
Assert.Equal(instanceId, completedEvent.WorkflowInstanceId);
```

This event-driven strategy ensures assertions only happen once Elsa signals the workflow is complete, making tests both fast and deterministic.

---

### 2. Importing and publishing workflow definitions for testing workflows

Two main patterns:

**A. Programmatic/Code‑first registration (recommended for component tests)**
- Register workflows as code (C# fluent `IWorkflowBuilder`) inside test setup. This is fast and avoids serialization roundtrips.
- Use a lightweight in‑memory `IWorkflowRegistry` implementation for tests.
- Good for tests that validate runtime behavior without involving persistence or designer serialization.

**B. Serialized definitions (recommended for integration & E2E tests)**
- Store JSON/YAML workflow definition artifacts in the `tests/definitions/` folder in the repo, commit them with semantic versions, and let tests import them into the engine via the same publish/import APIs used in production.
- Tests are responsible for *publishing* the definitions into the test host if the scenario requires persistence (e.g. testing versioned definitions or import behavior).
- For bulk tests, provide an import script (`ImportTestDefinitions.sh`) that uploads all artifacts in a single batch before running assertions.

**Which to use?**
- Component tests: code-first programmatic definitions.
- Integration/E2E tests: use serialized artifacts to validate persistence, designer output and versioning behavior.


### 3. Bulk import

- Implement a **test importer utility** that accepts a directory of workflow definition artifacts (JSON/YAML) and publishes them via the engine's public API or directly seeds the persistence store. The utility should:
    - Validate schema and version.
    - Report conflicts or duplicate IDs.
    - Run in parallel but enforce deterministic ordering when versions matter.
- For very large import workloads, support an optimized DB seed path used only in tests (direct DB insert) to avoid the overhead of the full publish pipeline. Mark this as *test-only*.


### 4. Working with Docker, env, K8s cluster deployments

- **Test strategy split**:
    - Local developer tests: use in‑process hosts and in‑memory/ephemeral DBs (SQLite in-memory or Testcontainers-based DB). Fast and deterministic.
    - CI Docker Compose: spin up a lightweight containerized environment with the host app, a real DB (Postgres, SQL Server or Mongo) and optional message broker; use Testcontainers (or Docker Compose) to orchestrate in CI.
    - K8s E2E: run a small suite that deploys a test namespace with Helm or apply manifests. Use ephemeral resources and ensure cleanup. Keep these tests in a separate CI stage.

- **Use Testcontainers** (or equivalent) to provision ephemeral DBs/brokers in CI; this keeps environments close to production while still being isolated and reproducible.

- **Configuration**: Keep environment variables and k8s manifests in `tests/ci/` and parametrize connection strings so tests can switch between in‑process and containerized runs.


### 5. Managing test and workflow definition versions

- **Source control the workflow artifacts** and apply semantic versioning to their filenames or metadata (e.g. `payment-process.v1.2.0.json`).
- **Immutable artifacts**: Do not overwrite a published artifact used by tests. If a workflow changes, publish a new version and update tests to point to the new artifact.
- **Automate snapshots**: On CI, capture the actual deployed workflow definition version (ID + version) and record it with test results for traceability.


### 6. Avoiding repetition

**Test lifecycle template (common for integration/E2E tests)**
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

## Concrete examples & small templates

> The repository should provide a `tests/test-utilities` project with the following helpers that are *reusable by all test projects*.

### `TestHostFactory` (conceptual)

```csharp
public static IHost CreateTestHost(Action<IServiceCollection> configure)
{
    var host = Host.CreateDefaultBuilder()
        .ConfigureServices((ctx, services) =>
        {
            services.AddElsa(elsa =>
            {
                // Use in-memory persistence and deterministic scheduler for tests
                elsa.UseInMemoryPersistence();
                elsa.UseDeterministicScheduler();
            });

            // Additional test overrides
            configure?.Invoke(services);
        })
        .Build();

    host.Start();
    return host;
}
```

### `DeterministicScheduler` interface

```csharp
public interface IDeterministicScheduler
{
    Task<IEnumerable<Bookmark>> GetDueBookmarksAsync();
    Task TriggerBookmarkAsync(string bookmarkId, string correlationId = null);
}
```

### Example test flow (integration, in‑process)

1. Create host with `TestHostFactory`.
2. Load workflow definitions (code‑first or JSON loader).
3. Start workflow via `IWorkflowInvoker.StartAsync(definitionId, inputs, correlationId)` -> returns `instanceId`.
4. If workflow waits on bookmark, call `scheduler.TriggerBookmarkAsync(bookmarkId, correlationId)`.
5. Assert final instance status via `IWorkflowInstanceStore.FindById(instanceId)` or via `IEventCollector`.


### Example manifest (tests/manifests/decision.json)

```json
{
  "suiteName": "decision",
  "definitions": [
    { "id": "decision-true", "version": "1.2.0", "path": "definitions/decision-true.v1.2.0.json" },
    { "id": "decision-true",  "version": "1.0.0", "path": "definitions/decision-false.v1.0.0.json" }
  ]
}
```

---

## CI recommendations

- **Local unit tests**: run in `dotnet test` step (fast). Use in‑memory stores and determinism.
- **Integration tests**: run with Testcontainers (or similar) to provide real DB/broker. Run these in a separate CI job because they are slower.
- **K8s smoke tests**: optional separate stage. Deploy to ephemeral namespace via Helm and run a small suite of smoke E2E tests; tear down after.
- **Parallelization**: run multiple test matrices (DB providers) but avoid running heavy E2E jobs in parallel unless you have isolated resources.
- **Flaky test detection**: enable a flaky test retry policy for known non‑deterministic tests, but treat retries as signals to fix the underlying determinism problems.

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
2. **Create `tests/test-utilities` project** and convert a couple of existing tests to use it as examples.
3. **Define manifest schema** and commit a couple of versioned workflow definitions to `tests/definitions/`.
4. **Add one CI integration job** that uses Testcontainers (or similar) to run the integration suite against popular db providers (MySql, Postgres and Mongo, for example).
5. **Add telemetry and event collectors** to support in‑process assertions (easier than parsing journal text).
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

