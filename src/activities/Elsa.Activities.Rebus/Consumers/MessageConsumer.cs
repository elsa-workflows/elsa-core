using System.Threading.Tasks;
using Elsa.Activities.Rebus.Bookmarks;
using Elsa.Dispatch;
using MediatR;
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
        
        private readonly IMediator _mediator;
        
        public MessageConsumer(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task Handle(T message)
        {
            var correlationId = MessageContext.Current.TransportMessage.Headers.GetValueOrNull(Headers.CorrelationId);
            await _mediator.Send(new TriggerWorkflowsRequest(
                nameof(RebusMessageReceived),
                new MessageReceivedBookmark {MessageType = message.GetType().Name, CorrelationId = correlationId},
                new MessageReceivedBookmark {MessageType = message.GetType().Name},
                message,
                correlationId,
                default,
                TenantId));
        }
    }
}