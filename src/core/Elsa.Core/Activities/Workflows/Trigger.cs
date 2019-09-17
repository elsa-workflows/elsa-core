using System.Threading;
using System.Threading.Tasks;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Workflows
{
    /// <summary>
    /// Triggers the specified signal.
    /// </summary>
    public class Trigger : Activity
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;
        private readonly IWorkflowInvoker workflowInvoker;

        public Trigger(IWorkflowExpressionEvaluator expressionEvaluator, IWorkflowInvoker workflowInvoker)
        {
            this.expressionEvaluator = expressionEvaluator;
            this.workflowInvoker = workflowInvoker;
        }

        public WorkflowExpression<string> Signal
        {
            get => GetState<WorkflowExpression<string>>();
            set => SetState(value);
        }

        public WorkflowExpression<Variables> Input
        {
            get => GetState(() => new WorkflowExpression<Variables>(JavaScriptEvaluator.SyntaxName, "{}"));
            set => SetState(value);
        }

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

            input["Signal"] = signal;

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