using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.ControlFlow
{
    [ActivityDefinition(
        Category = "Workflows",
        Description = "Removes any blocking activities and sets the status of the workflow to Completed.",
        Icon = "fas fa-flag-checkered"
    )]
    public class Complete : Activity
    {
        [ActivityProperty(Hint = "An expression that evaluates to the activity's output.'")]
        public IWorkflowExpression<Variable> Output
        {
            get => GetState<IWorkflowExpression<Variable>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var output = await workflowExecutionContext.EvaluateAsync(Output, activityExecutionContext, cancellationToken) ?? new Variable();
            
            workflowExecutionContext.BlockingActivities.Clear();
            workflowExecutionContext.Status = WorkflowStatus.Completed;
            
            return Done(output);
        }
    }
}