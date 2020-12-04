using Elsa.Models;
using Elsa.Triggers;
using NodaTime;

namespace Elsa.Activities.Timers.Triggers
{
    public class CronTrigger : Trigger
    {
        public Instant ExecuteAt { get; set; }
    }
    
    public class CronTriggerProvider : TriggerProvider<CronTrigger, Cron>
    {
        public override ITrigger GetTrigger(TriggerProviderContext<Cron> context)
        {
            var executeAt = context.Activity.GetState(x => x.ExecuteAt);

            if (executeAt == null || context.ActivityExecutionContext.WorkflowExecutionContext.WorkflowInstance.Status != WorkflowStatus.Suspended)
                return NullTrigger.Instance;

            return new CronTrigger
            {
                ExecuteAt = executeAt.Value,
            };
        }
    }
}