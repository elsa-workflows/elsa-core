using Elsa.Models;
using Elsa.Triggers;
using NodaTime;

namespace Elsa.Activities.Timers.Triggers
{
    public class TimerTrigger : Trigger
    {
        public Instant ExecuteAt { get; set; }
    }

    public class TimerTriggerProvider : TriggerProvider<TimerTrigger, Timer>
    {
        public override ITrigger GetTrigger(TriggerProviderContext<Timer> context)
        {
            var executeAt = context.Activity.GetState(x => x.ExecuteAt);

            if (executeAt == null || context.ActivityExecutionContext.WorkflowExecutionContext.WorkflowInstance.WorkflowStatus != WorkflowStatus.Suspended)
                return NullTrigger.Instance;

            return new TimerTrigger
            {
                ExecuteAt = executeAt.Value,
            };
        }
    }
}