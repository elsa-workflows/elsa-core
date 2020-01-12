using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        private readonly IWorkflowHost workflowHost;

        public WorkflowConsumer(IWorkflowHost workflowHost)
        {
            this.workflowHost = workflowHost;
        }

        public async Task Consume(ConsumeContext<T> context)
        {
            var message = context.Message;
            var activityType = nameof(ReceiveMassTransitMessage);
            var correlationId = default(Guid?);
            
            foreach (var item in CorrelationIdSelectors)
            {
                if (item.TryGetCorrelationId(message, out correlationId))
                    break;
            }

            await workflowHost.TriggerAsync(
                activityType,
                Variable.From(message),
                correlationId?.ToString(),
                x => ReceiveMassTransitMessage.GetMessageType(x) == message.GetType(),
                context.CancellationToken);
        }
    }
}