using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;

namespace Sample21.Messages
{
    public class CartItemAdded : CorrelatedBy<Guid>
    {
        public Guid CorrelationId => CardId;

        public Guid CardId { get; set; }

        public DateTime Timestamp { get; set; }
    }

    public class CartExpiredEvent : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; set; }
        public Guid CartId { get; set; }
    }

    public class CartRemovedEvent : CorrelatedBy<Guid>
    {
        public Guid CorrelationId => CartId;

        public Guid CartId { get; set; }
    }
}
