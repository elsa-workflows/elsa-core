using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Options;
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

        public CreateSubscriptions(
            IServiceBusFactory serviceBusFactory,
            ElsaOptions elsaOptions,
            IContainerNameAccessor containerNameAccessor,
            IDistributedLockProvider distributedLockProvider)
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
                _competingMessageTypes.Add(new MessageTypeConfig(typeof(ExecuteWorkflowDefinitionRequest), ServiceBusOptions.FormatChannelQueueName("ExecuteWorkflow", workflowChannel)));
                _competingMessageTypes.Add(new MessageTypeConfig(typeof(ExecuteWorkflowInstanceRequest), ServiceBusOptions.FormatChannelQueueName("ExecuteWorkflow", workflowChannel)));
            }
        }

        public int Order => 900;

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await using var handle = await _distributedLockProvider.AcquireLockAsync(nameof(CreateSubscriptions), _elsaOptions.DistributedLockTimeout, cancellationToken);

            if (handle == null)
                throw new Exception("Could not acquire a lock within the maximum amount of time configured");

            var competingMessageTypeGroups = _competingMessageTypes.GroupBy(x => x.QueueName);

            foreach (var messageTypeGroup in competingMessageTypeGroups)
            {
                var queueName = messageTypeGroup.Key!;
                var messageTypes = messageTypeGroup.Select(x => x.MessageType).ToList();
                var bus = _serviceBusFactory.ConfigureServiceBus(messageTypes, queueName);

                foreach (var messageType in messageTypes)
                    await bus.Subscribe(messageType);
            }

            var containerName = _containerNameAccessor.GetContainerName();
            var pubSubMessageTypeGroups = _pubSubMessageTypes.GroupBy(x => x.QueueName);

            foreach (var messageTypeGroup in pubSubMessageTypeGroups)
            {
                var queueName = $"{containerName}:{messageTypeGroup.Key}";
                var messageTypes = messageTypeGroup.Select(x => x.MessageType).ToList();
                var bus = _serviceBusFactory.ConfigureServiceBus(messageTypes, queueName, true);

                foreach (var messageType in messageTypes)
                    await bus.Subscribe(messageType);
            }
        }
    }
}