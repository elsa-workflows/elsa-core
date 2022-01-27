using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Services.Messaging
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IServiceBusFactory _serviceBusFactory;
        private readonly SemaphoreSlim _semaphore = new(1);

        public EventPublisher(IServiceBusFactory serviceBusFactory)
        {
            _serviceBusFactory = serviceBusFactory;
        }

        public async Task PublishAsync(object message, IDictionary<string, string>? headers = default)
        {
            await _semaphore.WaitAsync();
            
            try
            {
                var bus = _serviceBusFactory.GetServiceBus(message.GetType());
                await bus.Publish(message, headers);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}