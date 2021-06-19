using Elsa;
using Elsa.Activities.File;
using System;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFileActivities(this IServiceCollection services)
        {
            return services;
        }

        public static ElsaOptionsBuilder AddFileActivities(this ElsaOptionsBuilder builder) 
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.AddActivity<DeleteFile>()
                .AddActivity<EnumerateFiles>()
                .AddActivity<FileExists>()
                .AddActivity<OutFile>()
                .AddActivity<ReadFile>()
                .AddActivity<TempFile>();

            return builder;
        }
    }
}
