using System;
using MassTransit;

namespace Sample21.Messages
{
    public class CartItemAdded : CorrelatedBy<Guid>
    {
        public Guid CorrelationId => CartId;

        public Guid CartId { get; set; }

        public string UserName { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
