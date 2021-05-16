using System;
using Hangfire;
using Elsa.Activities.Temporal;

// ReSharper disable once CheckNamespace
namespace Elsa
{
    public static class ElsaOptionsExtensions
    {
        /// <summary>
        /// Adds Hangfire services and temporal (time-based) activities to Elsa using the Hangfire implementation.
        /// </summary>
        /// <param name="options">Elsa options</param>
        /// <param name="configure">A Hangfire configuration callback</param>
        /// <returns>The Elsa options, enabling method chaining</returns>
        public static ElsaOptionsBuilder AddHangfireTemporalActivities(
            this ElsaOptionsBuilder options,
            Action<IGlobalConfiguration> configure) =>
            options.AddCommonTemporalActivities(timer => timer.UseHangfire(configure));
        
        /// <summary>
        /// Adds temporal (time-based) activities to Elsa without using the Hangfire implementation. You need to add Hangfire services yourself
        /// </summary>
        /// <param name="options">Elsa options</param>
        /// <returns>The Elsa options, enabling method chaining</returns>
        public static ElsaOptionsBuilder AddHangfireTemporalActivities(
            this ElsaOptionsBuilder options) =>
            options.AddCommonTemporalActivities(timer => timer.UseHangfire());
    }
}