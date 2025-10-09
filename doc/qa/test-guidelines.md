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

## High-level testing pyramid

- **Unit tests** — single-class logic (activities, converters, expression evaluators, serializers, service providers). Fast; no persistence.
- **Integration tests** — multiple Elsa subsystems together (invoker + activities + registries). In-process; may deserialize workflow JSON. Use [`IWorkflowRunner.RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) and [`PopulateRegistriesAsync()`](../../src/common/Elsa.Testing.Shared.Integration/ServiceProviderExtensions.cs) when using existing definitions.
- **Component tests** — persisted behaviour, journal/instance store assertions, bookmarks/resumption across lifecycle boundaries. Use [`AppComponentTest`](../../test/component/Elsa.Workflows.ComponentTests/Helpers/Abstractions/AppComponentTest.cs) to instantiate and [`IWorkflowInstanceStore`](../../src/modules/Elsa.Workflows.Management/Contracts/IWorkflowInstanceStore.cs) queries for assertions.

Each test layer has distinct goals and clear boundaries — see [**Which parts of Elsa to test**](#which-parts-of-elsa-to-test-and-which-test-types-to-use) for precise mapping of which aspects belong to which layer.

---

## Quick Start for Contributors

**Before you write a test:**
1. ✅ Understand what you're testing (see [**Which parts of Elsa to test**](#which-parts-of-elsa-to-test-and-which-test-types-to-use))
2. ✅ Choose the right test layer (unit vs integration vs component)
3. ✅ Use existing helpers (don't reinvent - see [**Test Helpers Reference**](#test-helpers-reference-quick-lookup))

**5-Minute Checklist:**
- [ ] Read the relevant section below for your change type:
    - Changed activity logic? → See [Activities](#activities)
    - Changed workflow execution? → See [Workflows execution](#workflow-execution-invoker-middleware-bookmarks)
    - Changed persistence? → See [Persistence & serialization](#persistence--serialization)
- [ ] Follow steps and code patterns in that section
- [ ] Run tests locally: `dotnet test`
- [ ] Verify no flaky behavior (run 10 times: `dotnet test --no-build -- repeat 10`)

---

## Characteristics for testing

- **Activities:** First-class pluggable units. Each activity implements execution logic and interacts with the [`ActivityExecutionContext`](../../src/modules/Elsa.Workflows.Core/Contexts/ActivityExecutionContext.cs). Many activity tests in the repository use [`RunActivityAsync`](../../src/common/Elsa.Testing.Shared.Integration/RunActivityExtensions.cs) to create the required context and invoke the activity inline.

- **Workflows:** Graphs of activities. A workflow can run synchronously or schedule asynchronous work (bookmarks, timers). When you run a workflow in-process with [`IWorkflowRunner.RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs), the runner will return when synchronous work completes. Some activities set `RunAsynchronously` causing background scheduling — tests need to take care when asserting.

---

## Which parts of Elsa to test, and which test types to use

This section maps Elsa aspects to the exact kinds of tests you should write, with examples and code patterns referencing repository conventions.

### Activities

#### **Unit tests:**
- Test the activity class logic only (no persistence, no scheduler). Cover configuration permutations and boundary inputs.
- Use [`TestApplicationBuilder`](../../src/common/Elsa.Testing.Shared.Integration/TestApplicationBuilder.cs) + [`RunActivityAsync`](../../src/common/Elsa.Testing.Shared.Integration/RunActivityExtensions.cs) to obtain an [`ActivityExecutionContext`](../../src/modules/Elsa.Workflows.Core/Contexts/ActivityExecutionContext.cs) and run the activity.

**Example:**
```csharp
// Arrange
var serviceProvider = new TestApplicationBuilder(testOutputHelper)
    .WithCapturingTextWriter(capturingTextWriter)
    .Build();

// Act
var writeLine = new WriteLine("Hello world!");
await serviceProvider.RunActivityAsync(writeLine);

// Assert
Assert.Equal("Hello world!", capturingTextWriter.Lines.Single());
```

#### **Integration tests (recommended if activity participates in workflows):**
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

### Persistence & Serialization

#### Integration tests:**
- Import a JSON workflow definition via the same serializers used by the engine (the test helper [`PopulateRegistriesAsync()`](../../src/common/Elsa.Testing.Shared.Integration/ServiceProviderExtensions.cs) demonstrates this pattern). Run the workflow through [`IWorkflowRunner`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) to validate deserialization + execution.
---

## Test Helpers Reference (Quick Lookup)

| Helper | Purpose | Use When |
|--------|---------|----------|
| `TestApplicationBuilder` | Build test service provider | All tests (entry point) |
| `RunActivityAsync` | Run single activity | Unit testing activities |
| `IWorkflowRunner.RunAsync` | Execute workflow in-process | Integration tests |
| `PopulateRegistriesAsync` | Register types for JSON deserialization | Loading JSON workflows |
| `IWorkflowInstanceStore` | Query persisted instances | Component tests (persistence) |
| `RunWorkflowUntilEndAsync` | Drive workflow to completion | Complex resumption scenarios |

---

## Decision helper (what to add — follow in order)

