using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities.MassTransit.Bookmarks;
using Elsa.Activities.MassTransit.Consumers.MessageCorrelation;
using Elsa.Services;
using MassTransit;
using MediatR;

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

        private readonly IMediator _mediator;
        private readonly IWorkflowLaunchpad _workflowLaunchpad;

        public WorkflowConsumer(IMediator mediator, IWorkflowLaunchpad workflowLaunchpad)
        {
            _mediator = mediator;
            _workflowLaunchpad = workflowLaunchpad;
        }

        public async Task Consume(ConsumeContext<T> context)
        {
            var message = context.Message;
            var correlationId = default(Guid?);

            foreach (var item in CorrelationIdSelectors)
                if (item.TryGetCorrelationId(message, out correlationId))
                    break;

            var bookmark = new MessageReceivedBookmark
            {
                MessageType = message.GetType().Name,
                CorrelationId = correlationId.ToString()
            };
            var trigger = new MessageReceivedBookmark
            {
                MessageType = message.GetType().Name
            };

            await _workflowLaunchpad.CollectAndExecuteWorkflowsAsync(new CollectWorkflowsContext(
                nameof(ReceiveMassTransitMessage),
                bookmark,
                trigger,
                correlationId.ToString()
            ));
        }
    }
}