using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Elsa.Samples.MassTransitRabbitMq.Messages
{
    public interface IInterfaceMessage
    {
        Guid CorrelationId { get; set; }
    }
}
