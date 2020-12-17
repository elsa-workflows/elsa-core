using System.Threading.Tasks;

using Elsa.Activities.Timers.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Timers
{

    [Trigger(Category = "Timers", Description = "Cancel a timer (Cron, StartAt, Timer) so that it is not executed. ")]
    public class CancelTimer : Activity
    {
        private readonly IWorkflowScheduler _workflowScheduler;

        public CancelTimer(IWorkflowScheduler workflowScheduler)
        {
            _workflowScheduler = workflowScheduler;
        }

        [ActivityProperty(Hint = "The id of the timer (Cron, StartAt, Timer) activity, which is to be canceled")]
        public string ActivityId { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            await _workflowScheduler.UnscheduleWorkflowAsync(context.WorkflowExecutionContext, ActivityId);
            return Done();
        }
    }
}
