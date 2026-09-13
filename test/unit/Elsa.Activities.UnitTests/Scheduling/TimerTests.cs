using Elsa.Common;
using Elsa.Scheduling;
using Elsa.Scheduling.Bookmarks;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Timer = Elsa.Scheduling.Activities.Timer;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Scheduling;

public class TimerTests
{
    [Test]
    [Arguments(1, 0, 0)]    // 1 hour
    [Arguments(0, 30, 0)]   // 30 minutes
    [Arguments(0, 0, 45)]   // 45 seconds
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
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Running);
        var payload = await GetTimerPayload(context);
        await Assert.That(payload.ResumeAt).IsEqualTo(expectedResumeAt);
    }

    [Test]
    public async Task WhenIsTrigger_CompletesImmediately()
    {
        // Arrange
        var interval = TimeSpan.FromMinutes(30);
        var activity = new Timer(interval);

        // Act
        var context = await ExecuteAsTriggerAsync(activity);

        // Assert
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Completed);
        await Assert.That(context.WorkflowExecutionContext.Bookmarks).IsEmpty();
    }

    [Test]
    [MethodDataSource(nameof(FactoryMethodTestCases))]
    public async Task FactoryMethods_CreateCorrectIntervals(Timer activity, TimeSpan expected)
    {
        // Arrange
        var now = new DateTimeOffset(2025, 1, 6, 12, 0, 0, TimeSpan.Zero);
        var clock = CreateClock(now);

        // Act
        var context = await ExecuteAsync(activity, clock);

        // Assert
        var payload = await GetTimerPayload(context);
        await Assert.That(payload.ResumeAt).IsEqualTo(now.Add(expected));
    }

    public static IEnumerable<Func<(Timer, TimeSpan)>> FactoryMethodTestCases() =>
    [
        () => (Timer.FromSeconds(30), TimeSpan.FromSeconds(30)),
        () => (Timer.FromTimeSpan(TimeSpan.FromMinutes(15)), TimeSpan.FromMinutes(15))
    ];

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

    private static async Task<TimerBookmarkPayload> GetTimerPayload(ActivityExecutionContext context)
    {
        var bookmark = await Assert.That(context.WorkflowExecutionContext.Bookmarks).HasSingleItem();
        await Assert.That(bookmark.Name).IsEqualTo(SchedulingStimulusNames.Timer);
        await Assert.That(bookmark.Payload).IsOfType(typeof(TimerBookmarkPayload));
        return (TimerBookmarkPayload)bookmark.Payload!;
    }
}
