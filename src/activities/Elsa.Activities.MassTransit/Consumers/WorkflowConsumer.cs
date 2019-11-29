using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities.MassTransit.Activities;
using Elsa.Activities.MassTransit.Consumers.MessageCorrelation;
using Elsa.Models;
using Elsa.Services;
using MassTransit;

namespace Elsa.Activities.MassTransit.Consumers
{
    public class WorkflowConsumer<T> : IConsumer<T> where T : class
    {
        private static readonly IList<ICorrelationIdSelector<T>> CorrelationIdSelectors = new ICorrelationIdSelector<T>[]
        {
            new CorrelatedByCorrelationIdSelector<T>(),
            new PropertyCorrelationIdSelector<T>("CorrelationId"),
            new PropertyCorrelationIdSelector<T>("EventId"),
            new PropertyCorrelationIdSelector<T>("CommandId")
        };

        private readonly IWorkflowRunner workflowRunner;

        public WorkflowConsumer(IWorkflowRunner workflowRunner)
        {
            this.workflowRunner = workflowRunner;
        }

        public async Task Consume(ConsumeContext<T> context)
        {
            var message = context.Message;
            var activityType = nameof(ReceiveMassTransitMessage);
            var input = new Variables();

            input.SetVariable(Constants.MessageInputKey, message);
            input.SetVariable(Constants.MessageTypeNameInputKey, typeof(T).AssemblyQualifiedName);

            Guid? correlationId = default;
            foreach (var item in CorrelationIdSelectors)
            {
                if (item.TryGetCorrelationId(message, out correlationId))
                    break;
            }

            await workflowRunner.TriggerAsync(
                activityType,
                input,
                correlationId.ToString(),
                x => ReceiveMassTransitMessage.GetMessageType(x) == message.GetType(),
                context.CancellationToken);
        }
    }
}