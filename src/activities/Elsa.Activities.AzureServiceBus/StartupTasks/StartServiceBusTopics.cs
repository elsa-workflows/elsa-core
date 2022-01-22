using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.AzureServiceBus.Bookmarks;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.AzureServiceBus.StartupTasks
{
    public class StartServiceBusTopics : BackgroundService
    {
        private readonly IServiceBusTopicsStarter _serviceBusTopicsStarter;
        private readonly IServiceScopeFactory _scopeFactory;
        
        public StartServiceBusTopics(IServiceBusTopicsStarter serviceBusTopicsStarter, IServiceScopeFactory scopeFactory)
        {
            _serviceBusTopicsStarter = serviceBusTopicsStarter;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var bookmarkFinder = scope.ServiceProvider.GetRequiredService<IBookmarkFinder>();
            
            // Load all TopicMessageReceivedBookmark bookmarks.
            var bookmarks = await bookmarkFinder.FindBookmarksByTypeAsync<TopicMessageReceivedBookmark>(cancellationToken: stoppingToken).ToList();

            // For each bookmark, start a worker.
            await _serviceBusTopicsStarter.CreateWorkersAsync(bookmarks, stoppingToken);
        }
    }
}