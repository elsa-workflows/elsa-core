using Rebus.Messages;
using Rebus.Transport;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq.Decorators
{
    public class MessageHandlingTransportDecorator : ITransport
    {
        static readonly Task<TransportMessage> NoMessage = Task.FromResult<TransportMessage>(null);

        readonly ITransport _transport;
        readonly MessageHandlingToggle _toggle;

        public MessageHandlingTransportDecorator(ITransport transport, MessageHandlingToggle toggle)
        {
            _transport = transport;
            _toggle = toggle;
        }

        public string Address => _transport.Address;

        public void CreateQueue(string address) => _transport.CreateQueue(address);

        public Task Send(string destinationAddress, TransportMessage message, ITransactionContext context)
            => _transport.Send(destinationAddress, message, context);

        public Task<TransportMessage> Receive(ITransactionContext context, CancellationToken cancellationToken)
            => _toggle.IsReceivingMessages
                ? _transport.Receive(context, cancellationToken)
                : NoMessage;
    }
}
