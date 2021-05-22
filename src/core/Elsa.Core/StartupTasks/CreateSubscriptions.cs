using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.StartupTasks
{
    public class CreateSubscriptions : IStartupTask
    {
        private readonly IServiceBusFactory _serviceBusFactory;
        private readonly IContainerNameAccessor _containerNameAccessor;
        private readonly IEnumerable<Type> _competingMessageTypes;
        private readonly IEnumerable<Type> _pubSubMessageTypes;

        public CreateSubscriptions(IServiceBusFactory serviceBusFactory, ElsaOptions elsaOptions, IContainerNameAccessor containerNameAccessor)
        {
            _serviceBusFactory = serviceBusFactory;
            _containerNameAccessor = containerNameAccessor;
            _competingMessageTypes = elsaOptions.CompetingMessageTypes;
            _pubSubMessageTypes = elsaOptions.PubSubMessageTypes;
        }

        public int Order => 900;

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            foreach (var messageType in _competingMessageTypes)
            {
                var bus = await _serviceBusFactory.GetServiceBusAsync(messageType, cancellationToken);
                await bus.Subscribe(messageType);
            }

            var containerName = _containerNameAccessor.GetContainerName();
            foreach (var messageType in _pubSubMessageTypes)
            {
                var queueName = $"{containerName}:{messageType.Name}";
                var bus = await _serviceBusFactory.GetServiceBusAsync(messageType, queueName, cancellationToken);
                await bus.Subscribe(messageType);
            }
        }
    }
}