using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.ActivityResults;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.AzureServiceBus.ActivityExecutionResults
{
    public class ServiceBusActionResult : ActivityExecutionResult
    {
        public ServiceBusActionResult(string? queue, string? topic, ServiceBusMessage message, bool delayed)
        {
            Queue = queue;
            Topic = topic;
            Message = message;
            Delayed = delayed;
        }

        public string? Queue { get; }
        public string? Topic { get; }
        public ServiceBusMessage Message { get; }
        public bool Delayed { get; }

        public override async ValueTask ExecuteAsync(ActivityExecutionContext activityExecutionContext, CancellationToken cancellationToken)
        {
            if (Delayed)
                activityExecutionContext.WorkflowExecutionContext.RegisterTask(SendAsync);
            else
                await SendAsync(activityExecutionContext.WorkflowExecutionContext, cancellationToken);
        }

        private async ValueTask SendAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken)
        {
            var factory = workflowExecutionContext.ServiceProvider.GetRequiredService<IMessageSenderFactory>();
            await using var sender = await factory.CreateSenderAsync(Queue, Topic, cancellationToken);
            await sender.SendMessageAsync(Message, cancellationToken);
        }
    }
}