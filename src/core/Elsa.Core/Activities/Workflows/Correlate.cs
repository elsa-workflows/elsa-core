using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Workflows
{
    /// <summary>
    /// Sets the CorrelationId of the workflow to a given value.
    /// </summary>
    public class Correlate : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public Correlate(IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.expressionEvaluator = expressionEvaluator;
        }

        public WorkflowExpression<string> Expression
        {
            get => GetState<WorkflowExpression<string>>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var value = await expressionEvaluator.EvaluateAsync(Expression, workflowContext, cancellationToken);
            workflowContext.Workflow.CorrelationId = value;
            return Done();
        }
    }
}