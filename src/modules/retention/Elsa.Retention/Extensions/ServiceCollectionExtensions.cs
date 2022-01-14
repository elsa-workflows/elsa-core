using System;
using Elsa.Retention.HostedServices;
using Elsa.Retention.Jobs;
using Elsa.Retention.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Retention.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRetentionServices(this IServiceCollection services, Action<CleanupOptions> configureOptions, IConfiguration configuration)
        {
            services
                .Configure(configureOptions)
                .AddScoped<CleanupJob>();

            var multitenancyEnabled = configuration.GetValue<bool>("Elsa:MultiTenancy");

            if (multitenancyEnabled)
                services.AddHostedService<MultitenantCleanupService>();
            else
                services.AddHostedService<CleanupService>();

            return services;
        }
    }
}