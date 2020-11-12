using System;
using MassTransit;

namespace Sample27.Messages
{
    public class CartRemovedEvent : CorrelatedBy<Guid>
    {
        public Guid CorrelationId => CartId;

        public Guid CartId { get; set; }
    }
}