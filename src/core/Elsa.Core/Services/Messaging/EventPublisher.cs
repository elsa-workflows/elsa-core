using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rebus.Bus;
using Rebus.Exceptions;

namespace Elsa.Services.Messaging
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IServiceBusFactory _serviceBusFactory;
        private readonly ILogger<EventPublisher> _logger;
        private readonly SemaphoreSlim _semaphore = new(1);

        public EventPublisher(IServiceBusFactory serviceBusFactory, ILogger<EventPublisher> logger)
        {
            _serviceBusFactory = serviceBusFactory;
            _logger = logger;
        }

        public async Task PublishAsync(object message, IDictionary<string, string>? headers = default, CancellationToken cancellationToken = default)
        {
            var bus = await GetBusAsync(message, cancellationToken);
            await _semaphore.WaitAsync(cancellationToken);
            
            // Attempt to prevent: Could not 'GetOrAdd' item with key 'new-azure-service-bus-transport' error.
            try
            {
                await bus.Publish(message, headers);
            }
            catch (RebusApplicationException e)
            {
                await _serviceBusFactory.DisposeServiceBusAsync(bus, cancellationToken);
                bus = await GetBusAsync(message, cancellationToken);
                await bus.Send(message, headers);
            }
            finally
            {
                _semaphore.Release();
            }
        }
        
        private Task<IBus> GetBusAsync(object message, CancellationToken cancellationToken) => _serviceBusFactory.GetServiceBusAsync(message.GetType(), cancellationToken: cancellationToken);
    }
}