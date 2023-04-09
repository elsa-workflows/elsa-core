using System.Text.Json;
using Elsa.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Scheduling.Services;

public class WorkflowTriggerScheduler : IWorkflowTriggerScheduler
{
    private const string RootGroupKey = "WorkflowDefinition";

    // private readonly IJobScheduler _jobScheduler;
    //
    // public WorkflowTriggerScheduler(IJobScheduler jobScheduler)
    // {
    //     _jobScheduler = jobScheduler;
    // }

    public async Task ScheduleTriggersAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var triggerList = triggers.ToList();

        // Select all Timer triggers.
        var timerTriggers = triggerList.Filter<Activities.Timer>().ToList();
        var startAtTriggers = triggerList.Filter<StartAt>().ToList();

        // Schedule each Timer trigger.
        foreach (var trigger in timerTriggers)
        {
            var (dateTime, timeSpan) = JsonSerializer.Deserialize<TimerPayload>(trigger.Data!)!;
            var groupKeys = new[] { RootGroupKey, trigger.WorkflowDefinitionId };
            //await _jobScheduler.ScheduleAsync(new RunWorkflowJob(trigger.WorkflowDefinitionId), trigger.WorkflowDefinitionId, new RecurringSchedule(dateTime, timeSpan), groupKeys, cancellationToken);
        }

        // Schedule each StartAt trigger.
        foreach (var trigger in startAtTriggers)
        {
            var executeAt = JsonSerializer.Deserialize<StartAtPayload>(trigger.Data!)!.ExecuteAt;
            var groupKeys = new[] { RootGroupKey, trigger.WorkflowDefinitionId };
            var input = new { ExecuteAt = executeAt }.ToDictionary();
            //await _jobScheduler.ScheduleAsync(new RunWorkflowJob(trigger.WorkflowDefinitionId, input), trigger.WorkflowDefinitionId, new SpecificInstantSchedule(executeAt), groupKeys, cancellationToken);
        }
    }

    public async Task UnscheduleTriggersAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var triggerList = triggers.ToList();

        // Select all Timer triggers.
        var timerTriggers = triggerList.Filter<Activities.Timer>().ToList();

        // Select all StartAt triggers.
        var startAtTriggers = triggerList.Filter<Activities.Timer>().ToList();

        // Unschedule all triggers for the distinct set of affected workflows.
        var workflowDefinitionIds = timerTriggers
            .Select(x => x.WorkflowDefinitionId)
            .Concat(startAtTriggers.Select(x => x.WorkflowDefinitionId))
            .Distinct()
            .ToList();

        foreach (var workflowDefinitionId in workflowDefinitionIds)
        {
            var groupKeys = new[] { RootGroupKey, workflowDefinitionId };
            //await _jobScheduler.ClearAsync(groupKeys, cancellationToken);
        }
    }
}