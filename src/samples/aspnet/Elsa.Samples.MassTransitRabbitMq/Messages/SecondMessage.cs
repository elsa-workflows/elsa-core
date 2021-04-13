using System;

namespace Elsa.Samples.MassTransitRabbitMq.Messages
{
    public class FirstMessage
    {    
        public Guid CorrelationId { get; private set; }

        public FirstMessage()
        {
            CorrelationId = Guid.Parse("e9ca46dd-36b9-4fc4-b7db-3bb7190e4488");
        }
    }
}