using System;

namespace Elsa.Activities.MassTransit.Consumers.MessageCorrelation
{
    public interface ICorrelationIdSelector<in T>
        where T : class
    {
        bool TryGetCorrelationId(T message, out Guid? correlationId);
    }
}