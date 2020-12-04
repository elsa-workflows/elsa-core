using Elsa.Models;
using Elsa.Triggers;
using NodaTime;

namespace Elsa.Activities.Timers.Triggers
{
    public class CronEventTrigger : Trigger
    {
        public Instant ExecuteAt { get; set; }
    }
    
    public class CronEventTriggerProvider : TriggerProvider<CronEventTrigger, Cron>
    {
        public override ITrigger GetTrigger(TriggerProviderContext<Cron> context)
        {
            var executeAt = context.Activity.GetState(x => x.ExecuteAt);

            if (executeAt == null || context.ActivityExecutionContext.WorkflowExecutionContext.WorkflowInstance.Status != WorkflowStatus.Suspended)
                return NullTrigger.Instance;

            return new CronEventTrigger
            {
                ExecuteAt = executeAt.Value,
            };
        }
    }
}