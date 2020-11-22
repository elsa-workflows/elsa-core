using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Services;
using Elsa.Triggers;
using NodaTime;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.Timers.Triggers
{
    public class InstantEventTrigger : Trigger
    {
        /// <summary>
        /// The instant at which the event should trigger.
        /// </summary>
        public Instant ExecuteAt { get; set; }

        public override bool IsOneOff => true;
    }

    public class InstantEventTriggerProvider : TriggerProvider<InstantEventTrigger, InstantEvent>
    {
        private readonly IWorkflowInstanceManager _workflowInstanceManager;
        private readonly IClock _clock;

        public InstantEventTriggerProvider(IWorkflowInstanceManager workflowInstanceManager, IClock clock)
        {
            _workflowInstanceManager = workflowInstanceManager;
            _clock = clock;
        }
        
        public override async ValueTask<ITrigger> GetTriggerAsync(TriggerProviderContext<InstantEvent> context, CancellationToken cancellationToken)
        {
            // Only provide a trigger if the workflow hasn't executed already sometime in the past.
            var workflowDefinitionId = context.ActivityExecutionContext.WorkflowExecutionContext.WorkflowBlueprint.Id;
            var instanceCount = await _workflowInstanceManager.ListByDefinitionAsync(workflowDefinitionId, cancellationToken).Count();
            var executeAt = context.Activity.GetState(x => x.ExecuteAt);
            var now = _clock.GetCurrentInstant();

            // If the configured instant lies in the past, and the workflow was already executed once, we don't trigger again.
            if (executeAt <= now && instanceCount > 0)
                return NullTrigger.Instance;
            
            return new InstantEventTrigger
            {
                ExecuteAt = executeAt
            };
        }
    }
}