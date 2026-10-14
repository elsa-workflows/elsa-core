using Elsa.Common;
using Elsa.Scheduling.Bookmarks;
using Elsa.Scheduling.Services;
using Elsa.Scheduling.TriggerPayloadValidators;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Entities;
using NSubstitute;

namespace Elsa.Scheduling.UnitTests.TriggerPayloadValidators;

public class CronTriggerPayloadValidatorTests
{
    [Fact]
    public async Task ValidateAsync_WhenQuartzStyleParserThrowsFormatException_AddsValidationError()
    {
        var errors = await ValidateAsync(
            new ThrowingCronParser(new FormatException("Invalid Quartz cron expression")),
            "invalid");

        var error = Assert.Single(errors);
        Assert.Equal("Error when parsing cron expression: Invalid Quartz cron expression", error.Message);
        Assert.Equal("cron-activity", error.ActivityId);
    }

    [Fact]
    public async Task ValidateAsync_WhenCronosParserRejectsExpression_AddsValidationError()
    {
        var errors = await ValidateAsync(CreateCronosParser(), "not a cron expression");

        var error = Assert.Single(errors);
        Assert.StartsWith("Error when parsing cron expression: ", error.Message);
        Assert.Equal("cron-activity", error.ActivityId);
    }

    [Fact]
    public async Task ValidateAsync_WhenCronosParserAcceptsExpression_HasNoErrors()
    {
        var errors = await ValidateAsync(CreateCronosParser(), "0 0 0 * * *");

        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateAsync_WhenParserThrowsUnexpectedException_DoesNotSuppressIt()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await ValidateAsync(
                new ThrowingCronParser(new InvalidOperationException("Parser unavailable")),
                "0 0 0 * * *"));
    }

    private static async Task<List<WorkflowValidationError>> ValidateAsync(ICronParser parser, string cronExpression)
    {
        var validator = new CronTriggerPayloadValidator(parser);
        var trigger = new StoredTrigger { ActivityId = "cron-activity" };
        List<WorkflowValidationError> errors = [];

        await validator.ValidateAsync(
            new CronTriggerPayload(cronExpression),
            new Workflow(),
            trigger,
            errors,
            CancellationToken.None);

        return errors;
    }

    private static CronosCronParser CreateCronosParser()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(new DateTimeOffset(2025, 1, 6, 12, 0, 0, TimeSpan.Zero));
        return new CronosCronParser(clock);
    }

    private sealed class ThrowingCronParser(Exception exception) : ICronParser
    {
        public DateTimeOffset GetNextOccurrence(string expression) => throw exception;
    }
}
