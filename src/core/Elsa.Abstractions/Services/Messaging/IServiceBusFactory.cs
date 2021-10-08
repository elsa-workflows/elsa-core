using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;

namespace Elsa.Services
{
    public interface IServiceBusFactory
    {
        IBus ConfigureServiceBus(IEnumerable<Type> messageTypes, string queueName);
        IBus GetServiceBus(Type messageType, string? queueName = default);
    }
}