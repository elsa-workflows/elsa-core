using System.Threading.Tasks;
using Elsa.Activities.Signaling.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Persistence;
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
        private readonly IWorkflowInstanceStore _workflowInstanceStore;

        public SendSignal(ISignaler signaler, IWorkflowInstanceStore workflowInstanceStore)
        {
            _signaler = signaler;
            _workflowInstanceStore = workflowInstanceStore;
        }

        [ActivityInput(Hint = "An expression that evaluates to the name of the signal to trigger.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Signal { get; set; } = default!;

        [ActivityInput(Hint = "An expression that evaluates to the correlation ID to use when signaling.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string? CorrelationId { get; set; }

        [ActivityInput(Hint = "An expression that evaluates to an input value when triggering the signal.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public object? Input { get; set; }

        [ActivityInput(Hint = "The send-mode controls whether the signal should be sent asynchronously or synchronously.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public SendSignalMode SendMode { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            // Persist the workflow before sending the signal. This fixes a use case where a responding workflow sends a response signal handled by this workflow in a separate branch for example.
            await _workflowInstanceStore.SaveAsync(context.WorkflowInstance, context.CancellationToken);

            // Trigger the signal synchronously. If we dispatched the signal instead, we don't have to explicitly save the workflow instance. For future reconsideration.
            // Warning: Sending a signal directly instead of dispatching it can potentially cause a deadlock if another workflow sends a signal back to this workflow immediately.

            switch (SendMode)
            {
                case SendSignalMode.Synchronously:
                    await _signaler.TriggerSignalAsync(Signal, Input, correlationId: CorrelationId, cancellationToken: context.CancellationToken);
                    break;
                case SendSignalMode.Asynchronously:
                default:
                    await _signaler.DispatchSignalAsync(Signal, Input, correlationId: CorrelationId, cancellationToken: context.CancellationToken);
                    break;
            }

            return Done();
        }
    }
}