using System.Threading.Tasks;
using Elsa.Activities.Rebus.Bookmarks;
using Elsa.Services;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Elsa.Activities.Rebus.Consumers
{
    public class MessageConsumer<T> : IHandleMessages<T> where T:notnull
    {
        // TODO: Figure out how to start jobs across multiple tenants / how to get a list of all tenants. 
        private const string TenantId = default;
        
        private readonly IWorkflowRunner _workflowRunner;

        public MessageConsumer(IWorkflowRunner workflowRunner)
        {
            _workflowRunner = workflowRunner;
        }

        public async Task Handle(T message)
        {
            var correlationId = MessageContext.Current.TransportMessage.Headers.GetValueOrNull(Headers.CorrelationId);
            await _workflowRunner.TriggerWorkflowsAsync<RebusMessageReceived>(
                new MessageReceivedBookmark{ MessageType = message.GetType().Name, CorrelationId = correlationId},
                TenantId,
                message, 
                correlationId);
        }
    }
}