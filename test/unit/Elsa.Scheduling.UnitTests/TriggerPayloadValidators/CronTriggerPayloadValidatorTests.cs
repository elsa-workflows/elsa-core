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
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ValidateAsync_WithBlankExpression_ProducesNoErrors(string? cronExpression)
    {
        var validator = CreateValidator();
        var errors = new List<WorkflowValidationError>();

        await validator.ValidateAsync(new CronTriggerPayload(cronExpression!), new Workflow(), new StoredTrigger(), errors, default);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("* * * *")] // Too few fields.
    [InlineData("60 * * * * *")] // Invalid second.
    public async Task ValidateAsync_WithMalformedExpression_ProducesError(string cronExpression)
    {
        var validator = CreateValidator();
        var errors = new List<WorkflowValidationError>();

        await validator.ValidateAsync(new CronTriggerPayload(cronExpression), new Workflow(), new StoredTrigger(), errors, default);

        Assert.Single(errors);
    }

    [Theory]
    [InlineData("0 0 0 * * *")]
    [InlineData("0 0 9 * * MON-FRI")]
    public async Task ValidateAsync_WithValidExpression_ProducesNoErrors(string cronExpression)
    {
        var validator = CreateValidator();
        var errors = new List<WorkflowValidationError>();

        await validator.ValidateAsync(new CronTriggerPayload(cronExpression), new Workflow(), new StoredTrigger(), errors, default);

        Assert.Empty(errors);
    }

    private static CronTriggerPayloadValidator CreateValidator()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(new DateTimeOffset(2025, 1, 6, 12, 0, 0, TimeSpan.Zero));
        return new CronTriggerPayloadValidator(new CronosCronParser(clock));
    }
}
