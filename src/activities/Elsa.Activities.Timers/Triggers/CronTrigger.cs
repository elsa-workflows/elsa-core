using System.Collections.Generic;
using Elsa.Models;
using Elsa.Triggers;
using NodaTime;

namespace Elsa.Activities.Timers.Triggers
{
    public class CronTrigger : ITrigger
    {
        public Instant ExecuteAt { get; set; }
    }
    
    public class CronWorkflowTriggerProvider : WorkflowTriggerProvider<CronTrigger, Cron>
    {
        public override IEnumerable<ITrigger> GetTriggers(TriggerProviderContext<Cron> context)
        {
            var executeAt = context.Activity.GetState(x => x.ExecuteAt);

            if (executeAt == null || context.ActivityExecutionContext.WorkflowExecutionContext.WorkflowInstance.WorkflowStatus != WorkflowStatus.Suspended)
                yield break;

            yield return new CronTrigger
            {
                ExecuteAt = executeAt.Value,
            };
        }
    }
}