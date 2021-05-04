using Elsa;
using Elsa.Activities.File;
using Elsa.Activities.File.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFileActivities(this IServiceCollection services)
        {
            return services;
        }

        public static ElsaOptionsBuilder AddFileActivities(this ElsaOptionsBuilder builder, Action<FileWatcherOptions>? configureOptions = null)
        {
            builder.Services.AddFileServices(configureOptions);
            builder.AddActivity<DeleteFile>();
            builder.AddActivity<FileExists>();
            builder.AddActivity<OutFile>();
            builder.AddActivity<TempFile>();

            return builder;
        }

        public static IServiceCollection AddFileServices(this IServiceCollection services, Action<FileWatcherOptions>? configureOptions = null)
        {
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            return services;
        }
    }
}
