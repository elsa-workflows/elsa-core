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
    /// Sends the specified signal.
    /// </summary>
    [Action(
        Category = "Workflows",
        Description = "Sends the specified signal.",
        Outcomes = new[] { OutcomeNames.Done }
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

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            await _signaler.SendSignalAsync(Signal, Input, CorrelationId, context.CancellationToken);
            return Done();
        }
    }
}