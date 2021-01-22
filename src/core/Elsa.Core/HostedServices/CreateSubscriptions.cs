using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Microsoft.Extensions.Hosting;

namespace Elsa.HostedServices
{
    public class CreateSubscriptions : IHostedService
    {
        private readonly IServiceBusFactory _serviceBusFactory;
        private readonly IEnumerable<Type> _messageTypes;

        public CreateSubscriptions(IServiceBusFactory serviceBusFactory, ElsaOptions elsaOptions)
        {
            _serviceBusFactory = serviceBusFactory;
            _messageTypes = elsaOptions.MessageTypes;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var messageType in _messageTypes)
            {
                var bus = await _serviceBusFactory.GetServiceBusAsync(messageType, cancellationToken);
                await bus.Subscribe(messageType);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}