using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Scripting.JavaScript.Services;
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
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public Finish(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        [ActivityProperty(Hint = "An expression that evaluates to a dictionary to be set as the workflow's output.'")]
        public WorkflowExpression<Variables> WorkflowOutput
        {
            get => GetState(() => new WorkflowExpression<Variables>(JavaScriptExpressionEvaluator.SyntaxName, "({})"));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(
            WorkflowExecutionContext workflowContext,
            CancellationToken cancellationToken)
        {
            var workflowOutput = await expressionEvaluator.EvaluateAsync(
                WorkflowOutput,
                workflowContext,
                cancellationToken
            );
            
            workflowContext.Workflow.Output = workflowOutput;

            return Finish();
        }
    }
}