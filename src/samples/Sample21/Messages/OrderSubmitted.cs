using System;
using MassTransit;

namespace Sample21.Messages
{
    public class OrderSubmitted : CorrelatedBy<Guid>
    {
        public Guid CorrelationId => CartId;

        public Guid OrderId { get; set; }

        public DateTime Timestamp { get; set; }

        public Guid CartId { get; set; }

        public string UserName { get; set; }
    }
}