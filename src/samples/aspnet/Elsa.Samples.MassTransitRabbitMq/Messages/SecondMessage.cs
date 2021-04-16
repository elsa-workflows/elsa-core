using System;

namespace Elsa.Samples.MassTransitRabbitMq.Messages
{
    public class SecondMessage
    {    
        public Guid CorrelationId { get; private set; }

        public SecondMessage()
        {
            CorrelationId = Guid.Parse("e9ca46dd-36b9-4fc4-b7db-3bb7190e4488");
        }
    }
}