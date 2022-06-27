using System.Threading;
using Confluent.Kafka;

namespace Elsa.Activities.Kafka.Models
{
    public class KafkaMessageEvent
    {
        public Message<Ignore, string> Message { get; }
        public CancellationToken CancellationToken { get; }

        public KafkaMessageEvent(Message<Ignore, string> message, CancellationToken cancellationToken)
        {
            Message = message;
            CancellationToken = cancellationToken;
        }
    }
}

