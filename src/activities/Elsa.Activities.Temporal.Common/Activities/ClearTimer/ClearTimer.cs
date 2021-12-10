using System.Linq;
using System.Threading.Tasks;
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
        [ActivityInput(Hint = "The ID of the timer (Cron, StartAt, Timer) activity, which is to be cleared.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string ActivityId { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            // Remove blocking activity.
            var blockingActivity = context.WorkflowInstance.BlockingActivities.FirstOrDefault(x => x.ActivityId == ActivityId);

            if (blockingActivity != null)
                await context.WorkflowExecutionContext.RemoveBlockingActivityAsync(blockingActivity);

            return Done();
        }
    }
}