1. **Changed code is a single activity class with no persistence/external calls?** → Unit test only.
2. **Change touches invoker/scheduler/bookmarks or workflow composition?** → Integration test using [`IWorkflowRunner.RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) and a small workflow. If persistence semantics change, add component tests.
3. **Change touches persistence/serializers or requires durable evidence (journal, bookmarks)?** → Component tests against [`IWorkflowInstanceStore`](../../src/modules/Elsa.Workflows.Management/Contracts/IWorkflowInstanceStore.cs).

When in doubt, add the minimal unit tests plus one integration test that reproduces the scenario.

---

## Deterministic patterns to avoid flaky tests

1. **Prefer returned state from [`RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs).** Always inspect [`RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) results first — it is deterministic for synchronous workflows.
2. **Resume bookmarks explicitly.** Do not wait for external schedulers — call the engine's resume/trigger APIs in your test to continue execution.
3. **Locate instances deterministically.** Use an instance id returned by [`RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) or attach a `CorrelationId` test variable and query [`IWorkflowInstanceStore.FindByCorrelationIdAsync(...)`](../../src/modules/Elsa.Workflows.Management/Contracts/IWorkflowInstanceStore.cs). Avoid using "latest" queries.
4. **Use short polling where necessary.** If you must poll the instance store (e.g., testing asynchronous controllers), use a short interval and a deterministic timeout (helper code snippets in examples above).

---

## Failure testing (faults & incidents)

- **Unit test [`Fault`](../../src/modules/Elsa.Workflows.Core/Activities/Fault.cs) activity**: instantiate the [`Fault`](../../src/modules/Elsa.Workflows.Core/Activities/Fault.cs) activity class and assert the expected exception/behavior.
- **Integration test faulted workflows**: build a workflow that throws and run via [`RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) — assert [`WorkflowInstance.Status`](../../src/modules/Elsa.Workflows.Management/Entities/WorkflowInstance.cs) == [`Faulted`](../../src/modules/Elsa.Workflows.Core/Enums/WorkflowStatus.cs) on the returned state or via [`IWorkflowInstanceStore`](../../src/modules/Elsa.Workflows.Management/Contracts/IWorkflowInstanceStore.cs).
- **Component tests for recovery/resume**: persist a faulted instance (or cause a host restart scenario), run your recovery logic, and assert the final state.

**Tip:** tests that simulate host restart should recreate the service provider but reuse the same persistence store instance (in-memory DB configured at the test scope or repo test fixtures). This proves the engine resumes from persisted state.

---

## Practical test recipes & snippets (copy/paste-ready)

### Unit test (activity) — pattern

```csharp
[Fact]
public async Task MyActivity_WritesExpectedOutput()
{
    var sp = new TestApplicationBuilder(testOutput).Build();
    var activity = new MyActivity { Input = "x" };

    await sp.RunActivityAsync(activity);

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

### Component test — pattern asserting persisted state

```csharp
[Fact]
public async Task Workflow_Persists_Instance_And_Journal()
{
    var sp = new TestApplicationBuilder(testOutput)
        .UseRealPersistenceForTests()
        .Build();

    var runner = sp.GetRequiredService<IWorkflowRunner>();
    var store = sp.GetRequiredService<IWorkflowInstanceStore>();

    var result = await runner.RunAsync(workflow);
    var instanceId = result.WorkflowInstance!.Id;

    // deterministic lookup: query store until terminal state
    var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
    WorkflowInstance? instance = null;
    while (DateTime.UtcNow < deadline)
    {
        instance = await store.FindByIdAsync(instanceId);
        if (instance is not null && instance.Status is WorkflowStatus.Finished or WorkflowStatus.Faulted)
            break;
        await Task.Delay(150);
    }

    instance.Should().NotBeNull();
    instance!.Status.Should().Be(WorkflowStatus.Finished);
}
```
---

## FAQ (quick pointers)

**Q: How do I import workflow definitions in tests?**
A: For JSON-defined workflows use the repo's test integration helpers ([`PopulateRegistriesAsync()`](../../src/common/Elsa.Testing.Shared.Integration/ServiceProviderExtensions.cs) or the test registration helpers in `test/common`). See integration test examples in the test tree.

**Q: Which helper should I use to run a workflow?**
A: Prefer [`IWorkflowRunner.RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) for in-process deterministic runs. For activities use [`RunActivityAsync`](../../src/common/Elsa.Testing.Shared.Integration/RunActivityExtensions.cs) via [`TestApplicationBuilder`](../../src/common/Elsa.Testing.Shared.Integration/TestApplicationBuilder.cs).

**Q: How do I check persisted journal entries?**
A: Query [`IWorkflowInstanceStore`](../../src/modules/Elsa.Workflows.Management/Contracts/IWorkflowInstanceStore.cs) and inspect the persisted journal on the instance. Use deterministic instance id or correlation id to locate the exact instance.

**Q: Do I need a new helper to wait for workflow completion?**
A: Not yet — the repo provides [`RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) and integration helpers that cover most scenarios. If you find many duplicated poll loops, open an issue requesting a canonical `WaitForCompletion` helper in `test/shared`.

---

## Appendix — examples in the repository (where to look)

Search the `test/` tree for examples that follow the above patterns:

- Unit activity examples: `test/unit/*` (look for [`RunActivityAsync`](../../src/common/Elsa.Testing.Shared.Integration/RunActivityExtensions.cs) usage).
- Integration workflow examples: `test/integration/*` (look for [`PopulateRegistriesAsync()`](../../src/common/Elsa.Testing.Shared.Integration/ServiceProviderExtensions.cs) and [`IWorkflowRunner.RunAsync`](../../src/modules/Elsa.Workflows.Core/Contracts/IWorkflowRunner.cs) usage).
- Component scenarios exercising persistence: `test/component/*` (look for [`AppComponentTest`](../../test/component/Elsa.Workflows.ComponentTests/Helpers/Abstractions/AppComponentTest.cs) scaffolds and [`IWorkflowInstanceStore`](../../src/modules/Elsa.Workflows.Management/Contracts/IWorkflowInstanceStore.cs) assertions).




