using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Modules.Scheduling.Contracts;
using Elsa.Modules.Scheduling.Jobs;
using Elsa.Modules.Scheduling.Triggers;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Extensions;
using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Schedules;
using Timer = Elsa.Modules.Scheduling.Triggers.Timer;

namespace Elsa.Modules.Scheduling.Services;

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
        // Select all Timer triggers.
        var timerTriggers = triggers.Filter<Timer>().ToList();

        // Schedule each trigger.
        foreach (var trigger in timerTriggers)
        {
            // Schedule trigger.
            var (dateTime, timeSpan) = JsonSerializer.Deserialize<TimerPayload>(trigger.Data!)!;
            var groupKeys = new[] { RootGroupKey, trigger.WorkflowDefinitionId };
            await _jobScheduler.ScheduleAsync(new RunWorkflowJob(trigger.WorkflowDefinitionId), new RecurringSchedule(dateTime, timeSpan), groupKeys, cancellationToken);
        }
    }

    public async Task UnscheduleTriggersAsync(IEnumerable<WorkflowTrigger> triggers, CancellationToken cancellationToken = default)
    {
        // Select all Timer triggers.
        var timerTriggers = triggers.Filter<Timer>().ToList();

        // Unschedule all triggers for the distinct set of affected workflows.
        var workflowDefinitionIds = timerTriggers.Select(x => x.WorkflowDefinitionId).Distinct().ToList();

        foreach (var workflowDefinitionId in workflowDefinitionIds)
        {
            var groupKeys = new[] { RootGroupKey, workflowDefinitionId };
            await _jobScheduler.ClearAsync(groupKeys, cancellationToken);
        }
    }
}