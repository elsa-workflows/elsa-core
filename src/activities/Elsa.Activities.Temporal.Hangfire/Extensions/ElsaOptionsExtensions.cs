using System;
using Hangfire;
using Elsa.Activities.Temporal;

// ReSharper disable once CheckNamespace
namespace Elsa
{
    public static class ElsaOptionsExtensions
    {
        /// <summary>
        /// Adds temporal (time-based) activities to Elsa, using the Hangfire implementation.
        /// </summary>
        /// <param name="options">Elsa options</param>
        /// <param name="configure">A Hangfire configuration callback</param>
        /// <returns>The Elsa options, enabling method chaining</returns>
        public static ElsaOptions AddHangfireTemporalActivities(this ElsaOptions options,
                                                                Action<IGlobalConfiguration> configure)
        {
            CommonTemporalActivityServices.AddCommonTemporalActivities(options, timer => timer.UseHangfire(configure));
            return options;
        }
    }
}