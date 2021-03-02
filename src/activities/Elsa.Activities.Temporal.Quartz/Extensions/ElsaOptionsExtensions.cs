using System;
using Elsa.Activities.Temporal;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Elsa
{
    public static class ElsaOptionsExtensions
    {
        /// <summary>
        /// Adds temporal (time-based) activities to Elsa, using the Quartz implementation.
        /// </summary>
        /// <param name="options">Elsa options</param>
        /// <param name="configure">An optional Quartz configuration callback</param>
        /// <param name="configureQuartz">An optional service collection Quartz configuration callback</param>
        /// <returns>The Elsa options, enabling method chaining</returns>
        public static ElsaOptions AddQuartzTemporalActivities(this ElsaOptions options,
                                                              Action<QuartzOptions>? configure = default,
                                                              Action<IServiceCollectionQuartzConfigurator>? configureQuartz = default)
        {
            CommonTemporalActivityServices.AddCommonTemporalActivities(options, timer => timer.UseQuartzProvider(configure, configureQuartz));
            return options;
        }
    }
}