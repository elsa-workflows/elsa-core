using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elsa.Services
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
    }
}