using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rebus.Exceptions;

namespace Elsa.Services.Messaging
{
    public class EventPublisher : IEventPublisher
    {
        private readonly IServiceBusFactory _serviceBusFactory;
        private readonly ILogger<EventPublisher> _logger;
        private readonly ITenantProvider _tenantProvider;

        public EventPublisher(IServiceBusFactory serviceBusFactory, ILogger<EventPublisher> logger, ITenantProvider tenantProvider)
        {
            _serviceBusFactory = serviceBusFactory;
            _logger = logger;
            _tenantProvider = tenantProvider;
        }

        public async Task PublishAsync(object message, IDictionary<string, string>? headers = default)
        {
            try
            {
                var bus = _serviceBusFactory.GetServiceBus(message.GetType());

                headers = await AddTenantHeader(headers);

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

        private async Task<IDictionary<string, string>> AddTenantHeader(IDictionary<string, string>? headers)
        {
            if (headers == null)
                headers = new Dictionary<string, string>();

            var tenant = await _tenantProvider.GetCurrentTenantAsync();
            var serializedTenant = JsonConvert.SerializeObject(tenant);

            headers.Add("tenant", serializedTenant);

            return headers;
        }
    }
}