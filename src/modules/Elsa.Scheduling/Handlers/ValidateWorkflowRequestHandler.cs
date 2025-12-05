using Cronos;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Scheduling.Bookmarks;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;

namespace Elsa.Scheduling.Handlers;

/// <summary>
/// A <see cref="ValidateWorkflowRequestHandler"/> handler that validates a workflow cron epxression and return any errors.
/// </summary>
[UsedImplicitly]
public class ValidateWorkflowRequestHandler : INotificationHandler<WorkflowDefinitionValidating>
{
    private readonly ITriggerIndexer _triggerIndexer;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ValidateWorkflowRequestHandler(ITriggerIndexer triggerIndexer)
    {
        _triggerIndexer = triggerIndexer;
    }
    
    public async Task HandleAsync(WorkflowDefinitionValidating notification, CancellationToken cancellationToken)
    {
        var workflow = notification.Workflow;
        var validationErrors = notification.ValidationErrors;
        
        var cronTriggers = (await _triggerIndexer.GetTriggersAsync(workflow, cancellationToken))
            .Where(x => x.Payload is CronTriggerPayload)
            .ToList();

        foreach (var cronTrigger in cronTriggers)
        {
            var cronPayload = cronTrigger.GetPayload<CronTriggerPayload>();

            if (!CronExpression.TryParse(cronPayload.CronExpression, out _))
            {
                var message = $"The Cron Expression: {cronPayload.CronExpression} is not a valid Cron Expression!";
                validationErrors.Add(new WorkflowValidationError(message, cronTrigger.ActivityId));
            }
        }
        
        var timerTriggers = (await _triggerIndexer.GetTriggersAsync(workflow, cancellationToken))
            .Where(x => x.Payload is TimerTriggerPayload)
            .ToList();

        foreach (var timerTrigger in timerTriggers)
        {
            var cronPayload = timerTrigger.GetPayload<CronTriggerPayload>();

            if (!CronExpression.TryParse(cronPayload.CronExpression, out _))
            {
                var message = $"The Cron Expression: {cronPayload.CronExpression} is not a valid Cron Expression!";
                validationErrors.Add(new WorkflowValidationError(message, timerTrigger.ActivityId));
            }
        }
    }
}
