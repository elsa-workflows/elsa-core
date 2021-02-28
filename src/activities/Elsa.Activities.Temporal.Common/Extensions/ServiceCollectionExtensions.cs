using System;
using Elsa;
using Elsa.Activities.Temporal;
using Elsa.Activities.Temporal.Bookmarks;
using Elsa.Activities.Temporal.Handlers;
using Elsa.Activities.Temporal.Options;
using Elsa.Activities.Temporal.StartupTasks;
using Elsa.Runtime;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptions AddTimerActivities(this ElsaOptions options, Action<TimersOptions>? configure = default)
        {
            var timersOptions = new TimersOptions(options.Services);
            configure?.Invoke(timersOptions);

            options.Services
                .AddNotificationHandlers(typeof(RemoveScheduledTriggers))
                .AddStartupTask<StartJobs>()
                .AddBookmarkProvider<TimerBookmarkProvider>()
                .AddBookmarkProvider<CronBookmarkProvider>()
                .AddBookmarkProvider<StartAtBookmarkProvider>();

            return options
                .AddActivity<Cron>()
                .AddActivity<Timer>()
                .AddActivity<StartAt>()
                .AddActivity<ClearTimer>();
        }
    }
}