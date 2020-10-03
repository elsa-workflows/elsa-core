using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities.MassTransit.Consumers.MessageCorrelation;
using Elsa.Services;
using MassTransit;

namespace Elsa.Activities.MassTransit.Consumers
{
    public class WorkflowConsumer<T> : IConsumer<T> where T : class
    {
        private static readonly IList<ICorrelationIdSelector<T>> CorrelationIdSelectors =
            new ICorrelationIdSelector<T>[]
            {
                new CorrelatedByCorrelationIdSelector<T>(),
                new PropertyCorrelationIdSelector<T>("CorrelationId"),
                new PropertyCorrelationIdSelector<T>("EventId"),
                new PropertyCorrelationIdSelector<T>("CommandId")
            };

        private readonly IWorkflowScheduler workflowScheduler;

        public WorkflowConsumer(IWorkflowScheduler workflowScheduler)
        {
            this.workflowScheduler = workflowScheduler;
        }

        public async Task Consume(ConsumeContext<T> context)
        {
            var message = context.Message;
            var correlationId = default(Guid?);

            foreach (var item in CorrelationIdSelectors)
                if (item.TryGetCorrelationId(message, out correlationId))
                    break;

            await workflowScheduler.TriggerWorkflowsAsync<ReceiveMassTransitMessage>(
                message,
                correlationId?.ToString(),
                x => x.MessageType == message.GetType(),
                context.CancellationToken);
        }
    }
}