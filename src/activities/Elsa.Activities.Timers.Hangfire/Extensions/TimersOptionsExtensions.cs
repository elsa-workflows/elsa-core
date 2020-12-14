using System;

using Elsa.Activities.Timers.Hangfire.Services;
using Elsa.Activities.Timers.Options;
using Elsa.Activities.Timers.Services;

using Hangfire;

using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Timers.Hangfire.Extensions
{
    public static class TimersOptionsExtensions
    {
        /// <summary>
        /// Add Hangfire for background processing
        /// </summary>
        /// <param name="timersOptions"></param>
        public static void UseHangfireProviderCore(this TimersOptions timersOptions)
        {
            timersOptions.Services
                .AddSingleton<IWorkflowScheduler, HangfireWorkflowScheduler>()
                .AddSingleton<ICrontabParser, HangfireCrontabParser>();
        }

        /// <summary>
        /// Add Hangfire for background processing
        /// </summary>
        /// <remarks>
        /// Only if Hangfire is not already registered in DI
        /// </remarks>
        /// <param name="timersOptions"></param>
        /// <param name="timersOptions">Hangfire settings</param>
        public static void UseHangfireProvider(this TimersOptions timersOptions, Action<IGlobalConfiguration> configure)
        {
            timersOptions.UseHangfireProviderCore();

            // Add Hangfire services.
            timersOptions.Services.AddHangfire(configure);

            // Add the processing server as IHostedService
            timersOptions.Services.AddHangfireServer();
        }
    }
}
