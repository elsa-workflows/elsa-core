using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using Rebus.Bus;

namespace Elsa.Services.Messaging
{
    public class CommandSender : ICommandSender
    {
        private readonly IServiceBusFactory _serviceBusFactory;
        private readonly SemaphoreSlim _semaphore = new(1);

        public CommandSender(IServiceBusFactory serviceBusFactory)
        {
            _serviceBusFactory = serviceBusFactory;
        }

        public async Task SendAsync(object message, string? queueName = default, IDictionary<string, string>? headers = default, CancellationToken cancellationToken = default)
        {
            var bus = await GetBusAsync(message, queueName, cancellationToken);

            // Attempt to prevent: Could not 'GetOrAdd' item with key 'new-azure-service-bus-transport' error.
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                await bus.Send(message, headers);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DeferAsync(object message, Duration delay, string? queueName = default, IDictionary<string, string>? headers = default, CancellationToken cancellationToken = default)
        {
            var bus = await GetBusAsync(message, queueName, cancellationToken);
            await bus.Defer(delay.ToTimeSpan(), message, headers);
        }

        private Task<IBus> GetBusAsync(object message, string? queueName, CancellationToken cancellationToken) => _serviceBusFactory.GetServiceBusAsync(message.GetType(), queueName, cancellationToken);
    }
}