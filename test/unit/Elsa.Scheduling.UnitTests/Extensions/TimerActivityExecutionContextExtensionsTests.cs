using Elsa.Common;
using Elsa.Extensions;
using Elsa.Scheduling;
using Elsa.Scheduling.Bookmarks;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Scheduling.UnitTests.Extensions;

public class TimerActivityExecutionContextExtensionsTests
{
    [Theory]
    [InlineData(1, 0, 0)] // 1 hour
    [InlineData(0, 15, 0)] // 15 minutes
    [InlineData(0, 0, 30)] // 30 seconds
    [InlineData(24, 0, 0)] // 1 day
    public async Task RepeatWithInterval_WhenNotTrigger_CalculatesCorrectResumeTime(int hours, int minutes, int seconds)
    {
        // Arrange
        var now = new DateTimeOffset(2025, 1, 6, 12, 0, 0, TimeSpan.Zero);
        var interval = new TimeSpan(hours, minutes, seconds);
        var expectedResumeAt = now.Add(interval);
        var clock = CreateClock(now);

        var activity = new Inline(ctx => ctx.RepeatWithInterval(interval));

        // Act
        var context = await ExecuteAsync(activity, clock);

        // Assert
        var payload = GetTimerPayload(context);
        Assert.Equal(expectedResumeAt, payload.ResumeAt);
    }

    [Fact]
    public async Task RepeatWithInterval_WhenIsTrigger_DoesNotCreateBookmark()
    {
        // Arrange
        var interval = TimeSpan.FromMinutes(30);
        var activity = new Inline(ctx => ctx.RepeatWithInterval(interval));

        // Act
        var context = await ExecuteAsTriggerAsync(activity);

        // Assert
        Assert.Empty(context.WorkflowExecutionContext.Bookmarks);
    }

    private static async Task<ActivityExecutionContext> ExecuteAsync(Inline activity, ISystemClock? clock = null)
    {
        var fixture = new ActivityTestFixture(activity);

        if (clock != null)
            fixture.ConfigureServices(services => services.AddSingleton(clock));

        return await fixture.ExecuteAsync();
    }

    private static async Task<ActivityExecutionContext> ExecuteAsTriggerAsync(Inline activity)
    {
        return await new ActivityTestFixture(activity)
            .ConfigureContext(ctx => ctx.WorkflowExecutionContext.TriggerActivityId = ctx.Activity.Id)
            .ExecuteAsync();
    }

    private static ISystemClock CreateClock(DateTimeOffset now)
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(now);
        return clock;
    }

    private static TimerBookmarkPayload GetTimerPayload(ActivityExecutionContext context)
    {
        var bookmark = Assert.Single(context.WorkflowExecutionContext.Bookmarks);
        Assert.Equal(SchedulingStimulusNames.Timer, bookmark.Name);
        return Assert.IsType<TimerBookmarkPayload>(bookmark.Payload);
    }
}
