using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Signaling.Services;
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
        Description = "Sends the specified signal.",
        Icon = "fas fa-broadcast-tower"
    )]
    public class SendSignal : Activity
    {
        private readonly ISignaler _signaler;

        public SendSignal(ISignaler signaler)
        {
            _signaler = signaler;
        }

        [ActivityProperty(Hint = "An expression that evaluates to the name of the signal to trigger.")]
        public string Signal { get; set; } = default!;

        [ActivityProperty(Hint = "An expression that evaluates to the correlation ID to use when signaling.")]
        public string? CorrelationId { get; set; }

        [ActivityProperty(Hint = "An expression that evaluates to an input value when triggering the signal.")]
        public object? Input { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context, CancellationToken cancellationToken)
        {
            await _signaler.SendSignal(Signal, Input, CorrelationId, cancellationToken);
            return Done();
        }
    }
}