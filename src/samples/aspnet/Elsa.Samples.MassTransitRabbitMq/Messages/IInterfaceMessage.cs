using System;

namespace Elsa.Samples.MassTransitRabbitMq.Messages
{
    public interface IInterfaceMessage
    {
        Guid CorrelationId { get; set; }
    }
}
