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

        public async Task SendAsync(object message, string? queue = default, IDictionary<string, string>? headers = default, CancellationToken cancellationToken = default)
        {
            var bus = await GetBusAsync(message, queue, cancellationToken);
            await bus.Send(message, headers);
        }
        
        public async Task DeferAsync(object message, Duration delay, string? queue = default, IDictionary<string, string>? headers = default, CancellationToken cancellationToken = default)
        {
            var bus = await GetBusAsync(message, queue, cancellationToken);
            await bus.Defer(delay.ToTimeSpan(), message, headers);
        }
        
        private async Task<IBus> GetBusAsync(object message, string? queue, CancellationToken cancellationToken) => await _serviceBusFactory.GetServiceBusAsync(message.GetType(), queue, cancellationToken);
    }
}