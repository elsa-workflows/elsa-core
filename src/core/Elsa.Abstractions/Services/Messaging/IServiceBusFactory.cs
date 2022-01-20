using System;
using System.Collections.Generic;
using Rebus.Bus;

namespace Elsa.Services
{
    public interface IServiceBusFactory
    {
        IBus ConfigureServiceBus(IEnumerable<Type> messageTypes, string queueName);
        IBus GetServiceBus(Type messageType, string? queueName = default);
    }
}