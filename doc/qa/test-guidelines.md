# Elsa Core — Testing Strategy

## Purpose

This document is a practical test guideline. It tells you *what* to test, *when* to test it, and *how* to write deterministic, actionable tests using the repository's existing test helpers and patterns.

---

## Summary
The philosophy of testing in Elsa can be summarized as:

***Whenever a test fails, it should provide a clear direction towards the cause of the problem.***

Tests should be fast, deterministic, and precise: they should pinpoint the failing subsystem (activity, invoker, persistence, scheduler, etc.) with minimal noise. 

For contributors, tests are the first line of code review: they must document intended behaviour and prevent regressions.

---

## Glossary
- **Activity** — a single unit of workflow logic (e.g., WriteLine, If, ForEach, HttpRequest).
- **Workflow** — a graph of activities connected by control flow.
- **Workflow Definition** — a serializable representation of a workflow (JSON or code).
- **Workflow Instance** — a persisted execution of a workflow definition, including state, variables, and journal.
- **Bookmark** — a pause point in a workflow where execution is suspended until an external event resumes it.
- **Invoker** — the core engine component that orchestrates workflow execution, including activity execution, scheduling, and state transitions.
- **Scheduler** — the subsystem that manages background tasks, timers, and resumption of workflows.
- **Journal** — a log of all activity executions and state changes in a workflow instance.
- **Persistence** — the storage mechanism for workflow definitions and instances (e.g., EF Core, MongoDB).
- **Unit Test** — a test that verifies a small, isolated piece of code (such as a function or method) works as expected.
- **Integration Test** — a test that verifies the interaction between multiple components or subsystems works as expected.
- **Component Test** — a test that verifies the behavior of a larger part of the system, often involving persistence and external dependencies.

---

## High-level testing pyramid

