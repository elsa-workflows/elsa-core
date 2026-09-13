using Elsa.Scheduling;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Bookmarks;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Scheduling;

public class CronTests
{
    [Test]
    [Arguments("0 0 0 * * *")]      // Daily at midnight
    [Arguments("0 0 */6 * * *")]    // Every 6 hours
    [Arguments("0 0 9 * * MON-FRI")] // Weekdays at 9 AM
    public async Task WhenNotTrigger_CreatesBookmarkWithParsedTime(string cronExpression)
    {
        // Arrange
        var expectedTime = new DateTimeOffset(2025, 1, 7, 0, 0, 0, TimeSpan.Zero);
        var cronParser = CreateCronParser(expectedTime);

        var activity = Cron.FromCronExpression(cronExpression);

        // Act
        var context = await ExecuteAsync(activity, cronParser);

        // Assert
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Running);
        var payload = await GetCronPayload(context);
        await Assert.That(payload.ExecuteAt).IsEqualTo(expectedTime);
        await Assert.That(payload.CronExpression).IsEqualTo(cronExpression);
    }

    [Test]
    public async Task WhenIsTrigger_CompletesImmediately()
    {
        // Arrange
        var activity = Cron.FromCronExpression("0 0 0 * * *");

        // Act
        var context = await ExecuteAsTriggerAsync(activity);

        // Assert
        await Assert.That(context.Status).IsEqualTo(ActivityStatus.Completed);
        await Assert.That(context.WorkflowExecutionContext.Bookmarks).IsEmpty();
    }

    [Test]
    public async Task RecordsExecuteAtInJournal()
    {
        // Arrange
        var expectedTime = new DateTimeOffset(2025, 1, 7, 0, 0, 0, TimeSpan.Zero);
        var cronParser = CreateCronParser(expectedTime);
        var activity = Cron.FromCronExpression("0 0 0 * * *");

        // Act
        var context = await ExecuteAsync(activity, cronParser);

        // Assert
        await Assert.That(context.JournalData.TryGetValue("ExecuteAt", out var executeAt)).IsTrue();
        await Assert.That(executeAt).IsEqualTo(expectedTime);
    }

    [Test]
    public async Task FactoryMethod_CreatesWithExpression()
    {
        // Arrange
        var cronExpression = "0 0 12 * * *";
        var expectedTime = new DateTimeOffset(2025, 1, 7, 12, 0, 0, TimeSpan.Zero);
        var cronParser = CreateCronParser(expectedTime);

        var activity = Cron.FromCronExpression(cronExpression);

        // Act
        var context = await ExecuteAsync(activity, cronParser);

        // Assert
        var payload = await GetCronPayload(context);
        await Assert.That(payload.CronExpression).IsEqualTo(cronExpression);
    }

    private static async Task<ActivityExecutionContext> ExecuteAsync(Cron activity, ICronParser cronParser)
    {
        return await new ActivityTestFixture(activity)
            .ConfigureServices(services => services.AddSingleton(cronParser))
            .ExecuteAsync();
    }

    private static async Task<ActivityExecutionContext> ExecuteAsTriggerAsync(Cron activity)
    {
        return await new ActivityTestFixture(activity)
            .ConfigureContext(ctx => ctx.WorkflowExecutionContext.TriggerActivityId = ctx.Activity.Id)
            .ExecuteAsync();
    }

    private static ICronParser CreateCronParser(DateTimeOffset returnValue)
    {
        var parser = Substitute.For<ICronParser>();
        parser.GetNextOccurrence(Arg.Any<string>()).Returns(returnValue);
        return parser;
    }

    private static async Task<CronBookmarkPayload> GetCronPayload(ActivityExecutionContext context)
    {
        var bookmark = await Assert.That(context.WorkflowExecutionContext.Bookmarks).HasSingleItem();
        await Assert.That(bookmark.Payload).IsOfType(typeof(CronBookmarkPayload));
        return (CronBookmarkPayload)bookmark.Payload!;
    }
}
