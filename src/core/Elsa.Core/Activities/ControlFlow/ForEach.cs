using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using ScheduledActivity = Elsa.Services.Models.ScheduledActivity;

namespace Elsa.Activities.ControlFlow
{
    [ActivityDefinition(Category = "Control Flow", Description = "Iterate over a collection.", Icon = "far fa-circle")]
    public class ForEach : Activity
    {
        [ActivityProperty(Hint = "Enter an expression that evaluates to an array of items to iterate over.")]
        public IWorkflowExpression<IList<object>> Collection
        {
            get => GetState<IWorkflowExpression<IList<object>>>();
            set => SetState(value);
        }

        [Outlet(OutcomeNames.Iterate)]
        public IActivity Activity
        {
            get => GetState<IActivity>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var collection = await workflowExecutionContext.EvaluateAsync(Collection, activityExecutionContext, cancellationToken) ?? new object[0];
            var scheduledActivities = collection.Reverse().Select(x => new ScheduledActivity(Activity, Variable.From(x))).ToList();

            return Schedule(scheduledActivities);
        }
    }
}