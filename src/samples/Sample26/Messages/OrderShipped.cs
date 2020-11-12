using System;
using Sample26.Models;

namespace Sample26.Messages
{
    public class OrderShipped
    {
        // Convention based property for message correlation
        // https://masstransit-project.com/MassTransit/usage/messages.html#message-correlation
        public Guid CorrelationId { get; set; }
        public Order Order { get; set; }
    }
}