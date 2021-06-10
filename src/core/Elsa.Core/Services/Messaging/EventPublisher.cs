using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elsa.Services.Messaging
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IServiceBusFactory _serviceBusFactory;

        public EventPublisher(IServiceBusFactory serviceBusFactory)
        {
            _serviceBusFactory = serviceBusFactory;
        }

        public async Task PublishAsync(object message, IDictionary<string, string>? headers = default)
        {
            var bus = await _serviceBusFactory.GetServiceBusAsync(message.GetType(), default);
            await bus.Publish(message, headers);
        }
    }
}