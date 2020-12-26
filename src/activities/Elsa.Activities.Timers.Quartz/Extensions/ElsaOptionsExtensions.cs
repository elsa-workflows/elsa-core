using System;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

// ReSharper disable once CheckNamespace
namespace Elsa
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions AddQuartzTimerActivities(this ElsaOptions options, Action<QuartzOptions>? configureOptions = default, Action<IServiceCollectionQuartzConfigurator>? configureQuartz = default) => options.AddTimerActivities(timer => timer.UseQuartzProvider(configureOptions, configureQuartz));
    }
}