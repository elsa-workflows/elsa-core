using System.Collections.Generic;
using Elsa.Models;
using Elsa.Triggers;
using NodaTime;

namespace Elsa.Activities.Timers.Triggers
{
    public class StartAtTrigger : ITrigger
    {
        public Instant ExecuteAt { get; set; }
    }

    public class StartAtWorkflowTriggerProvider : WorkflowTriggerProvider<StartAtTrigger, StartAt>
    {
        public override IEnumerable<ITrigger> GetTriggers(TriggerProviderContext<StartAt> context)
        {
            var executeAt = context.Activity.GetState(x => x.ExecuteAt);

            if (executeAt == null || context.ActivityExecutionContext.WorkflowExecutionContext.WorkflowInstance.WorkflowStatus != WorkflowStatus.Suspended)
                yield break;

            yield return new StartAtTrigger
            {
                ExecuteAt = executeAt.Value,
            };
        }
    }
}