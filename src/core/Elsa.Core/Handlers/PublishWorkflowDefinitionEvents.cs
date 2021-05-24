using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Services;
using MediatR;

namespace Elsa.Handlers
{
    public class PublishWorkflowDefinitionEvents : INotificationHandler<WorkflowDefinitionPublished>, INotificationHandler<WorkflowDefinitionRetracted>, INotificationHandler<WorkflowDefinitionDeleted>
    {
        private readonly IEventPublisher _eventPublisher;
        public PublishWorkflowDefinitionEvents(IEventPublisher eventPublisher) => _eventPublisher = eventPublisher;
        public Task Handle(WorkflowDefinitionPublished notification, CancellationToken cancellationToken) => _eventPublisher.PublishAsync(notification);
        public Task Handle(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken) => _eventPublisher.PublishAsync(notification);
        public Task Handle(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken) => _eventPublisher.PublishAsync(notification);
    }
}