using Elsa.Common;
using Elsa.Scheduling;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Bookmarks;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Scheduling;

public class DelayTests
{
    [Test]
    [Arguments(1, 0, 0)]    // 1 hour
    [Arguments(0, 30, 0)]   // 30 minutes
    [Arguments(0, 0, 45)]   // 45 seconds
    [Arguments(24, 0, 0)]   // 1 day
    public async Task CreatesBookmark_WithCorrectResumeTime(int hours, int minutes, int seconds)
    {
        // Arrange
        var now = new DateTimeOffset(2025, 1, 6, 12, 0, 0, TimeSpan.Zero);
        var delay = new TimeSpan(hours, minutes, seconds);
        var expectedResumeAt = now.Add(delay);
        var clock = CreateClock(now);

        var activity = new Delay(delay);

        // Act
        var context = await ExecuteAsync(activity, clock);

        // Assert
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Running);
        var payload = await GetDelayPayload(context);
        await Assert.That(payload.ResumeAt).IsEqualTo(expectedResumeAt);
    }

    [Test]
    [MethodDataSource(nameof(FactoryMethodTestCases))]
    public async Task FactoryMethods_CreateCorrectTimeSpans(Delay activity, TimeSpan expected)
    {
        // Arrange
        var now = new DateTimeOffset(2025, 1, 6, 12, 0, 0, TimeSpan.Zero);
        var clock = CreateClock(now);

        // Act
        var context = await ExecuteAsync(activity, clock);

        // Assert
        var payload = await GetDelayPayload(context);
        await Assert.That(payload.ResumeAt).IsEqualTo(now.Add(expected));
    }

    public static IEnumerable<Func<(Delay, TimeSpan)>> FactoryMethodTestCases() =>
    [
        () => (Delay.FromSeconds(30), TimeSpan.FromSeconds(30)),
        () => (Delay.FromMinutes(5), TimeSpan.FromMinutes(5)),
        () => (Delay.FromHours(2), TimeSpan.FromHours(2)),
        () => (Delay.FromDays(1), TimeSpan.FromDays(1))
    ];

    private static async Task<ActivityExecutionContext> ExecuteAsync(Delay activity, ISystemClock clock)
    {
        return await new ActivityTestFixture(activity)
            .ConfigureServices(services => services.AddSingleton(clock))
            .ExecuteAsync();
    }

    private static ISystemClock CreateClock(DateTimeOffset now)
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(now);
        return clock;
    }

    private static async Task<DelayPayload> GetDelayPayload(ActivityExecutionContext context)
    {
        var bookmark = await Assert.That(context.WorkflowExecutionContext.Bookmarks).HasSingleItem();
        await Assert.That(bookmark.Name).IsEqualTo(SchedulingStimulusNames.Delay);
        await Assert.That(bookmark.Payload).IsOfType(typeof(DelayPayload));
        return (DelayPayload)bookmark.Payload!;
    }
}
