using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using MassTransit;
using Sample08.Activities;
using Sample08.Messages;

namespace Sample08.Consumers
{
    public class CreateOrderConsumer : IConsumer<CreateOrder>
    {
        private readonly IWorkflowInvoker workflowInvoker;

        public CreateOrderConsumer(IWorkflowInvoker workflowInvoker)
        {
            this.workflowInvoker = workflowInvoker;
        }
        
        public async Task Consume(ConsumeContext<CreateOrder> context)
        {
            var message = context.Message;
            var activityType = nameof(ReceiveMassTransitMessage<CreateOrder>);
            var input = new Variables { ["message"] = message };
            
            await workflowInvoker.TriggerAsync(activityType, input, context.CancellationToken);
        }
    }
}