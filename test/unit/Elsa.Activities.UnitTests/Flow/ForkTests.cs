using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Extensions;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Flow;

/// <summary>
/// Unit tests for the <see cref="Fork"/> activity.
/// </summary>
public class ForkTests
{
    [Test]
    [DisplayName("Fork schedules all branches")]
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
            await Assert.That(context.HasScheduledActivity(branch)).IsTrue().Because($"{branch.Id} should be scheduled");
        }
    }

    [Test]
    [DisplayName("Fork with no branches completes immediately")]
    public async Task Fork_WithNoBranchesCompletesImmediately()
    {
        // Arrange
        var fork = new Fork { JoinMode = ForkJoinMode.WaitAll };

        // Act
        var context = await ExecuteForkAsync(fork);

        // Assert
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Completed);
    }

    [Test]
    [DisplayName("Fork respects join mode")]
    [Arguments(ForkJoinMode.WaitAll)]
    [Arguments(ForkJoinMode.WaitAny)]
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
        await Assert.That(fork.JoinMode).IsEqualTo(joinMode);
        await Assert.That(context.HasScheduledActivity(branch)).IsTrue();
    }

    [Test]
    [DisplayName("Fork resumes when completed activity IDs are restored as a list")]
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
        await Assert.That(targetContext.Status).IsEqualTo(ActivityStatus.Completed);
        var completedActivityIds = targetContext.GetProperty<HashSet<string>>("Completed");
        await Assert.That(completedActivityIds).IsNotNull();
        await Assert.That(completedActivityIds.SetEquals([branches[0].Id, branches[1].Id])).IsTrue();
    }

    private static Task<ActivityExecutionContext> ExecuteForkAsync(Fork fork) =>
        new ActivityTestFixture(fork).ExecuteAsync();

    private static async Task CompleteChildAsync(Fork fork, ActivityExecutionContext targetContext, ActivityExecutionContext childContext)
    {
        var childNode = targetContext.WorkflowExecutionContext.FindNodeByActivity(childContext.Activity);
        await Assert.That(childNode).IsNotNull();

        targetContext.WorkflowExecutionContext.AddCompletionCallback(targetContext, childNode!, fork.GetActivityCompletionCallback("CompleteChildAsync"));
        var callback = targetContext.WorkflowExecutionContext.PopCompletionCallback(targetContext, childNode!);

        await Assert.That(callback?.CompletionCallback is not null).IsTrue();
        await callback!.CompletionCallback!(new ActivityCompletedContext(targetContext, childContext));
    }

    private static WriteLine[] CreateBranches(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new WriteLine($"Branch {i}") { Id = $"branch-{i}" })
            .ToArray();
}
