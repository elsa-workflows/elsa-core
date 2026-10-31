using Elsa.Scheduling.Bookmarks;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Scheduling.TriggerPayloadValidators;

public class CronTriggerPayloadValidator(ICronParser cronParser) : ITriggerPayloadValidator<CronTriggerPayload>
{
    public Task ValidateAsync(
        CronTriggerPayload payload,
        Workflow workflow,
        StoredTrigger trigger,
        ICollection<WorkflowValidationError> validationErrors,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(payload.CronExpression))
        {
            validationErrors.Add(new("Cron expression is required", trigger.ActivityId));
            return Task.CompletedTask;
        }

        try
        {
            cronParser.GetNextOccurrence(payload.CronExpression);
        }
        catch (FormatException ex)
        {
            validationErrors.Add(new("Error when parsing cron expression: " + ex.Message,
                trigger.ActivityId));
        }
        return Task.CompletedTask;
    }
}