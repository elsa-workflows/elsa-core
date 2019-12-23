using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Scripting;
using Elsa.Scripting.JavaScript;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Workflows.Activities
{
    [ActivityDefinition(
        Category = "Workflows",
        Description = "Removes any blocking activities and sets the status of the workflow to Finished.",
        Icon = "fas fa-flag-checkered"
    )]
    public class Finish : Activity
    {
        [ActivityProperty(Hint = "An expression that evaluates to a dictionary to be set as the workflow's output.'")]
        public IWorkflowExpression<Variables> WorkflowScriptOutput
        {
            get => GetState<IWorkflowExpression<Variables>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var workflowOutput = await workflowContext.EvaluateAsync(WorkflowScriptOutput, cancellationToken) ?? new Variables();
            
            workflowContext.Workflow.Output = workflowOutput;

            return Finish();
        }
    }
}