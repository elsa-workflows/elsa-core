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
    public async Task Should_Complete_Immediately_When_IsTrigger()
    {
        // Arrange
        var futureTime = DateTimeOffset.UtcNow.AddHours(1);
        var startAt = new StartAt(futureTime);

        // Act
        var context = await new ActivityTestFixture(startAt)
            .ConfigureContext(ctx => ctx.WorkflowExecutionContext.TriggerActivityId = ctx.Activity.Id)
            .ExecuteAsync();

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.Empty(context.WorkflowExecutionContext.Bookmarks);
    }

    [Theory]
    [InlineData(-1, false)]  // Past
    [InlineData(0, false)]   // Now
    [InlineData(1, true)]    // Future
    public async Task Should_Complete_Or_CreateBookmark_BasedOn_DateTime(int hoursOffset, bool shouldCreateBookmark)
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var executeAt = now.AddHours(hoursOffset);
        var startAt = new StartAt(executeAt);

        // Act
        var context = await ExecuteAsync(startAt, now);

        // Assert
        if (shouldCreateBookmark)
        {
            Assert.Equal(ActivityStatus.Running, context.Status);
            var bookmark = Assert.Single(context.WorkflowExecutionContext.Bookmarks);
            var payload = Assert.IsType<StartAtPayload>(bookmark.Payload);
            Assert.Equal(executeAt, payload.ExecuteAt);
        }
        else
        {
            Assert.Equal(ActivityStatus.Completed, context.Status);
            Assert.Empty(context.WorkflowExecutionContext.Bookmarks);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(24)]
    [InlineData(168)]
    public async Task Should_CreateBookmark_WithCorrectExecuteAt(int hoursInFuture)
    {
        // Arrange
        var futureTime = DateTimeOffset.UtcNow.AddHours(hoursInFuture);
        var startAt = new StartAt(futureTime);

        // Act
        var context = await ExecuteAsync(startAt);

        // Assert
        var bookmark = Assert.Single(context.WorkflowExecutionContext.Bookmarks);
        var payload = Assert.IsType<StartAtPayload>(bookmark.Payload);
        Assert.Equal(futureTime, payload.ExecuteAt);
    }

    [Fact]
    public async Task Should_RecordExecutedAt_InJournalData()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var futureTime = now.AddHours(1);
        var startAt = new StartAt(futureTime);

        // Act
        var context = await ExecuteAsync(startAt, now);

        // Assert
        Assert.True(context.JournalData.ContainsKey("Executed At"));
        Assert.Equal(now, context.JournalData["Executed At"]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-24)]
    [InlineData(-168)]
    public async Task Should_Complete_When_DateTime_InPast_ByVariousAmounts(int hoursPast)
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var pastTime = now.AddHours(hoursPast);
        var startAt = new StartAt(pastTime);

        // Act
        var context = await ExecuteAsync(startAt, now);

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.Empty(context.WorkflowExecutionContext.Bookmarks);
    }

    private static async Task<ActivityExecutionContext> ExecuteAsync(StartAt activity, DateTimeOffset? clockTime = null)
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(clockTime ?? DateTimeOffset.UtcNow);

        return await new ActivityTestFixture(activity)
            .ConfigureServices(services => services.AddSingleton(clock))
            .ExecuteAsync();
    }
}