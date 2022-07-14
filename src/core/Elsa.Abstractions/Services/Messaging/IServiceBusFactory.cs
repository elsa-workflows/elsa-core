using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;

namespace Elsa.Services
{
    public interface IServiceBusFactory
    {
        IBus ConfigureServiceBus(IEnumerable<Type> messageTypes, string queueName, bool autoCleanup = false);
        Task<IBus> GetServiceBusAsync(Type messageType, string? queueName = default, CancellationToken cancellationToken = default);
        Task DisposeServiceBusAsync(IBus bus, CancellationToken cancellationToken = default);
    }
}