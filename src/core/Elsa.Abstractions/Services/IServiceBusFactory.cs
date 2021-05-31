using System;
using System.Threading;
using System.Threading.Tasks;
using Rebus.Bus;

namespace Elsa.Services
{
    public interface IServiceBusFactory
    {
        Task<IBus> GetServiceBusAsync(Type messageType, string queueName, CancellationToken cancellationToken = default);
        Task<IBus> GetServiceBusAsync(Type messageType, CancellationToken cancellationToken = default);
    }
}