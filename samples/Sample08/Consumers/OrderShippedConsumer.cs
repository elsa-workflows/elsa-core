using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using MassTransit;
using Sample08.Activities;
using Sample08.Messages;

namespace Sample08.Consumers
{
    public class OrderShippedConsumer : IConsumer<OrderShipped>
    {
        private readonly IWorkflowInvoker workflowInvoker;

        public OrderShippedConsumer(IWorkflowInvoker workflowInvoker)
        {
            this.workflowInvoker = workflowInvoker;
        }
        
        public async Task Consume(ConsumeContext<OrderShipped> context)
        {
            var message = context.Message;
            var activityType = nameof(ReceiveMassTransitMessage<OrderShipped>);
            var input = new Variables { ["message"] = message };
            
            await workflowInvoker.TriggerAsync(activityType, input, context.CancellationToken);
        }
    }
}