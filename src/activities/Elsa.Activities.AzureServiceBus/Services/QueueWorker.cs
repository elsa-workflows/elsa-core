using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Triggers;
using Elsa.Services;
using Microsoft.Azure.ServiceBus.Core;

namespace Elsa.Activities.AzureServiceBus.Services
{
    public class QueueWorker
    {
        private readonly IMessageReceiver _messageReceiver;
        private readonly IWorkflowScheduler _workflowScheduler;

        public QueueWorker(IMessageReceiver messageReceiver, IWorkflowScheduler workflowScheduler)
        {
            _messageReceiver = messageReceiver;
            _workflowScheduler = workflowScheduler;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await Task.Factory.StartNew(() => ReadQueueAsync(cancellationTokenSource.Token), cancellationToken);
        }

        private async Task ReadQueueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await _messageReceiver.ReceiveAsync();

                if(message == null)
                    continue;

                await _workflowScheduler.TriggerWorkflowsAsync<MessageReceivedTrigger>(x => x.QueueName == _messageReceiver.Path && (string.IsNullOrWhiteSpace(x.CorrelationId) || x.CorrelationId == message.CorrelationId), message,
                    message.CorrelationId, cancellationToken: cancellationToken);
            }
        }
    }
}