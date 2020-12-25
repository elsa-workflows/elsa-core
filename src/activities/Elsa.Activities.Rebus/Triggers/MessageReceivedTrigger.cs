using System.Threading;
using System.Threading.Tasks;
using Elsa.Triggers;

namespace Elsa.Activities.Rebus.Triggers
{
    public class MessageReceivedTrigger : Trigger
    {
        public string MessageType { get; set; } = default!;
        public string? CorrelationId { get; set; }
    }

    public class MessageReceivedTriggerProvider : TriggerProvider<MessageReceivedTrigger, RebusMessageReceived>
    {
        public override async ValueTask<ITrigger> GetTriggerAsync(TriggerProviderContext<RebusMessageReceived> context, CancellationToken cancellationToken) =>
            new MessageReceivedTrigger
            {
                MessageType = (await context.Activity.GetPropertyValueAsync(x => x.MessageType, cancellationToken))!.Name,
                CorrelationId = context.ActivityExecutionContext.WorkflowExecutionContext.CorrelationId
            };
    }
}