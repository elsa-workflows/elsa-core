using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow.Activities
{
    [ActivityDefinition(Category = "Control Flow", Description = "Execute while a given condition is true.", Icon = "far fa-circle")]
    public class While : Activity
    {
        [ActivityProperty(Hint = "Enter an expression that evaluates to a boolean value.")]
        public IWorkflowExpression<bool> Condition
        {
            get => GetState<IWorkflowExpression<bool>>();
            set => SetState(value);
        }

        public IActivity Activity
        {
            get => GetState<IActivity>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var loop = await context.EvaluateAsync(Condition, cancellationToken);
            var results = new List<IActivityExecutionResult>();

            if (loop)
                results.Add(ScheduleActivities(new[] { this, Activity }));
            else
                results.Add(Done());

            return Combine(results);
        }
    }
}