using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Services;
using MediatR;

namespace Elsa.Handlers
{
    public class PublishDomainEventsToServiceBus : 
        INotificationHandler<WorkflowDefinitionPublished>, 
        INotificationHandler<WorkflowDefinitionRetracted>, 
        INotificationHandler<WorkflowDefinitionDeleted>,
        INotificationHandler<TriggerIndexingFinished>,
        INotificationHandler<TriggersDeleted>,
        INotificationHandler<BookmarkIndexingFinished>,
        INotificationHandler<BookmarksDeleted>
    {
        private readonly IEventPublisher _eventPublisher;
        public PublishDomainEventsToServiceBus(IEventPublisher eventPublisher) => _eventPublisher = eventPublisher;
        public Task Handle(WorkflowDefinitionPublished notification, CancellationToken cancellationToken) => _eventPublisher.PublishAsync(notification);
        public Task Handle(WorkflowDefinitionRetracted notification, CancellationToken cancellationToken) => _eventPublisher.PublishAsync(notification);
        public Task Handle(WorkflowDefinitionDeleted notification, CancellationToken cancellationToken) => _eventPublisher.PublishAsync(notification);
        public Task Handle(TriggerIndexingFinished notification, CancellationToken cancellationToken) => _eventPublisher.PublishAsync(notification);
        public Task Handle(TriggersDeleted notification, CancellationToken cancellationToken) => _eventPublisher.PublishAsync(notification);
        public Task Handle(BookmarkIndexingFinished notification, CancellationToken cancellationToken) => _eventPublisher.PublishAsync(notification);
        public Task Handle(BookmarksDeleted notification, CancellationToken cancellationToken) => _eventPublisher.PublishAsync(notification);
    }
}