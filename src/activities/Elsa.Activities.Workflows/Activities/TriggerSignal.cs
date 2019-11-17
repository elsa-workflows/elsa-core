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
    /// <summary>
    /// Triggers the specified signal.
    /// </summary>
    [ActivityDefinition(
        Category = "Workflows",
        Description = "Trigger the specified signal.",
        Icon = "fas fa-broadcast-tower"
    )]
    public class TriggerSignal : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly IWorkflowInvoker workflowInvoker;

        public TriggerSignal(IWorkflowExpressionEvaluator expressionEvaluator, IWorkflowInvoker workflowInvoker)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.workflowInvoker = workflowInvoker;
        }

        [ActivityProperty(Hint = "An expression that evaluates to the name of the signal to trigger.")]
        public WorkflowExpression<string> Signal
        {
            get => GetState<WorkflowExpression<string>>();
            set => SetState(value);
        }

        [ActivityProperty(
            Hint = "An expression that evaluates to a dictionary to be provided as input when signaling."
        )]
        public WorkflowExpression<Variables> Input
        {
            get => GetState(() => new WorkflowExpression<Variables>(JavaScriptExpressionEvaluator.SyntaxName, "{}"));
            set => SetState(value);
        }

        [ActivityProperty(Hint = "An expression that evaluates to the correlation ID to use when signaling.")]
        public WorkflowExpression<string> CorrelationId
        {
            get => GetState(() => new WorkflowExpression<string>(LiteralEvaluator.SyntaxName, ""));
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(
            WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            var signal = await expressionEvaluator.EvaluateAsync(Signal, context, cancellationToken);
            var input = (await expressionEvaluator.EvaluateAsync(Input, context, cancellationToken)) ?? new Variables();
            var correlationId = await expressionEvaluator.EvaluateAsync(CorrelationId, context, cancellationToken);

            input.SetVariable("Signal", signal);

            await workflowInvoker.TriggerAsync(
                nameof(Signaled),
                input,
                correlationId,
                cancellationToken: cancellationToken
            );

            return Done();
        }
    }
}