using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;
using Newtonsoft.Json;
using NodaTime;
using Rebus.Bus;

namespace Elsa.Services.Messaging
{
    public class CommandSender : ICommandSender
    {
        private readonly IServiceBusFactory _serviceBusFactory;
        private readonly ITenantProvider _tenantProvider;

        public CommandSender(IServiceBusFactory serviceBusFactory, ITenantProvider tenantProvider)
        {
            _serviceBusFactory = serviceBusFactory;
            _tenantProvider = tenantProvider;
        }

        public async Task SendAsync(object message, string? queueName = default, IDictionary<string, string>? headers = default, CancellationToken cancellationToken = default)
        {
            var bus = GetBus(message, queueName);

            headers = await AddTenantHeader(headers);

            await bus.Send(message, headers);
        }

        public async Task DeferAsync(object message, Duration delay, string? queueName = default, IDictionary<string, string>? headers = default, CancellationToken cancellationToken = default)
        {
            var bus = GetBus(message, queueName);

            headers = await AddTenantHeader(headers);

            await bus.Defer(delay.ToTimeSpan(), message, headers);
        }

        private IBus GetBus(object message, string? queueName) => _serviceBusFactory.GetServiceBus(message.GetType(), queueName);

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