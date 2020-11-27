using System.Threading.Tasks;
using Elsa.Activities.Rebus.Triggers;
using Elsa.Services;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Elsa.Activities.Rebus.Consumers
{
    public class MessageConsumer<T> : IHandleMessages<T> where T:notnull
    {
        private readonly IWorkflowRunner _workflowRunner;

        public MessageConsumer(IWorkflowRunner workflowRunner)
        {
            _workflowRunner = workflowRunner;
        }

        public async Task Handle(T message)
        {
            var correlationId = MessageContext.Current.TransportMessage.Headers.GetValueOrNull(Headers.CorrelationId);
            await _workflowRunner.TriggerWorkflowsAsync<MessageReceivedTrigger>(
                x => x.MessageType == typeof(T).Name && (x.CorrelationId == null || x.CorrelationId == correlationId), 
                message, 
                correlationId);
        }
    }
}