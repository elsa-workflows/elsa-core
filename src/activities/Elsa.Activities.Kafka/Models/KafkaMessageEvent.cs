using System.Threading;
using Confluent.Kafka;

namespace Elsa.Activities.Kafka.Models
{
    public class KafkaMessageEvent
    {
        public Message<Ignore, byte[]> Message { get; }
        public CancellationToken CancellationToken { get; }

        public KafkaMessageEvent(Message<Ignore, byte[]> message, CancellationToken cancellationToken)
        {
            Message = message;
            CancellationToken = cancellationToken;
        }
    }
}

