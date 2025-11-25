using Elsa.Testing.Shared;
using Elsa.Workflows;

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

    private static Task<ActivityExecutionContext> ExecuteForkAsync(Fork fork) =>
        new ActivityTestFixture(fork).ExecuteAsync();

    private static WriteLine[] CreateBranches(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new WriteLine($"Branch {i}") { Id = $"branch-{i}" })
            .ToArray();
}
