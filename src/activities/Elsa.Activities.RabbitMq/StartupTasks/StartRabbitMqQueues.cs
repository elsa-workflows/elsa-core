using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;
using Elsa.Activities.RabbitMq.Bookmarks;
using Elsa.Activities.RabbitMq.Services;
using Elsa.Services;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.RabbitMq.StartupTasks
{
    public class StartRabbitMqQueues : IStartupTask
    {
        private readonly IRabbitMqQueueStarter _rabbitMqQueueStarter;
        private readonly IServiceProvider _services;
        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly ITriggerFinder _triggerFinder;

        public StartRabbitMqQueues(IRabbitMqQueueStarter rabbitMqQueueStarter, IServiceProvider services, IBookmarkFinder bookmarkFinder, ITriggerFinder triggerFinder)
        {
            _rabbitMqQueueStarter = rabbitMqQueueStarter;
            _services = services;
            _bookmarkFinder = bookmarkFinder;
            _triggerFinder = triggerFinder;
        }

        public int Order => 2000;
        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Load bookmarks.
            var bookmarks = await _bookmarkFinder.FindBookmarksByTypeAsync<MessageReceivedBookmark>(cancellationToken: stoppingToken).ToList();

            // For each bookmark, start a worker.
            await _rabbitMqQueueStarter.CreateWorkersAsync(bookmarks, _services, stoppingToken);

            // Load triggers.
            var triggers = await _triggerFinder.FindTriggersByTypeAsync<MessageReceivedBookmark>(cancellationToken: stoppingToken).ToList();

            // For each trigger, start a worker.
            await _rabbitMqQueueStarter.CreateWorkersAsync(triggers, _services, stoppingToken);
        }
    }
}