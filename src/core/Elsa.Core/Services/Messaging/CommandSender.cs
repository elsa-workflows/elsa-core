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

        public CommandSender(IServiceBusFactory serviceBusFactory)
        {
            _serviceBusFactory = serviceBusFactory;
        }

        public async Task SendAsync(object message, string? queueName = default, IDictionary<string, string>? headers = default, CancellationToken cancellationToken = default)
        {
            var bus = GetBus(message, queueName);
            await bus.Send(message, headers);
        }

        public async Task DeferAsync(object message, Duration delay, string? queueName = default, IDictionary<string, string>? headers = default, CancellationToken cancellationToken = default)
        {
            var bus = GetBus(message, queueName);
            await bus.Defer(delay.ToTimeSpan(), message, headers);
        }

        private IBus GetBus(object message, string? queueName) => _serviceBusFactory.GetServiceBus(message.GetType(), queueName);
    }
}