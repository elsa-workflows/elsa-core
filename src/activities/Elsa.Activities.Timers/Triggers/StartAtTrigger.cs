using Elsa.Models;
using Elsa.Triggers;
using NodaTime;

namespace Elsa.Activities.Timers.Triggers
{
    public class StartAtTrigger : Trigger
    {
        public Instant ExecuteAt { get; set; }
    }

    public class StartAtTriggerProvider : TriggerProvider<StartAtTrigger, StartAt>
    {
        public override ITrigger GetTrigger(TriggerProviderContext<StartAt> context)
        {
            var executeAt = context.Activity.GetState(x => x.ExecuteAt);

            if (executeAt == null || context.ActivityExecutionContext.WorkflowExecutionContext.WorkflowInstance.WorkflowStatus != WorkflowStatus.Suspended)
                return NullTrigger.Instance;

            return new StartAtTrigger
            {
                ExecuteAt = executeAt.Value,
            };
        }
    }
}