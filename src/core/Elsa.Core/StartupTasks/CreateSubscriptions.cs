using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.StartupTasks
{
    public class CreateSubscriptions : IStartupTask
    {
        private readonly IServiceBusFactory _serviceBusFactory;
        private readonly IContainerNameAccessor _containerNameAccessor;
        private readonly IList<CompetingMessageType> _competingMessageTypes;
        private readonly IEnumerable<Type> _pubSubMessageTypes;

        public CreateSubscriptions(IServiceBusFactory serviceBusFactory, ElsaOptions elsaOptions, IContainerNameAccessor containerNameAccessor)
        {
            _serviceBusFactory = serviceBusFactory;
            _containerNameAccessor = containerNameAccessor;
            _competingMessageTypes = elsaOptions.CompetingMessageTypes.ToList();
            _pubSubMessageTypes = elsaOptions.PubSubMessageTypes;

            var workflowChannelOptions = elsaOptions.WorkflowChannelOptions;
            var workflowChannels = workflowChannelOptions.Channels.ToList();

            // For each workflow channel, register a competing message type for workflow definition and workflow instance consumers.
            foreach (var workflowChannel in workflowChannels)
            {
                _competingMessageTypes.Add(new CompetingMessageType(typeof(ExecuteWorkflowDefinitionRequest), ElsaOptions.FormatChannelQueueName<ExecuteWorkflowDefinitionRequest>(workflowChannel)));
                _competingMessageTypes.Add(new CompetingMessageType(typeof(ExecuteWorkflowInstanceRequest), ElsaOptions.FormatChannelQueueName<ExecuteWorkflowInstanceRequest>(workflowChannel)));
            }
        }

        public int Order => 900;

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            foreach (var messageType in _competingMessageTypes)
            {
                var bus = await _serviceBusFactory.GetServiceBusAsync(messageType.MessageType, messageType.Queue, cancellationToken);
                await bus.Subscribe(messageType.MessageType);
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