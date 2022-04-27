using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Jobs.Schedules;
using Elsa.Jobs.Services;
using Elsa.Modules.Scheduling.Activities;
using Elsa.Modules.Scheduling.Jobs;
using Elsa.Modules.Scheduling.Services;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Extensions;
using Timer = Elsa.Modules.Scheduling.Activities.Timer;

namespace Elsa.Modules.Scheduling.Implementations;

public class WorkflowTriggerScheduler : IWorkflowTriggerScheduler
{
    private const string RootGroupKey = "WorkflowDefinition";

    private readonly IJobScheduler _jobScheduler;

    public WorkflowTriggerScheduler(IJobScheduler jobScheduler)
    {
        _jobScheduler = jobScheduler;
    }

    public async Task ScheduleTriggersAsync(IEnumerable<WorkflowTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var triggerList = triggers.ToList();

        // Select all Timer triggers.
        var timerTriggers = triggerList.Filter<Timer>().ToList();
        var startAtTriggers = triggerList.Filter<StartAt>().ToList();

        // Schedule each Timer trigger.
        foreach (var trigger in timerTriggers)
        {
            var (dateTime, timeSpan) = JsonSerializer.Deserialize<TimerPayload>(trigger.Data!)!;
            var groupKeys = new[] { RootGroupKey, trigger.WorkflowDefinitionId };
            await _jobScheduler.ScheduleAsync(new RunWorkflowJob(trigger.WorkflowDefinitionId), trigger.WorkflowDefinitionId, new RecurringSchedule(dateTime, timeSpan), groupKeys, cancellationToken);
        }

        // Schedule each StartAt trigger.
        foreach (var trigger in startAtTriggers)
        {
            var executeAt = JsonSerializer.Deserialize<StartAtPayload>(trigger.Data!)!.ExecuteAt;
            var groupKeys = new[] { RootGroupKey, trigger.WorkflowDefinitionId };
            await _jobScheduler.ScheduleAsync(new RunWorkflowJob(trigger.WorkflowDefinitionId), trigger.WorkflowDefinitionId, new SpecificInstantSchedule(executeAt), groupKeys, cancellationToken);
        }
    }

    public async Task UnscheduleTriggersAsync(IEnumerable<WorkflowTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var triggerList = triggers.ToList();

        // Select all Timer triggers.
        var timerTriggers = triggerList.Filter<Timer>().ToList();

        // Select all StartAt triggers.
        var startAtTriggers = triggerList.Filter<Timer>().ToList();

        // Unschedule all triggers for the distinct set of affected workflows.
        var workflowDefinitionIds = timerTriggers
            .Select(x => x.WorkflowDefinitionId)
            .Concat(startAtTriggers.Select(x => x.WorkflowDefinitionId))
            .Distinct()
            .ToList();

        foreach (var workflowDefinitionId in workflowDefinitionIds)
        {
            var groupKeys = new[] { RootGroupKey, workflowDefinitionId };
            await _jobScheduler.ClearAsync(groupKeys, cancellationToken);
        }
    }
}