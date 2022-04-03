using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Services;
using MediatR;

namespace Elsa.Handlers
{
    public class PublishDomainEventsToServiceBus :
        INotificationHandler<TriggerIndexingFinished>,
        INotificationHandler<TriggersDeleted>,
        INotificationHandler<BookmarkIndexingFinished>,
        INotificationHandler<BookmarksDeleted>
    {
        private readonly IEventPublisher _eventPublisher;

        public PublishDomainEventsToServiceBus(IEventPublisher eventPublisher) => _eventPublisher = eventPublisher;
        public async Task Handle(TriggerIndexingFinished notification, CancellationToken cancellationToken) => await _eventPublisher.PublishAsync(notification, cancellationToken: cancellationToken);
        public async Task Handle(TriggersDeleted notification, CancellationToken cancellationToken) => await _eventPublisher.PublishAsync(notification, cancellationToken: cancellationToken);
        public async Task Handle(BookmarkIndexingFinished notification, CancellationToken cancellationToken) => await _eventPublisher.PublishAsync(notification, cancellationToken: cancellationToken);
        public async Task Handle(BookmarksDeleted notification, CancellationToken cancellationToken) => await _eventPublisher.PublishAsync(notification, cancellationToken: cancellationToken);
    }
}