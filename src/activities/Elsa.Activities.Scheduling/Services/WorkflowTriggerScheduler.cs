using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Scheduling.Contracts;
using Elsa.Activities.Scheduling.Jobs;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Extensions;
using Elsa.Scheduling.Contracts;
using Elsa.Scheduling.Schedules;

namespace Elsa.Activities.Scheduling.Services;

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

        // Unschedule all triggers for the distinct set of affected workflows.
        var workflowDefinitionIds = timerTriggers.Select(x => x.WorkflowDefinitionId).Distinct().ToList();

        foreach (var workflowDefinitionId in workflowDefinitionIds)
        {
            var groupKeys = new[] { RootGroupKey, workflowDefinitionId };
            await _jobScheduler.ClearAsync(groupKeys, cancellationToken);
        }

        foreach (var trigger in timerTriggers)
        {
            // Schedule trigger.
            var (dateTime, timeSpan) = JsonSerializer.Deserialize<TimerPayload>(trigger.Payload!)!;
            var groupKeys = new[] { RootGroupKey, trigger.WorkflowDefinitionId };
            await _jobScheduler.ScheduleAsync(new RunWorkflowJob(trigger.WorkflowDefinitionId), new RecurringSchedule(dateTime, timeSpan), groupKeys, cancellationToken);
        }
    }
}