using System.Threading;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
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
        private readonly IWorkflowScheduler _workflowScheduler;

        public TriggerSignal(IWorkflowScheduler workflowScheduler)
        {
            this._workflowScheduler = workflowScheduler;
        }

        [ActivityProperty(Hint = "An expression that evaluates to the name of the signal to trigger.")]
        public string Signal { get; set; } = default!;

        [ActivityProperty(Hint = "An expression that evaluates to the correlation ID to use when signaling.")]
        public string? CorrelationId { get; set; }
        
        [ActivityProperty(Hint = "An expression that evaluates to an input value when triggering the signal.")]
        public object? Input  { get; set; }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            var triggeredSignal = new TriggeredSignal(Signal, Input);

            await _workflowScheduler.TriggerWorkflowsAsync(
                nameof(Signaled),
                triggeredSignal,
                CorrelationId,
                cancellationToken: cancellationToken
            );

            return Done();
        }
    }
}