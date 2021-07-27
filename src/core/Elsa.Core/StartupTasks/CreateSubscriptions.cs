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
        private readonly ElsaOptions _elsaOptions;
        private readonly IContainerNameAccessor _containerNameAccessor;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly IList<MessageTypeConfig> _competingMessageTypes;
        private readonly IEnumerable<MessageTypeConfig> _pubSubMessageTypes;

        public CreateSubscriptions(IServiceBusFactory serviceBusFactory, ElsaOptions elsaOptions, IContainerNameAccessor containerNameAccessor, IDistributedLockProvider distributedLockProvider)
        {
            _serviceBusFactory = serviceBusFactory;
            _elsaOptions = elsaOptions;
            _containerNameAccessor = containerNameAccessor;
            _distributedLockProvider = distributedLockProvider;
            _competingMessageTypes = elsaOptions.CompetingMessageTypes.ToList();
            _pubSubMessageTypes = elsaOptions.PubSubMessageTypes;

            var workflowChannelOptions = elsaOptions.WorkflowChannelOptions;
            var workflowChannels = workflowChannelOptions.Channels.ToList();

            // For each workflow channel, register a competing message type for workflow definition and workflow instance consumers.
            foreach (var workflowChannel in workflowChannels)
            {
                _competingMessageTypes.Add(new MessageTypeConfig(typeof(ExecuteWorkflowDefinitionRequest), ElsaOptions.FormatChannelQueueName("ExecuteWorkflow", workflowChannel)));
                _competingMessageTypes.Add(new MessageTypeConfig(typeof(ExecuteWorkflowInstanceRequest), ElsaOptions.FormatChannelQueueName("ExecuteWorkflow", workflowChannel)));
            }
        }

        public int Order => 900;

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await using var handle = await _distributedLockProvider.AcquireLockAsync(nameof(CreateSubscriptions), _elsaOptions.DistributedLockTimeout, cancellationToken);

            if (handle == null)
                throw new Exception("Could not acquire a lock within the maximum amount of time configured");
            
            foreach (var messageType in _competingMessageTypes)
            {
                var bus = await _serviceBusFactory.GetServiceBusAsync(messageType.MessageType, messageType.QueueName, cancellationToken);
                await bus.Subscribe(messageType.MessageType);
            }

            var containerName = _containerNameAccessor.GetContainerName();
            foreach (var messageType in _pubSubMessageTypes)
            {
                var queueName = $"{containerName}:{messageType.QueueName ?? messageType.MessageType.Name}";
                var bus = await _serviceBusFactory.GetServiceBusAsync(messageType.MessageType, queueName, cancellationToken);
                await bus.Subscribe(messageType.MessageType);
            }
        }
    }
}