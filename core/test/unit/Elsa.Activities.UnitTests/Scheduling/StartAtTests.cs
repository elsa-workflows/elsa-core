using Elsa.Common;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Bookmarks;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Scheduling;

public class StartAtTests
{
    [Fact]
    public async Task WhenIsTrigger_CompletesImmediately()
    {
        // Arrange
        var futureTime = DateTimeOffset.UtcNow.AddHours(1);
        var activity = new StartAt(futureTime);

        // Act
        var context = await ExecuteAsTriggerAsync(activity);

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.Empty(context.WorkflowExecutionContext.Bookmarks);
    }

    [Theory]
    [InlineData(-1, false)]  // Past - completes immediately
    [InlineData(0, false)]   // Now - completes immediately
    [InlineData(1, true)]    // Future - creates bookmark
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
            Assert.Equal(ActivityStatus.Running, context.Status);
            var payload = GetStartAtPayload(context);
            Assert.Equal(executeAt, payload.ExecuteAt);
        }
        else
        {
            Assert.Equal(ActivityStatus.Completed, context.Status);
            Assert.Empty(context.WorkflowExecutionContext.Bookmarks);
        }
    }

    [Fact]
    public async Task RecordsExecutedTimeInJournal()
    {
        // Arrange
        var now = new DateTimeOffset(2025, 1, 6, 12, 0, 0, TimeSpan.Zero);
        var futureTime = now.AddHours(1);
        var activity = new StartAt(futureTime);

        // Act
        var context = await ExecuteAsync(activity, now);

        // Assert
        Assert.True(context.JournalData.ContainsKey("Executed At"));
        Assert.Equal(now, context.JournalData["Executed At"]);
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

    private static StartAtPayload GetStartAtPayload(ActivityExecutionContext context)
    {
        var bookmark = Assert.Single(context.WorkflowExecutionContext.Bookmarks);
        return Assert.IsType<StartAtPayload>(bookmark.Payload);
    }
}
