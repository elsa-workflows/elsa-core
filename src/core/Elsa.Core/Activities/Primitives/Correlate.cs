using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Primitives
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
        [ActivityProperty(Hint = "An expression that evaluates to the value to store as the correlation ID.")]
        public IWorkflowExpression<string> Value
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowExecutionContext, ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            var value = await workflowExecutionContext.EvaluateAsync(Value, activityExecutionContext, cancellationToken);
            return new CorrelateResult(value);
        }
    }
}