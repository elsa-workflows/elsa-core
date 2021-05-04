using System;
using Elsa.Activities.Temporal.Common.Options;
using Elsa.Activities.Temporal.Common.Services;
using Elsa.Activities.Temporal.Hangfire.Services;
using Hangfire;

using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa
{
    public static class TimersOptionsExtensions
    {
        /// <summary>
        /// Add Elsa Hangfire Services for background processing
        /// </summary>
        /// <param name="timersOptions"></param>
        public static void UseHangfire(this TimersOptions timersOptions)
        {
            timersOptions.Services
                .AddSingleton<IWorkflowScheduler, HangfireWorkflowScheduler>()
                .AddSingleton<ICrontabParser, HangfireCrontabParser>();
        }

        /// <summary>
        /// Add Elsa Hangfire Services for background processing and Hangfire services.
        /// </summary>
        /// <remarks>
        /// Use only if Hangfire is not already registered in DI.
        /// </remarks>
        /// <param name="timersOptions">The TimersOptions being configured</param>
        /// <param name="configure">Configure Hangfire settings</param>
        public static void UseHangfire(this TimersOptions timersOptions, Action<IGlobalConfiguration> configure)
        {
            timersOptions.UseHangfire();

            // Add Hangfire services.
            timersOptions.Services.AddHangfire(configure);

            // Add the processing server as IHostedService
            timersOptions.Services.AddHangfireServer();
        }
    }
}
