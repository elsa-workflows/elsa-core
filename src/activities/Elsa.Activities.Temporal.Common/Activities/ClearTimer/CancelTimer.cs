using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Temporal
{
    [Trigger(Category = "Timers", Description = "Cancel a timer (Cron, StartAt, Timer) so that it is not executed.")]
    public class ClearTimer : Activity
    {
        private readonly IWorkflowScheduler _workflowScheduler;

        public ClearTimer(IWorkflowScheduler workflowScheduler)
        {
            _workflowScheduler = workflowScheduler;
        }

        [ActivityProperty(Hint = "The ID of the timer (Cron, StartAt, Timer) activity, which is to be cleared.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string ActivityId { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            await _workflowScheduler.UnscheduleWorkflowAsync(null, context.WorkflowInstance.Id, ActivityId, context.WorkflowInstance.TenantId, context.CancellationToken);
            return Done();
        }
    }
}