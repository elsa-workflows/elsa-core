using System;
using Elsa.Retention.HostedServices;
using Elsa.Retention.Jobs;
using Elsa.Retention.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Retention.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRetentionServices(this IServiceCollection services, Action<CleanupOptions> configureOptions)
        {
            services
                .Configure(configureOptions)
                .AddScoped<CleanupJob>()
                .AddHostedService<CleanupService>();

            return services;
        }
    }
}