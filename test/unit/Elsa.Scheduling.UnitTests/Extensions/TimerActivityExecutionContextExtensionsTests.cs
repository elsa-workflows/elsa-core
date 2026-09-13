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
    [Test]
    [Arguments(1, 0, 0)] // 1 hour
    [Arguments(0, 15, 0)] // 15 minutes
    [Arguments(0, 0, 30)] // 30 seconds
    [Arguments(24, 0, 0)] // 1 day
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
        var payload = await GetTimerPayload(context);
        await Assert.That(payload.ResumeAt).IsEqualTo(expectedResumeAt);
    }

    [Test]
    public async Task RepeatWithInterval_WhenIsTrigger_DoesNotCreateBookmark()
    {
        // Arrange
        var interval = TimeSpan.FromMinutes(30);
        var activity = new Inline(ctx => ctx.RepeatWithInterval(interval));

        // Act
        var context = await ExecuteAsTriggerAsync(activity);

        // Assert
        await Assert.That(context.WorkflowExecutionContext.Bookmarks).IsEmpty();
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

    private static async Task<TimerBookmarkPayload> GetTimerPayload(ActivityExecutionContext context)
    {
        var bookmark = await Assert.That(context.WorkflowExecutionContext.Bookmarks).HasSingleItem();
        await Assert.That(bookmark.Name).IsEqualTo(SchedulingStimulusNames.Timer);
        await Assert.That(bookmark.Payload).IsOfType(typeof(TimerBookmarkPayload));
        return (TimerBookmarkPayload)bookmark.Payload!;
    }
}
