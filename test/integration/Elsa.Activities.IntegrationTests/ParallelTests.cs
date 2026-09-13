using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Parallel = Elsa.Workflows.Activities.Parallel;

namespace Elsa.Activities.IntegrationTests;

public class ParallelTests : IAsyncDisposable
{
    private readonly WorkflowTestFixture _fixture = new(TestContext.Current!.Output.StandardOutput);

    public ValueTask DisposeAsync() => _fixture.DisposeAsync();

    [Test]
    [DisplayName("Parallel executes all child activities and completes")]
    public async Task Parallel_ExecutesAllChildren_AndCompletes()
    {
        // Arrange
        var parallel = new Parallel(
            new WriteLine("Activity 1"),
            new WriteLine("Activity 2"),
            new WriteLine("Activity 3")
        );

        // Act
        var result = await _fixture.RunActivityAsync(parallel);

        // Assert
        var journal = result.Journal;
        var parallelContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity is Parallel);

        parallelContext = (await Assert.That(parallelContext).IsNotNull())!;

        await Assert.That(parallelContext.Status).IsEqualTo(ActivityStatus.Completed);

        await Assert.That(_fixture.CapturingTextWriter.Lines.Count).IsEqualTo(3);

        await Assert.That(_fixture.CapturingTextWriter.Lines).Contains("Activity 1");

        await Assert.That(_fixture.CapturingTextWriter.Lines).Contains("Activity 2");

        await Assert.That(_fixture.CapturingTextWriter.Lines).Contains("Activity 3");

    }

    [Test]
    [DisplayName("Parallel completes when empty")]
    public async Task Parallel_Completes_WhenEmpty()
    {
        // Arrange
        var parallel = new Parallel();

        // Act
        var result = await _fixture.RunActivityAsync(parallel);

        // Assert
        var journal = result.Journal;
        var parallelContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity is Parallel);

        parallelContext = (await Assert.That(parallelContext).IsNotNull())!;

        await Assert.That(parallelContext.Status).IsEqualTo(ActivityStatus.Completed);

        await Assert.That(_fixture.CapturingTextWriter.Lines).IsEmpty();

    }

    [Test]
    [DisplayName("Parallel executes single child activity and completes")]
    public async Task Parallel_ExecutesSingleChild_AndCompletes()
    {
        // Arrange
        var parallel = new Parallel(
            new WriteLine("Single Activity")
        );

        // Act
        var result = await _fixture.RunActivityAsync(parallel);

        // Assert
        var journal = result.Journal;
        var parallelContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity is Parallel);

        parallelContext = (await Assert.That(parallelContext).IsNotNull())!;

        await Assert.That(parallelContext.Status).IsEqualTo(ActivityStatus.Completed);

        await Assert.That(_fixture.CapturingTextWriter.Lines).HasSingleItem();

        await Assert.That(_fixture.CapturingTextWriter.Lines.Single()).IsEqualTo("Single Activity");

    }

    [Test]
    [DisplayName("Parallel executes multiple different activity types")]
    public async Task Parallel_ExecutesMixedActivityTypes_AndCompletes()
    {
        // Arrange
        var parallel = new Parallel(
            new WriteLine("First"),
            new SetName("TestName"),
            new WriteLine("Second")
        );

        // Act
        var result = await _fixture.RunActivityAsync(parallel);

        // Assert
        var journal = result.Journal;
        var parallelContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity is Parallel);

        parallelContext = (await Assert.That(parallelContext).IsNotNull())!;

        await Assert.That(parallelContext.Status).IsEqualTo(ActivityStatus.Completed);

        await Assert.That(_fixture.CapturingTextWriter.Lines.Count).IsEqualTo(2);

        await Assert.That(_fixture.CapturingTextWriter.Lines).Contains("First");

        await Assert.That(_fixture.CapturingTextWriter.Lines).Contains("Second");

    }

    [Test]
    [DisplayName("Parallel completes only after all children complete")]
    public async Task Parallel_CompletesOnlyAfterAllChildrenComplete()
    {
        // Arrange
        var parallel = new Parallel(
            new WriteLine("Child 1"),
            new WriteLine("Child 2"),
            new WriteLine("Child 3"),
            new WriteLine("Child 4")
        );

        // Act
        var result = await _fixture.RunActivityAsync(parallel);

        // Assert
        var journal = result.Journal;
        var parallelContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity is Parallel);

        parallelContext = (await Assert.That(parallelContext).IsNotNull())!;

        await Assert.That(parallelContext.Status).IsEqualTo(ActivityStatus.Completed);

        await Assert.That(_fixture.CapturingTextWriter.Lines.Count).IsEqualTo(4);

    }

    [Test]
    [DisplayName("Parallel executes nested Parallel activities")]
    public async Task Parallel_ExecutesNestedParallel_AndCompletes()
    {
        // Arrange
        var innerParallel = new Parallel(
            new WriteLine("Inner 1"),
            new WriteLine("Inner 2")
        );

        var outerParallel = new Parallel(
            new WriteLine("Outer 1"),
            innerParallel,
            new WriteLine("Outer 2")
        );

        // Act
        var result = await _fixture.RunActivityAsync(outerParallel);

        // Assert
        var journal = result.Journal;
        var outerParallelContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity == outerParallel);
        var innerParallelContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity == innerParallel);

        outerParallelContext = (await Assert.That(outerParallelContext).IsNotNull())!;

        innerParallelContext = (await Assert.That(innerParallelContext).IsNotNull())!;

        await Assert.That(outerParallelContext.Status).IsEqualTo(ActivityStatus.Completed);

        await Assert.That(innerParallelContext.Status).IsEqualTo(ActivityStatus.Completed);

        await Assert.That(_fixture.CapturingTextWriter.Lines.Count).IsEqualTo(4);

        await Assert.That(_fixture.CapturingTextWriter.Lines).Contains("Outer 1");

        await Assert.That(_fixture.CapturingTextWriter.Lines).Contains("Outer 2");

        await Assert.That(_fixture.CapturingTextWriter.Lines).Contains("Inner 1");

        await Assert.That(_fixture.CapturingTextWriter.Lines).Contains("Inner 2");

    }

    [Test]
    [DisplayName("Parallel remains in Running state when a child activity faults")]
    public async Task Parallel_RemainsRunning_WhenChildFaults()
    {
        // Arrange
        var parallel = new Parallel(
            new WriteLine("Before Fault"),
            new Fault { Message = new("Test fault") },
            new WriteLine("After Fault")
        );

        // Act
        var result = await _fixture.RunActivityAsync(parallel);

        // Assert
        var journal = result.Journal;
        var parallelContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity is Parallel);

        parallelContext = (await Assert.That(parallelContext).IsNotNull())!;

        await Assert.That(parallelContext.Status).IsEqualTo(ActivityStatus.Running);

        await Assert.That(parallelContext.AggregateFaultCount).IsEqualTo(1);

        // The non-faulted activities should still execute
        await Assert.That(_fixture.CapturingTextWriter.Lines).Contains("Before Fault");

        await Assert.That(_fixture.CapturingTextWriter.Lines).Contains("After Fault");

    }
}
