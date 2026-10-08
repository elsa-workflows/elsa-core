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

public class DelayActivityExecutionContextExtensionsTests
{
    [Theory]
    [InlineData(1, 0, 0)] // 1 hour
    [InlineData(0, 30, 0)] // 30 minutes
    [InlineData(0, 0, 45)] // 45 seconds
    [InlineData(24, 0, 0)] // 1 day
    public async Task DelayFor_CalculatesCorrectResumeTime(int hours, int minutes, int seconds)
    {
        // Arrange
        var now = new DateTimeOffset(2025, 1, 6, 12, 0, 0, TimeSpan.Zero);
        var delay = new TimeSpan(hours, minutes, seconds);
        var expectedResumeAt = now.Add(delay);
        var clock = CreateClock(now);

        var activity = new Inline(ctx => ctx.DelayFor(delay));

        // Act
        var context = await ExecuteAsync(activity, clock);

        // Assert
        var payload = GetDelayPayload(context);
        Assert.Equal(expectedResumeAt, payload.ResumeAt);
    }

    [Theory]
    [InlineData(2025, 1, 6, 14, 30, 0)]
    [InlineData(2025, 12, 31, 23, 59, 59)]
    [InlineData(2026, 6, 15, 8, 0, 0)]
    public async Task DelayUntil_UsesExactTime(int year, int month, int day, int hour, int minute, int second)
    {
        // Arrange
        var resumeAt = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);
        var activity = new Inline(ctx => ctx.DelayUntil(resumeAt));

        // Act
        var context = await ExecuteAsync(activity);

        // Assert
        var payload = GetDelayPayload(context);
        Assert.Equal(resumeAt, payload.ResumeAt);
    }

    private static async Task<ActivityExecutionContext> ExecuteAsync(Inline activity, ISystemClock? clock = null)
    {
        var fixture = new ActivityTestFixture(activity);

        if (clock != null)
            fixture.ConfigureServices(services => services.AddSingleton(clock));

        return await fixture.ExecuteAsync();
    }

    private static ISystemClock CreateClock(DateTimeOffset now)
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(now);
        return clock;
    }

    private static DelayPayload GetDelayPayload(ActivityExecutionContext context)
    {
        var bookmark = Assert.Single(context.WorkflowExecutionContext.Bookmarks);
        Assert.Equal(SchedulingStimulusNames.Delay, bookmark.Name);
        return Assert.IsType<DelayPayload>(bookmark.Payload);
    }
}
