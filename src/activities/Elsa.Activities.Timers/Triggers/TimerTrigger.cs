using System.Collections.Generic;
using Elsa.Models;
using Elsa.Triggers;
using NodaTime;

namespace Elsa.Activities.Timers.Triggers
{
    public class TimerTrigger : ITrigger
    {
        public Instant ExecuteAt { get; set; }
    }

    public class TimerWorkflowTriggerProvider : WorkflowTriggerProvider<TimerTrigger, Timer>
    {
        public override IEnumerable<ITrigger> GetTriggers(TriggerProviderContext<Timer> context)
        {
            var executeAt = context.Activity.GetState(x => x.ExecuteAt);

            if (executeAt == null || context.ActivityExecutionContext.WorkflowExecutionContext.WorkflowInstance.WorkflowStatus != WorkflowStatus.Suspended)
                yield break;

            yield return new TimerTrigger
            {
                ExecuteAt = executeAt.Value,
            };
        }
    }
}