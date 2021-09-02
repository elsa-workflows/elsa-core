using Elsa;
using Elsa.Activities.File;
using Elsa.Activities.File.Bookmarks;
using Elsa.Activities.File.Services;
using Elsa.Activities.File.StartupTasks;
using Elsa.Runtime;
using System;
using Elsa.Options;

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
                .AddActivity<TempFile>()
                .AddActivity<WatchDirectory>();

            builder.Services.AddSingleton<FileSystemWatchersStarter>()
                .AddBookmarkProvider<FileCreatedBookmarkProvider>()
                .AddStartupTask<StartFileSystemWatchers>();

            return builder;
        }
    }
}
