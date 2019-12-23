using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Workflows.Activities
{
    [ActivityDefinition(
        Category = "Workflows",
        Description = "Removes any blocking activities and sets the status of the workflow to Completed.",
        Icon = "fas fa-flag-checkered"
    )]
    public class Complete : Activity
    {
        [ActivityProperty(Hint = "An expression that evaluates to the workflow's output.'")]
        public IWorkflowExpression<Variable> WorkflowOutput
        {
            get => GetState<IWorkflowExpression<Variable>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var workflowOutput = await context.EvaluateAsync(WorkflowOutput, cancellationToken) ?? new Variable();
            
            context.WorkflowExecutionContext.Workflow.Output = workflowOutput;

            return Finish();
        }
    }
}