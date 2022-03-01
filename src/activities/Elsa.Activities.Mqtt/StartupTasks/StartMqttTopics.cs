using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Bookmarks;
using Elsa.Activities.Mqtt.Services;
using Elsa.Services;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.Mqtt.StartupTasks
{
    public class StartMqttTopics : IStartupTask
    {
        private readonly IMqttTopicsStarter _mqttTopicsStarter;
        private readonly IServiceProvider _services;
        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly ITriggerFinder _triggerFinder;

        public StartMqttTopics(IMqttTopicsStarter mqttTopicsStarter, IServiceProvider services, IBookmarkFinder bookmarkFinder, ITriggerFinder triggerFinder)
        {
            _mqttTopicsStarter = mqttTopicsStarter;
            _bookmarkFinder = bookmarkFinder;
            _triggerFinder = triggerFinder;
            _services = services;
        }

        public int Order => 2000;
        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Load bookmarks.
            var bookmarks = await _bookmarkFinder.FindBookmarksByTypeAsync<MessageReceivedBookmark>(cancellationToken: stoppingToken).ToList();

            // For each bookmark, start a worker.
            await _mqttTopicsStarter.CreateWorkersAsync(bookmarks, _services, stoppingToken);

            // Load triggers.
            var triggers = await _triggerFinder.FindTriggersByTypeAsync<MessageReceivedBookmark>(cancellationToken: stoppingToken).ToList();

            // For each trigger, start a worker.
            await _mqttTopicsStarter.CreateWorkersAsync(triggers, _services, stoppingToken);
        }
    }
}