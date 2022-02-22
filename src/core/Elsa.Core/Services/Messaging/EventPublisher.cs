using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rebus.Exceptions;

namespace Elsa.Services.Messaging
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IServiceBusFactory _serviceBusFactory;
        private readonly ILogger<EventPublisher> _logger;

        public EventPublisher(IServiceBusFactory serviceBusFactory, ILogger<EventPublisher> logger)
        {
            _serviceBusFactory = serviceBusFactory;
            _logger = logger;
        }

        public async Task PublishAsync(object message, IDictionary<string, string>? headers = default)
        {
            try
            {
                var bus = _serviceBusFactory.GetServiceBus(message.GetType());
                await bus.Publish(message, headers);
            }
            catch (RebusApplicationException e)
            {
                // This error is thrown sometimes when the transport tries to add another "OnCommitted" handler to the current transaction.
                // It looks like it's some form of race condition, and only seems to happen when publishing a message to all receivers (including the current bus)
                // Might reach out to @mookid8000 to get a better understanding
                _logger.LogWarning(e, "Failed to publish message {@Message}. This happens when the transaction context used by Rebus has already been completed. Should be fine", message);
            }
        }
    }
}