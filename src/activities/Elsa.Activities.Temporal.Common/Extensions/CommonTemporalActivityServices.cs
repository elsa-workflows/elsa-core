using System;
using Elsa.Activities.Temporal.Common.Bookmarks;
using Elsa.Activities.Temporal.Common.Handlers;
using Elsa.Activities.Temporal.Common.HostedServices;
using Elsa.Activities.Temporal.Common.Options;
using Elsa.HostedServices;
using Elsa.Runtime;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Temporal
{
    public static class CommonTemporalActivityServices
    {
        /// <summary>
        /// Adds services which are common to temporal activities.  This method is intended for internal use only.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Instead of calling this method directly, when setting up Elsa for temporal (time-based) activities,
        /// developers should make use of one of the implementation-specific add-temporal-activities methods.
        /// Without providing an implementation-specific configuration in <paramref name="configure"/>, this method
        /// will not fully set up the temporal activities.
        /// </para>
        /// </remarks>
        /// <param name="options">Elsa options</param>
        /// <param name="configure">The configuration for temporal activity options</param>
        public static ElsaOptionsBuilder AddCommonTemporalActivities(this ElsaOptionsBuilder options, Action<TimersOptions>? configure = default)
        {
            var timersOptions = new TimersOptions(options.Services);
            configure?.Invoke(timersOptions);

            options.Services
                .AddNotificationHandlers(typeof(RemoveScheduledTriggers))
                .AddHostedService<ScopedBackgroundService<StartJobs>>()
                .AddBookmarkProvider<TimerBookmarkProvider>()
                .AddBookmarkProvider<CronBookmarkProvider>()
                .AddBookmarkProvider<StartAtBookmarkProvider>();

            options
                .AddActivity<Cron>()
                .AddActivity<Timer>()
                .AddActivity<StartAt>()
                .AddActivity<ClearTimer>();

            return options;
        }
    }
}