- **Unit tests** — single-class logic (activities, converters, expression evaluators, serializers, service providers). Fast; no persistence.
- **Integration tests** — multiple Elsa subsystems together (e.g., invoker + activities + registries). In-process; may deserialize workflow JSON. Use [`IWorkflowRunner.RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) and [`PopulateRegistriesAsync()`](../../src/common/Elsa.Testing.Shared.Integration/ServiceProviderExtensions.cs) when using existing definitions.
- **Component tests** — persisted behaviour, journal/instance store assertions, bookmarks/resumption across lifecycle boundaries. Use [`AppComponentTest`](../../test/component/Elsa.Workflows.ComponentTests/Helpers/Abstractions/AppComponentTest.cs) to instantiate and [`IWorkflowInstanceStore`](../../src/modules/Elsa.Workflows.Management/Contracts/IWorkflowInstanceStore.cs) queries for assertions.

Each test layer has distinct goals and clear boundaries — see [**Which parts of Elsa to test**](#which-parts-of-elsa-to-test-and-which-test-types-to-use) for precise mapping of which aspects belong to which layer.

---

## Quick Start for Contributors

**Before you writ e a test:**
1. ✅ Understand what you're testing (see [**Which parts of Elsa to test**](#which-parts-of-elsa-to-test-and-which-test-types-to-use))
2. ✅ Choose the right test layer (unit vs integration vs component)
3. ✅ Use existing helpers (don't reinvent - see [**Test Helpers Reference**](#test-helpers-reference-quick-lookup))

**5-Minute Checklist:**
- [ ] Read the relevant section below for your change type:
    - Changed activity logic or created a new activity? → See [Activities](#activities)
    - Changed workflow execution? → See [Workflows execution](#workflow-execution-invoker-middleware-bookmarks)
- [ ] Follow steps and code patterns in that section
- [ ] Run tests locally: `dotnet test`
- [ ] Verify no flaky behavior (run 10 times: `dotnet test --no-build -- repeat 10`)

---

## Characteristics for testing

- **Activities:** First-class pluggable units. Each activity implements execution logic and interacts with the [`ActivityExecutionContext`](../../src/modules/Elsa.Workflows.Core/Contexts/ActivityExecutionContext.cs). More details in [**Activities**](#activities).

- **Workflows:** Graphs of activities. A workflow can run synchronously or schedule asynchronous work (bookmarks, timers). When you run a workflow in-process with [`IWorkflowRunner.RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs), the runner will return when synchronous work completes. Some activities set `RunAsynchronously` causing background scheduling — tests need to take care when asserting. More details in [**Workflow execution**](#workflow-execution-invoker-middleware-bookmarks).

---

## Which parts of Elsa to test, and which test types to use

This section maps Elsa aspects to the exact kinds of tests you should write, with examples and code patterns referencing repository conventions.

### Activities

#### **Unit tests:**
- Test the activity class logic only (no persistence, no scheduler). Cover configuration permutations and boundary inputs.
- Use [`ActivityTestFixture`](../../test/unit/Elsa.Activities.UnitTests/Helpers/ActivityTestFixture.cs) to configure and execute the activity and obtain an [`ActivityExecutionContext`](../../src/modules/Elsa.Workflows.Core/Contexts/ActivityExecutionContext.cs) for assertions.
- In case the activity sets outputs, assert using `context.GetExecutionOutput()`([extension method](../../src/modules/Elsa.Workflows.Core/Extensions/ActivityExecutionContextExtensions.cs)), otherwise, use `context.Get()`.

**Example (does not set output):**
```csharp
[Fact]
public async Task Should_Set_Variable_Integer()
{
    // Arrange
    const int expected = 42;
    var variable = new Variable<int>("myVar", 0, "myVar");
    var setVariable = new SetVariable<int>(variable, new Input<int>(expected));

    // Act
    var fixture = new ActivityTestFixture(setVariable);
    var context = await fixture.ExecuteAsync();

    // Assert
    var result = context.Get(variable);
    Assert.Equal(expected, result);
}
```

**Example (sets output):**
```csharp
[Fact]
    public async Task Should_Send_Get_Request_And_Handle_Success_Response()
    {
        // Arrange
        // Extra arrange steps omitted for brevity
        var sendHttpRequest = new SendHttpRequest
        {
            Url = new Input<Uri?>(expectedUrl),
            Method = new Input<string>("GET"),
            ExpectedStatusCodes = new List<HttpStatusCodeCase>()
        };

        // Act
        var fixture = new ActivityTestFixture(sendHttpRequest)
            .WithHttpServices(responseHandler); // HTTP-specific extension
        var context = await fixture.ExecuteAsync();

        // Assert
        var statusCodeOutput = context.GetExecutionOutput(_ => sendHttpRequest.StatusCode);
        Assert.Equal(200, statusCodeOutput);
    }
```

#### **Integration tests:**
- Place the activity inside a minimal workflow definition and run via [`IWorkflowRunner.RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs). Assert outputs/variables and that the activity integrates correctly with preceding/following activities.
- If activity creates bookmarks or relies on scheduler semantics, integration tests should resume bookmarks via the engine APIs to validate resumption.

Pattern note: [`RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) returns a [`RunWorkflowResult`](../../src/modules/Elsa.Workflows.Core/Models/RunWorkflowResult.cs) (or equivalent) containing the [`WorkflowInstance`](../../src/modules/Elsa.Workflows.Management/Entities/WorkflowInstance.cs) and output variables when run to completion. 
Use returned state for deterministic assertions where possible.


### Workflow execution (invoker, middleware, bookmarks)

#### Unit tests:
- Rare: low-level pure helpers in the invoker may have unit tests for edge cases. Most invoker behavior requires integration testing.

#### Integration tests:
- Use [`IWorkflowRunner.RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) with small workflows to test variables propagation, branch logic (If/ForEach/Parallel), expression evaluation, and `RunAsynchronously` flags.
- When a workflow schedules async child work (bookmarks), simulate resumption by calling resume APIs.


Call the workflow runner to execute a workflow object or a loaded definition. Prefer this when asserting logical flow and outputs.

```csharp
var runner = serviceProvider.GetRequiredService<IWorkflowRunner>();
await serviceProvider.PopulateRegistriesAsync();
var runResult = await runner.RunAsync(workflow);
Assert.Equal(WorkflowStatus.Finished, runResult.WorkflowInstance!.Status);
```

#### Component tests (persistence & resumption):
- Start a workflow that creates a bookmark. Persisted instance must be found via [`IWorkflowInstanceStore`](../../src/modules/Elsa.Workflows.Management/Contracts/IWorkflowInstanceStore.cs) after the creation point. Simulate host restart by disposing and rebuilding the service provider (keeping the same persistence store) and resume the bookmark to assert resumption completes.

**Code pattern to resume a bookmark (integration/component):**

```csharp
// assume instanceId found via RunAsync or correlation id
await workflowTriggerService.ResumeAsync(instanceId, activityId, input, CancellationToken.None);
var resumed = await runner.RunAsync(workflowInstance);
Assert.Equal(WorkflowStatus.Finished, resumed.WorkflowInstance.Status);
```

---

## Test Helpers Reference (Quick Lookup)

| Helper                           | Purpose                                    | Use When                                                                |
|----------------------------------|--------------------------------------------|-------------------------------------------------------------------------|
| `TestApplicationBuilder`         | Build test service provider                | All tests as entry point, <br/>except activities unit tests (see below) |
| `ActivityTestFixture`            | Configure and run single activity          | Unit testing activities                                                 |
| `context.GetExecutionOutput`     | Get output from isolated context           | Asserting activity outputs in unit tests                                |
| `IWorkflowRunner.RunAsync`       | Execute workflow in-process                | Integration / Component tests                                           |
| `PopulateRegistriesAsync`        | Register types for JSON deserialization.   | Loading JSON workflows. <br/>Integration tests only                     |
| `IWorkflowInstanceStore`         | Query persisted instances                  | Component tests (persistence)                                           |
| `RunWorkflowUntilEndAsync`       | Drive workflow to completion.              | Complex resumption scenarios. <br/>Integration tests only               |

---

## Decision helper (what to add — follow in order)

1. **Changed code is a single activity class with no persistence/external calls?** → Unit test only.
2. **Change touches multiple activities or workflow logic (If, ForEach, Parallel, Flow activities, etc.)?** → Integration test using [`IWorkflowRunner.RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) and a small workflow.
3. **Change touches invoker/scheduler/bookmarks or similar multi-component feature?** → Integration test using [`IWorkflowRunner.RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) and a small workflow. If persistence semantics change, add component tests.
4. **Change touches persistence/serializers or requires durable evidence (journal, bookmarks)?** → Component tests against [`IWorkflowInstanceStore`](../../src/modules/Elsa.Workflows.Management/Contracts/IWorkflowInstanceStore.cs).


**Rule of thumb:**
- If it’s about **internal logic**, write a **unit** test.
- If it’s about **collaboration between components**, write an **integration** test.
- If it’s about **end-to-end feature behavior**, write a **component** test


When in doubt, add the minimal unit tests plus one integration test that reproduces the scenario.

---

## Deterministic patterns to avoid flaky tests

1. **For activity unit tests, prefer returned state from [`ActivityTestFixture.ExecuteAsync`](../../test/unit/Elsa.Activities.UnitTests/Helpers/ActivityTestFixture.cs).** Always inspect on the returned context — it is deterministic for synchronous workflows.
2. **Resume bookmarks explicitly.** Do not wait for external schedulers — call the engine's resume/trigger APIs in your test to continue execution.
3. **For integration tests, locate instances deterministically.** Use an instance id returned by [`RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) or attach a `CorrelationId` test variable and query [`IWorkflowInstanceStore.FindByCorrelationIdAsync(...)`](../../src/modules/Elsa.Workflows.Management/Contracts/IWorkflowInstanceStore.cs). Avoid using "latest" queries.

---

## Failure testing (faults & incidents)
- **Integration test faulted workflows**: build a workflow that throws and run via [`RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) — assert [`WorkflowInstance.Status`](../../src/modules/Elsa.Workflows.Management/Entities/WorkflowInstance.cs) == [`Faulted`](../../src/modules/Elsa.Workflows.Core/Enums/WorkflowStatus.cs) on the returned state or via [`IWorkflowInstanceStore`](../../src/modules/Elsa.Workflows.Management/Contracts/IWorkflowInstanceStore.cs).
- **Component tests for recovery/resume**: persist a faulted instance (or cause a host restart scenario), run your recovery logic, and assert the final state.

---

## Practical test recipes & snippets (copy/paste-ready)

### Unit test (activity) — pattern

```csharp
[Fact]
public async Task MyActivity_Test()
{
    var activity = new ActivityToTest();

    // Act
    var fixture = new ActivityTestFixture(activity);
    var context = await fixture.ExecuteAsync();

    // assert behavior of activity in isolation
}
```

### Integration test — pattern using [`IWorkflowRunner.RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs)

```csharp
[Fact]
public async Task Workflow_With_MyActivity_Completes()
{
    var sp = new TestApplicationBuilder(testOutput).Build();
    await sp.PopulateRegistriesAsync();

    var runner = sp.GetRequiredService<IWorkflowRunner>();
    var workflow = new MyWorkflowDefinition();

    var result = await runner.RunAsync(workflow);

    Assert.Equal(WorkflowStatus.Finished, result.WorkflowInstance!.Status);
}
```

### Component test

```csharp
[Fact]
public async Task Workflow_Persists_Instance_And_Journal()
{
    var sp = new TestApplicationBuilder(testOutput)
        .Build();

    var runner = sp.GetRequiredService<AsyncWorkflowRunner>();
var result = await runner.RunAndAwaitWorkflowCompletionAsync(WorkflowDefinitionHandle.ByDefinitionId(someDefinitionId, VersionOptions.Published));
    result.WorkflowExecutionContext.Status.Should().Be(WorkflowStatus.Finished);
}
```
---

## FAQ (quick pointers)

**Q: How do I import workflow definitions in tests and where do I put them?**

A: For JSON-defined workflows use the repo's test integration helpers ([`PopulateRegistriesAsync()`](../../src/common/Elsa.Testing.Shared.Integration/ServiceProviderExtensions.cs) or the test registration helpers in `test/common`). 
See integration test examples in the test tree. 
Leave the definitions next to the tests that use them.

**Q: Which helper should I use to run a workflow?**

A: Prefer [`IWorkflowRunner.RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) for in-process deterministic runs. For activities use [`ActivityTestFixture`](../../test/unit/Elsa.Activities.UnitTests/Helpers/ActivityTestFixture.cs).

**Q: How do I check persisted journal entries?**

A: Query [`IWorkflowInstanceStore`](../../src/modules/Elsa.Workflows.Management/Contracts/IWorkflowInstanceStore.cs) and inspect the persisted journal on the instance. Use deterministic instance id or correlation id to locate the exact instance.

**Q: Do I need a new helper to wait for workflow completion?**

A: No. The repo provides [`RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) for integration tests and [`ActivityTestFixture`](../../test/unit/Elsa.Activities.UnitTests/Helpers/ActivityTestFixture.cs) for activity unit tests, as well as integration helpers that cover all necessary scenarios.

---

## Appendix — examples in the repository (where to look)

Search the `test/` tree for examples that follow the above patterns:

- Unit test activity examples: `test/unit/Elsa.Activities.UnitTests` (look for [`ActivityTestFixture`](../../test/unit/Elsa.Activities.UnitTests/Helpers/ActivityTestFixture.cs) usage).
- Integration workflow examples: `test/integration/Elsa.*.IntegrationTests` (look for [`PopulateRegistriesAsync()`](../../src/common/Elsa.Testing.Shared.Integration/ServiceProviderExtensions.cs) and [`IWorkflowRunner.RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) usage).
- Component scenarios exercising persistence: `test/component/Elsa.Workflows.ComponentTests (look for [`AppComponentTest`](../../test/component/Elsa.Workflows.ComponentTests/Helpers/Abstractions/AppComponentTest.cs) scaffolds and [`IWorkflowInstanceStore`](../../src/modules/Elsa.Workflows.Management/Contracts/IWorkflowInstanceStore.cs) assertions).




