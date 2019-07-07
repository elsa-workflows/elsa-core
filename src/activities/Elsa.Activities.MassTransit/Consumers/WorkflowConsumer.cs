using System.Threading.Tasks;
using Elsa.Activities.MassTransit.Activities;
using Elsa.Models;
using Elsa.Services;
using MassTransit;

namespace Elsa.Activities.MassTransit.Consumers
{
    public class WorkflowConsumer<T> : IConsumer<T> where T : class
    {
        private readonly IWorkflowInvoker workflowInvoker;

        public WorkflowConsumer(IWorkflowInvoker workflowInvoker)
        {
            this.workflowInvoker = workflowInvoker;
        }
        
        public async Task Consume(ConsumeContext<T> context)
        {
            var message = context.Message;
            var activityType = nameof(ReceiveMassTransitMessage);
            var input = new Variables { ["message"] = message };
            
            await workflowInvoker.TriggerAsync(
                activityType, 
                input,
                x => ReceiveMassTransitMessage.GetMessageType(x) == message.GetType(),
                context.CancellationToken);
        }
    }
}