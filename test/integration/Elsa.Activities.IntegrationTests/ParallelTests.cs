using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Xunit.Abstractions;
using Parallel = Elsa.Workflows.Activities.Parallel;

namespace Elsa.Activities.IntegrationTests;

public class ParallelTests
{
    private readonly CapturingTextWriter _capturingTextWriter = new();
    private readonly IServiceProvider _serviceProvider;

    public ParallelTests(ITestOutputHelper testOutputHelper)
    {
        _serviceProvider = new TestApplicationBuilder(testOutputHelper).WithCapturingTextWriter(_capturingTextWriter).Build();
    }

    [Fact(DisplayName = "Parallel executes all child activities and completes")]
    public async Task Parallel_ExecutesAllChildren_AndCompletes()
    {
        // Arrange
        var parallel = new Parallel(
            new WriteLine("Activity 1"),
            new WriteLine("Activity 2"),
            new WriteLine("Activity 3")
        );

        // Act
        var result = await _serviceProvider.RunActivityAsync(parallel);

        // Assert
        var journal = result.Journal;
        var parallelContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity is Parallel);

        Assert.NotNull(parallelContext);
        Assert.Equal(ActivityStatus.Completed, parallelContext.Status);
        Assert.Equal(3, _capturingTextWriter.Lines.Count);
        Assert.Contains("Activity 1", _capturingTextWriter.Lines);
        Assert.Contains("Activity 2", _capturingTextWriter.Lines);
        Assert.Contains("Activity 3", _capturingTextWriter.Lines);
    }

    [Fact(DisplayName = "Parallel completes when empty")]
    public async Task Parallel_Completes_WhenEmpty()
    {
        // Arrange
        var parallel = new Parallel();

        // Act
        var result = await _serviceProvider.RunActivityAsync(parallel);

        // Assert
        var journal = result.Journal;
        var parallelContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity is Parallel);

        Assert.NotNull(parallelContext);
        Assert.Equal(ActivityStatus.Completed, parallelContext.Status);
        Assert.Empty(_capturingTextWriter.Lines);
    }

    [Fact(DisplayName = "Parallel executes single child activity and completes")]
    public async Task Parallel_ExecutesSingleChild_AndCompletes()
    {
        // Arrange
        var parallel = new Parallel(
            new WriteLine("Single Activity")
        );

        // Act
        var result = await _serviceProvider.RunActivityAsync(parallel);

        // Assert
        var journal = result.Journal;
        var parallelContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity is Parallel);

        Assert.NotNull(parallelContext);
        Assert.Equal(ActivityStatus.Completed, parallelContext.Status);
        Assert.Single(_capturingTextWriter.Lines);
        Assert.Equal("Single Activity", _capturingTextWriter.Lines.Single());
    }

    [Fact(DisplayName = "Parallel executes multiple different activity types")]
    public async Task Parallel_ExecutesMixedActivityTypes_AndCompletes()
    {
        // Arrange
        var parallel = new Parallel(
            new WriteLine("First"),
            new SetName("TestName"),
            new WriteLine("Second")
        );

        // Act
        var result = await _serviceProvider.RunActivityAsync(parallel);

        // Assert
        var journal = result.Journal;
        var parallelContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity is Parallel);

        Assert.NotNull(parallelContext);
        Assert.Equal(ActivityStatus.Completed, parallelContext.Status);
        Assert.Equal(2, _capturingTextWriter.Lines.Count);
        Assert.Contains("First", _capturingTextWriter.Lines);
        Assert.Contains("Second", _capturingTextWriter.Lines);
    }

    [Fact(DisplayName = "Parallel completes only after all children complete")]
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
        var result = await _serviceProvider.RunActivityAsync(parallel);

        // Assert
        var journal = result.Journal;
        var parallelContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity is Parallel);

        Assert.NotNull(parallelContext);
        Assert.Equal(ActivityStatus.Completed, parallelContext.Status);
        Assert.Equal(4, _capturingTextWriter.Lines.Count);
    }

    [Fact(DisplayName = "Parallel executes nested Parallel activities")]
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
        var result = await _serviceProvider.RunActivityAsync(outerParallel);

        // Assert
        var journal = result.Journal;
        var outerParallelContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity == outerParallel);
        var innerParallelContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity == innerParallel);

        Assert.NotNull(outerParallelContext);
        Assert.NotNull(innerParallelContext);
        Assert.Equal(ActivityStatus.Completed, outerParallelContext.Status);
        Assert.Equal(ActivityStatus.Completed, innerParallelContext.Status);
        Assert.Equal(4, _capturingTextWriter.Lines.Count);
        Assert.Contains("Outer 1", _capturingTextWriter.Lines);
        Assert.Contains("Outer 2", _capturingTextWriter.Lines);
        Assert.Contains("Inner 1", _capturingTextWriter.Lines);
        Assert.Contains("Inner 2", _capturingTextWriter.Lines);
    }

    [Fact(DisplayName = "Parallel remains in Running state when a child activity faults")]
    public async Task Parallel_RemainsRunning_WhenChildFaults()
    {
        // Arrange
        var parallel = new Parallel(
            new WriteLine("Before Fault"),
            new Fault { Message = new("Test fault") },
            new WriteLine("After Fault")
        );

        // Act
        var result = await _serviceProvider.RunActivityAsync(parallel);

        // Assert
        var journal = result.Journal;
        var parallelContext = journal.ActivityExecutionContexts.FirstOrDefault(x => x.Activity is Parallel);

        Assert.NotNull(parallelContext);
        Assert.Equal(ActivityStatus.Running, parallelContext.Status);
        Assert.Equal(1, parallelContext.AggregateFaultCount);
        // The non-faulted activities should still execute
        Assert.Contains("Before Fault", _capturingTextWriter.Lines);
        Assert.Contains("After Fault", _capturingTextWriter.Lines);
    }
}
