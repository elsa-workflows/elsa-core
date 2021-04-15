using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Services;
using Elsa.Triggers;
using MediatR;

namespace Elsa.Handlers
{
    public class PublishWorkflowDefinitionEvents : INotificationHandler<WorkflowDefinitionPublished>, INotificationHandler<WorkflowDefinitionRetracted>
    {
        private readonly IEventPublisher _eventPublisher;

        public PublishWorkflowDefinitionEvents(IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        public async Task Handle(WorkflowDefinitionPublished notification, CancellationToken cancellationToken) => await _eventPublisher.PublishAsync(notification);
        public async Task Handle(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken) => await _eventPublisher.PublishAsync(notification);
    }
}