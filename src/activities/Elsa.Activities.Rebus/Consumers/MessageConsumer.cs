using System.Threading.Tasks;
using Elsa.Activities.Rebus.Bookmarks;
using Elsa.Services;
using Humanizer;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Elsa.Activities.Rebus.Consumers
{
    public class MessageConsumer<T> : IHandleMessages<T> where T : notnull
    {
        // TODO: Design multi-tenancy. 
        private const string TenantId = default;

        private readonly ITriggersWorkflows _triggersWorkflows;

        public MessageConsumer(ITriggersWorkflows triggersWorkflows)
        {
            _triggersWorkflows = triggersWorkflows;
        }

        public async Task Handle(T message)
        {
            var correlationId = MessageContext.Current.TransportMessage.Headers.GetValueOrNull(Headers.CorrelationId);
            await _triggersWorkflows.TriggerWorkflowsAsync(
                nameof(RebusMessageReceived),
                new MessageReceivedBookmark { MessageType = message.GetType().Name, CorrelationId = correlationId },
                new MessageReceivedBookmark { MessageType = message.GetType().Name },
                correlationId,
                message,
                tenantId: TenantId);
        }
    }
}