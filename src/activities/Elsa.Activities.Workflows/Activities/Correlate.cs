using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Workflows.Activities
{
    /// <summary>
    /// Sets the CorrelationId of the workflow to a given value.
    /// </summary>
    [ActivityDefinition(
        Category = "Workflows",
        Description = "Set the CorrelationId of the workflow to a given value.",
        Icon = "fas fa-link"
    )]
    public class Correlate : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public Correlate(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        [ActivityProperty(Hint = "An expression that evaluates to the value to store as the correlation ID.")]
        public WorkflowExpression<string> ValueExpression
        {
            get => GetState<WorkflowExpression<string>>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(
            WorkflowExecutionContext workflowContext,
            CancellationToken cancellationToken)
        {
            var value = await expressionEvaluator.EvaluateAsync(ValueExpression, workflowContext, cancellationToken);
            workflowContext.Workflow.CorrelationId = value;
            return Done();
        }
    }
}