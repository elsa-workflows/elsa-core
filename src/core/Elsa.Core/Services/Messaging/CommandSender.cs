using System.Collections.Generic;
using System.Threading.Tasks;
using NodaTime;

namespace Elsa.Services.Messaging
{
    public class CommandSender : ICommandSender
    {
        private readonly IServiceBusFactory _serviceBusFactory;

        public CommandSender(IServiceBusFactory serviceBusFactory)
        {
            _serviceBusFactory = serviceBusFactory;
        }
        
        public async Task SendAsync(object message, IDictionary<string, string>? headers = default)
        {
            var bus = await _serviceBusFactory.GetServiceBusAsync(message.GetType(), default);
            await bus.Send(message, headers);
        }
        
        public async Task DeferAsync(object message, Duration delay, IDictionary<string, string>? headers = default)
        {
            var bus = await _serviceBusFactory.GetServiceBusAsync(message.GetType(), default);
            await bus.Defer(delay.ToTimeSpan(), message, headers);
        }
    }
}