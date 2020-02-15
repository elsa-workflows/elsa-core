using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
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
        private readonly IWorkflowScheduler workflowScheduler;

        public TriggerSignal(IWorkflowScheduler workflowScheduler)
        {
            this.workflowScheduler = workflowScheduler;
        }

        [ActivityProperty(Hint = "An expression that evaluates to the name of the signal to trigger.")]
        public IWorkflowExpression<string> Signal
        {
            get => GetState<IWorkflowExpression<string>>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "An expression that evaluates to the correlation ID to use when signaling.")]
        public IWorkflowExpression<string>? CorrelationId
        {
            get => GetState<IWorkflowExpression<string>?>();
            set => SetState(value);
        }
        
        [ActivityProperty(Hint = "An expression that evaluates to an input value when triggering the signal.")]
        public IWorkflowExpression? Input
        {
            get => GetState<IWorkflowExpression?>();
            set => SetState(value);
        }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var signal = await context.EvaluateAsync(Signal, cancellationToken);
            var correlationId = await context.EvaluateAsync(CorrelationId, cancellationToken);
            var input = await context.EvaluateAsync(Input, cancellationToken);
            var triggeredSignal = new TriggeredSignal(signal, input);

            await workflowScheduler.TriggerWorkflowsAsync(
                nameof(Signaled),
                triggeredSignal,
                correlationId,
                cancellationToken: cancellationToken
            );

            return Done();
        }
    }
}