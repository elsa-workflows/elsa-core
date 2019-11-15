using System.Threading.Tasks;
using Elsa.Activities.MassTransit.Activities;
using Elsa.Models;
using Elsa.Services;
using MassTransit;
using Newtonsoft.Json.Linq;

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
            var input = new Variables();

            input.SetVariable(Constants.MessageInputKey, message);
            input.SetVariable(Constants.MessageTypeNameInputKey, typeof(T).AssemblyQualifiedName);

            var correlationId = context.CorrelationId?.ToString();

            await workflowInvoker.TriggerAsync(
                activityType,
                input,
                correlationId,
                x => ReceiveMassTransitMessage.GetMessageType(x) == message.GetType(),
                context.CancellationToken);
        }
    }
}