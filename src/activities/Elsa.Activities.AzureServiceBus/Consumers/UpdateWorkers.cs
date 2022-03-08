using System;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Events;
using Rebus.Handlers;

namespace Elsa.Activities.AzureServiceBus.Consumers
{
    public class UpdateWorkers : IHandleMessages<TriggerIndexingFinished>, IHandleMessages<TriggersDeleted>, IHandleMessages<BookmarkIndexingFinished>, IHandleMessages<BookmarksDeleted>
    {
        private readonly IWorkerManager _workerManager;
        private readonly IServiceProvider _services;

        public UpdateWorkers(IWorkerManager workerManager, IServiceProvider services)
        {
            _workerManager = workerManager;
            _services = services;
        }
        public async Task Handle(TriggerIndexingFinished message) => await _workerManager.CreateWorkersAsync(message.Triggers, _services);
        public async Task Handle(TriggersDeleted message) => await _workerManager.RemoveWorkersAsync(message.Triggers);
        public async Task Handle(BookmarkIndexingFinished message) => await _workerManager.CreateWorkersAsync(message.Bookmarks, _services);
        public async Task Handle(BookmarksDeleted message) => await _workerManager.CreateWorkersAsync(message.Bookmarks, _services);
    }
}