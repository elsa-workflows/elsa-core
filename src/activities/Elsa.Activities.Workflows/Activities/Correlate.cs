using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Scripting;
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
        [ActivityProperty(Hint = "An expression that evaluates to the value to store as the correlation ID.")]
        public IWorkflowExpression<string> ValueScriptExpression
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var value = await workflowContext.EvaluateAsync(ValueScriptExpression, cancellationToken);
            workflowContext.Workflow.CorrelationId = value;
            return Done();
        }
    }
}