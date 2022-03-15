using System;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Events;
using Elsa.MultiTenancy;
using Rebus.Handlers;
using Rebus.Pipeline;

namespace Elsa.Activities.AzureServiceBus.Consumers
{
    public class UpdateWorkers : MultitenantConsumer, IHandleMessages<TriggerIndexingFinished>, IHandleMessages<TriggersDeleted>, IHandleMessages<BookmarkIndexingFinished>, IHandleMessages<BookmarksDeleted>
    {
        private readonly IWorkerManager _workerManager;

        public UpdateWorkers(IWorkerManager workerManager, IMessageContext messageContext, IServiceProvider serviceProvider) : base(messageContext, serviceProvider) => _workerManager = workerManager;

        public async Task Handle(TriggerIndexingFinished message) => await _workerManager.CreateWorkersAsync(message.Triggers, _serviceProvider);
        public async Task Handle(TriggersDeleted message) => await _workerManager.RemoveWorkersAsync(message.Triggers);
        public async Task Handle(BookmarkIndexingFinished message) => await _workerManager.CreateWorkersAsync(message.Bookmarks, _serviceProvider);
        public async Task Handle(BookmarksDeleted message) => await _workerManager.CreateWorkersAsync(message.Bookmarks, _serviceProvider);
    }
}