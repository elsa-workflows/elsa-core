using Elsa.Scheduling;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Bookmarks;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Scheduling;

public class CronTests
{
    [Theory]
    [InlineData("0 0 0 * * *")]      // Daily at midnight
    [InlineData("0 0 */6 * * *")]    // Every 6 hours
    [InlineData("0 0 9 * * MON-FRI")] // Weekdays at 9 AM
    public async Task WhenNotTrigger_CreatesBookmarkWithParsedTime(string cronExpression)
    {
        // Arrange
        var expectedTime = new DateTimeOffset(2025, 1, 7, 0, 0, 0, TimeSpan.Zero);
        var cronParser = CreateCronParser(expectedTime);

        var activity = Cron.FromCronExpression(cronExpression);

        // Act
        var context = await ExecuteAsync(activity, cronParser);

        // Assert
        Assert.Equal(ActivityStatus.Running, context.Status);
        var payload = GetCronPayload(context);
        Assert.Equal(expectedTime, payload.ExecuteAt);
        Assert.Equal(cronExpression, payload.CronExpression);
    }

    [Fact]
    public async Task WhenIsTrigger_CompletesImmediately()
    {
        // Arrange
        var activity = Cron.FromCronExpression("0 0 0 * * *");

        // Act
        var context = await ExecuteAsTriggerAsync(activity);

        // Assert
        Assert.Equal(ActivityStatus.Completed, context.Status);
        Assert.Empty(context.WorkflowExecutionContext.Bookmarks);
    }

    [Fact]
    public async Task RecordsExecuteAtInJournal()
    {
        // Arrange
        var expectedTime = new DateTimeOffset(2025, 1, 7, 0, 0, 0, TimeSpan.Zero);
        var cronParser = CreateCronParser(expectedTime);
        var activity = Cron.FromCronExpression("0 0 0 * * *");

        // Act
        var context = await ExecuteAsync(activity, cronParser);

        // Assert
        Assert.True(context.JournalData.ContainsKey("ExecuteAt"));
        Assert.Equal(expectedTime, context.JournalData["ExecuteAt"]);
    }

    [Fact]
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
        var payload = GetCronPayload(context);
        Assert.Equal(cronExpression, payload.CronExpression);
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

    private static CronBookmarkPayload GetCronPayload(ActivityExecutionContext context)
    {
        var bookmark = Assert.Single(context.WorkflowExecutionContext.Bookmarks);
        return Assert.IsType<CronBookmarkPayload>(bookmark.Payload);
    }
}
