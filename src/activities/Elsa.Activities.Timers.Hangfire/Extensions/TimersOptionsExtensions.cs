using System;

using Elsa.Activities.Timers.Hangfire.Services;
using Elsa.Activities.Timers.Options;
using Elsa.Activities.Timers.Services;

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
        /// Add Elsa Hangfire Services for background processing and Hangfire Services
        /// </summary>
        /// <remarks>
        /// Only if Hangfire is not already registered in DI
        /// </remarks>
        /// <param name="timersOptions"></param>
        /// <param name="configure">Hangfire settings</param>
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
