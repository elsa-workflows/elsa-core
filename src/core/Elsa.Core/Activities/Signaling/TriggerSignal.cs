using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Models;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Signaling
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
        private readonly IWorkflowHost workflowHost;

        public TriggerSignal(IWorkflowHost workflowHost)
        {
            this.workflowHost = workflowHost;
        }

        [ActivityProperty(Hint = "An expression that evaluates to the name of the signal to trigger.")]
        public IWorkflowExpression<string> Signal
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "An expression that evaluates to the correlation ID to use when signaling.")]
        public IWorkflowExpression<string> CorrelationId
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var signal = await context.EvaluateAsync(Signal, cancellationToken);
            var correlationId = await context.EvaluateAsync(CorrelationId, cancellationToken);

            await workflowHost.TriggerAsync(
                nameof(Signaled),
                Variable.From(signal),
                correlationId,
                cancellationToken: cancellationToken
            );

            return Done();
        }
    }
}