using Elsa.Models;
using Elsa.Triggers;
using NodaTime;

namespace Elsa.Activities.Timers.Triggers
{
    public class TimerEventTrigger : Trigger
    {
        public Instant ExecuteAt { get; set; }
    }

    public class TimerEventTriggerProvider : TriggerProvider<TimerEventTrigger, TimerEvent>
    {
        public override ITrigger GetTrigger(TriggerProviderContext<TimerEvent> context)
        {
            var executeAt = context.Activity.GetState(x => x.ExecuteAt);

            if (executeAt == null || context.ActivityExecutionContext.WorkflowExecutionContext.WorkflowInstance.Status != WorkflowStatus.Suspended)
                return NullTrigger.Instance;

            return new TimerEventTrigger
            {
                ExecuteAt = executeAt.Value,
            };
        }
    }
}