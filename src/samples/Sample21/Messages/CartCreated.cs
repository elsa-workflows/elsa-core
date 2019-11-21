using System;
using MassTransit;

namespace Sample21.Messages
{
    public class CartCreated : CorrelatedBy<Guid>
    {
        public Guid CorrelationId => CartId;

        public Guid CartId { get; set; }

        public DateTime Timestamp { get; set; }
        public string UserName { get; set; }
    }
}