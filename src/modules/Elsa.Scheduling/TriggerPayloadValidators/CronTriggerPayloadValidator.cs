using Cronos;
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
        // A blank expression means the trigger is intentionally not configured (disabled); skip validation.
        if (string.IsNullOrWhiteSpace(payload.CronExpression))
            return Task.CompletedTask;

        try
        {
            cronParser.GetNextOccurrence(payload.CronExpression);
        }
        catch (CronFormatException ex)
        {
            validationErrors.Add(new("Error when parsing cron expression: " + ex.Message,
                trigger.ActivityId));
        }
        return Task.CompletedTask;
    }
}