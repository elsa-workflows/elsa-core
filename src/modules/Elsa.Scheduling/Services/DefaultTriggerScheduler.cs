using System.Text.Json;
using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Models.Requests;

namespace Elsa.Scheduling.Services;

/// <summary>
/// A default implementation of <see cref="ITriggerScheduler"/> that schedules triggers using <see cref="IWorkflowScheduler"/>.
/// </summary>
public class DefaultTriggerScheduler : ITriggerScheduler
{
    private readonly IWorkflowScheduler _workflowScheduler;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultTriggerScheduler"/> class.
    /// </summary>
    public DefaultTriggerScheduler(IWorkflowScheduler workflowScheduler)
    {
        _workflowScheduler = workflowScheduler;
    }

    /// <inheritdoc />
    public async Task ScheduleAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var triggerList = triggers.ToList();

        // Select all Timer triggers.
        var timerTriggers = triggerList.Filter<Activities.Timer>().ToList();
        var startAtTriggers = triggerList.Filter<StartAt>().ToList();

        // Schedule each Timer trigger.
        foreach (var trigger in timerTriggers)
        {
            var (startAt, interval) = JsonSerializer.Deserialize<TimerPayload>(trigger.Data!)!;
            var request = new DispatchWorkflowDefinitionRequest(trigger.WorkflowDefinitionId, VersionOptions.Published);
            await _workflowScheduler.ScheduleRecurringAsync(trigger.Id, request,  startAt, interval, cancellationToken);
        }

        // Schedule each StartAt trigger.
        foreach (var trigger in startAtTriggers)
        {
            var executeAt = JsonSerializer.Deserialize<StartAtPayload>(trigger.Data!)!.ExecuteAt;
            var input = new { ExecuteAt = executeAt }.ToDictionary();
            var request = new DispatchWorkflowDefinitionRequest
            {
                DefinitionId = trigger.WorkflowDefinitionId,
                VersionOptions = VersionOptions.Published,
                Input = input
            };
            
            await _workflowScheduler.ScheduleAtAsync(trigger.Id, request, executeAt, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task UnscheduleAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var triggerList = triggers.ToList();

        // Select all Timer triggers.
        var timerTriggers = triggerList.Filter<Activities.Timer>().ToList();

        // Select all StartAt triggers.
        var startAtTriggers = triggerList.Filter<Activities.Timer>().ToList();
        
        // Concatenate the filtered triggers.
        var filteredTriggers = timerTriggers.Concat(startAtTriggers).ToList();

        // Unschedule each trigger.
        foreach (var trigger in filteredTriggers)
            await _workflowScheduler.UnscheduleAsync(trigger.Id, cancellationToken);
    }
}