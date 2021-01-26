using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Triggers;

namespace Elsa.Activities.Rebus.Triggers
{
    public class MessageReceivedTrigger : ITrigger
    {
        public string MessageType { get; set; } = default!;
        public string? CorrelationId { get; set; }
    }

    public class MessageReceivedTriggerProvider : WorkflowTriggerProvider<MessageReceivedTrigger, RebusMessageReceived>
    {
        public override async ValueTask<IEnumerable<ITrigger>> GetTriggersAsync(TriggerProviderContext<RebusMessageReceived> context, CancellationToken cancellationToken) =>
            new[]
            {
                new MessageReceivedTrigger
                {
                    MessageType = (await context.Activity.GetPropertyValueAsync(x => x.MessageType, cancellationToken))!.Name,
                    CorrelationId = context.ActivityExecutionContext.WorkflowExecutionContext.CorrelationId
                }
            };
    }
}