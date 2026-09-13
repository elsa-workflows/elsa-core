using Elsa.Common;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Bookmarks;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Scheduling;

public class StartAtTests
{
    [Test]
    public async Task WhenIsTrigger_CompletesImmediately()
    {
        // Arrange
        var futureTime = DateTimeOffset.UtcNow.AddHours(1);
        var activity = new StartAt(futureTime);

        // Act
        var context = await ExecuteAsTriggerAsync(activity);

        // Assert
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Completed);
        await Assert.That(context.WorkflowExecutionContext.Bookmarks).IsEmpty();
    }

    [Test]
    [Arguments(-1, false)]  // Past - completes immediately
    [Arguments(0, false)]   // Now - completes immediately
    [Arguments(1, true)]    // Future - creates bookmark
    public async Task ExecuteBehavior_DependsOnTimeRelativeToNow(int hoursOffset, bool shouldCreateBookmark)
    {
        // Arrange
        var now = new DateTimeOffset(2025, 1, 6, 12, 0, 0, TimeSpan.Zero);
        var executeAt = now.AddHours(hoursOffset);
        var activity = new StartAt(executeAt);

        // Act
        var context = await ExecuteAsync(activity, now);

        // Assert
        if (shouldCreateBookmark)
        {
            await Assert.That(context.Status).IsEqualTo(ActivityStatus.Running);
            var payload = await GetStartAtPayload(context);
            await Assert.That(payload.ExecuteAt).IsEqualTo(executeAt);
        }
        else
        {
            await Assert.That(context.Status).IsEqualTo(ActivityStatus.Completed);
            await Assert.That(context.WorkflowExecutionContext.Bookmarks).IsEmpty();
        }
    }

    [Test]
    public async Task RecordsExecutedTimeInJournal()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 1, 6, 12, 0, 0, TimeSpan.Zero);
        var futureTime = now.AddHours(1);
        var activity = new StartAt(futureTime);

        // Act
        var context = await ExecuteAsync(activity, now);

        // Assert
        await Assert.That(context.JournalData.ContainsKey("Executed At")).IsTrue();
        await Assert.That(context.JournalData["Executed At"]).IsEqualTo(now);
    }

    private static async Task<ActivityExecutionContext> ExecuteAsync(StartAt activity, DateTimeOffset? clockTime = null)
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(clockTime ?? DateTimeOffset.UtcNow);

        return await new ActivityTestFixture(activity)
            .ConfigureServices(services => services.AddSingleton(clock))
            .ExecuteAsync();
    }

    private static async Task<ActivityExecutionContext> ExecuteAsTriggerAsync(StartAt activity)
    {
        return await new ActivityTestFixture(activity)
            .ConfigureContext(ctx => ctx.WorkflowExecutionContext.TriggerActivityId = ctx.Activity.Id)
            .ExecuteAsync();
    }

    private static async Task<StartAtPayload> GetStartAtPayload(ActivityExecutionContext context)
    {
        var bookmark = await Assert.That(context.WorkflowExecutionContext.Bookmarks).HasSingleItem();
        await Assert.That(bookmark.Payload).IsOfType(typeof(StartAtPayload));
        return (StartAtPayload)bookmark.Payload!;
    }
}
