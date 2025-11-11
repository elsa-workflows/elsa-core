using Elsa.Common;
using Elsa.Scheduling;
using Elsa.Scheduling.Bookmarks;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Timer = Elsa.Scheduling.Activities.Timer;

namespace Elsa.Activities.UnitTests.Scheduling;

public class TimerTests
{
    [Theory]
    [InlineData(1, 0, 0)]    // 1 hour
    [InlineData(0, 30, 0)]   // 30 minutes
    [InlineData(0, 0, 45)]   // 45 seconds
    public async Task WhenNotTrigger_CreatesBookmarkWithCorrectResumeTime(int hours, int minutes, int seconds)
    {
        // Arrange
        var now = new DateTimeOffset(2025, 1, 6, 12, 0, 0, TimeSpan.Zero);
        var interval = new TimeSpan(hours, minutes, seconds);
        var expectedResumeAt = now.Add(interval);
        var clock = CreateClock(now);

        var activity = new Timer(interval);

        // Act
        var context = await ExecuteAsync(activity, clock);

        // Assert
        Assert.Equal(ActivityStatus.Running, context.Status);
        var payload = GetTimerPayload(context);
        Assert.Equal(expectedResumeAt, payload.ResumeAt);
    }

    [Fact]
    public async Task WhenIsTrigger_CompletesImmediately()
    {
        // Arrange
        var interval = TimeSpan.FromMinutes(30);
        var activity = new Timer(interval);

        // Act
        var context = await ExecuteAsTriggerAsync(activity);

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.Empty(context.WorkflowExecutionContext.Bookmarks);
    }

    [Theory]
    [MemberData(nameof(FactoryMethodTestCases))]
    public async Task FactoryMethods_CreateCorrectIntervals(Timer activity, TimeSpan expected)
    {
        // Arrange
        var now = new DateTimeOffset(2025, 1, 6, 12, 0, 0, TimeSpan.Zero);
        var clock = CreateClock(now);

        // Act
        var context = await ExecuteAsync(activity, clock);

        // Assert
        var payload = GetTimerPayload(context);
        Assert.Equal(now.Add(expected), payload.ResumeAt);
    }

    public static TheoryData<Timer, TimeSpan> FactoryMethodTestCases() => new()
    {
        { Timer.FromSeconds(30), TimeSpan.FromSeconds(30) },
        { Timer.FromTimeSpan(TimeSpan.FromMinutes(15)), TimeSpan.FromMinutes(15) }
    };

    private static async Task<ActivityExecutionContext> ExecuteAsync(Timer activity, ISystemClock clock)
    {
        return await new ActivityTestFixture(activity)
            .ConfigureServices(services => services.AddSingleton(clock))
            .ExecuteAsync();
    }

    private static async Task<ActivityExecutionContext> ExecuteAsTriggerAsync(Timer activity)
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
