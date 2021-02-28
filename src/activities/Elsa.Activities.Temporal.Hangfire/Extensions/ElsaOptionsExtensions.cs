using System;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions AddHangfireTimerActivities(this ElsaOptions options, Action<IGlobalConfiguration> configure) => options.AddTimerActivities(timer => timer.UseHangfire(configure));
    }
}