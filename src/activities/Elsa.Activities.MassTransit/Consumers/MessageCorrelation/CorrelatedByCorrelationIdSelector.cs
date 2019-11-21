using System;
using MassTransit;
using MassTransit.Internals.Extensions;
using MassTransit.Internals.Reflection;

namespace Elsa.Activities.MassTransit.Consumers.MessageCorrelation
{
    public class CorrelatedByCorrelationIdSelector<T>
        : ICorrelationIdSelector<T>
        where T : class
    {
        public bool TryGetCorrelationId(T message, out Guid? correlationId)
        {
            var correlatedByInterface = typeof(T).GetInterface<CorrelatedBy<Guid>>();
            if (correlatedByInterface != null)
            {
                var property = ReadPropertyCache<CorrelatedBy<Guid>>.GetProperty<Guid>(nameof(CorrelatedBy<Guid>.CorrelationId));
                var correlatedBy = (CorrelatedBy<Guid>)message;
                correlationId = property.Get(correlatedBy);
                return true;
            }

            correlationId = null;
            return false;
        }
    }
}