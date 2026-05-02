using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Extensions;

namespace Elsa.Activities.UnitTests.Flow;

/// <summary>
/// Unit tests for the <see cref="Fork"/> activity.
/// </summary>
public class ForkTests
{
    [Fact(DisplayName = "Fork schedules all branches")]
    public async Task Fork_SchedulesAllBranches()
    {
        // Arrange
        var branches = CreateBranches(3);
        var fork = new Fork
        {
            JoinMode = ForkJoinMode.WaitAll,
            Branches = branches.Cast<IActivity>().ToList()
        };

        // Act
        var context = await ExecuteForkAsync(fork);

        // Assert
        foreach (var branch in branches)
        {
            Assert.True(context.HasScheduledActivity(branch), $"{branch.Id} should be scheduled");
        }
    }

    [Fact(DisplayName = "Fork with no branches completes immediately")]
    public async Task Fork_WithNoBranchesCompletesImmediately()
    {
        // Arrange
        var fork = new Fork { JoinMode = ForkJoinMode.WaitAll };

        // Act
        var context = await ExecuteForkAsync(fork);

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
    }

    [Theory(DisplayName = "Fork respects join mode")]
    [InlineData(ForkJoinMode.WaitAll)]
    [InlineData(ForkJoinMode.WaitAny)]
    public async Task Fork_RespectsJoinMode(ForkJoinMode joinMode)
    {
        // Arrange
        var branch = new WriteLine("Test Branch");
        var fork = new Fork
        {
            JoinMode = joinMode,
            Branches = { branch }
        };

        // Act
        var context = await ExecuteForkAsync(fork);

        // Assert
        Assert.Equal(joinMode, fork.JoinMode);
        Assert.True(context.HasScheduledActivity(branch));
    }

    [Fact(DisplayName = "Fork resumes when completed activity IDs are restored as a list")]
    public async Task Fork_Resumes_WhenCompletedActivityIdsAreRestoredAsList()
    {
        // Arrange
        var branches = CreateBranches(2);
        var fork = new Fork
        {
            JoinMode = ForkJoinMode.WaitAll,
            Branches = branches.Cast<IActivity>().ToList()
        };
        var fixture = new ActivityTestFixture(fork);
        var targetContext = await fixture.BuildAsync();
        await fixture.ExecuteAsync(targetContext);
        targetContext.SetProperty("Completed", new List<string> { branches[0].Id });
        var childContext = await targetContext.WorkflowExecutionContext.CreateActivityExecutionContextAsync(branches[1]);

        // Act
        await CompleteChildAsync(fork, targetContext, childContext);

        // Assert
        Assert.Equal(ActivityStatus.Completed, targetContext.Status);
        var completedActivityIds = targetContext.GetProperty<HashSet<string>>("Completed");
        Assert.NotNull(completedActivityIds);
        Assert.True(completedActivityIds.SetEquals([branches[0].Id, branches[1].Id]));
    }

    private static Task<ActivityExecutionContext> ExecuteForkAsync(Fork fork) =>
        new ActivityTestFixture(fork).ExecuteAsync();

    private static async Task CompleteChildAsync(Fork fork, ActivityExecutionContext targetContext, ActivityExecutionContext childContext)
    {
        var childNode = targetContext.WorkflowExecutionContext.FindNodeByActivity(childContext.Activity);
        Assert.NotNull(childNode);

        targetContext.WorkflowExecutionContext.AddCompletionCallback(targetContext, childNode!, fork.GetActivityCompletionCallback("CompleteChildAsync"));
        var callback = targetContext.WorkflowExecutionContext.PopCompletionCallback(targetContext, childNode!);

        Assert.NotNull(callback?.CompletionCallback);
        await callback!.CompletionCallback!(new ActivityCompletedContext(targetContext, childContext));
    }

    private static WriteLine[] CreateBranches(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new WriteLine($"Branch {i}") { Id = $"branch-{i}" })
            .ToArray();
}
