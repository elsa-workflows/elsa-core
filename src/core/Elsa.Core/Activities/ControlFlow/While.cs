using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow
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

        [Outlet(OutcomeNames.Iterate)]
        public IActivity Activity
        {
            get => GetState<IActivity>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var loop = await workflowExecutionContext.EvaluateAsync(Condition, activityExecutionContext, cancellationToken);

            if (loop)
                return Schedule(new[] { this, Activity }, activityExecutionContext.Input);

            return Done();
        }
    }
}