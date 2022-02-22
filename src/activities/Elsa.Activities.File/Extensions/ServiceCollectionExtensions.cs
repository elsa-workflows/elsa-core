using Elsa;
using Elsa.Activities.File;
using Elsa.Activities.File.Bookmarks;
using Elsa.Activities.File.Services;
using Elsa.Activities.File.StartupTasks;
using System;
using Elsa.Activities.File.Consumers;
using Elsa.Options;
using Elsa.Activities.File.MapperProfiles;
using Elsa.Events;

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

            builder.AddPubSubConsumer<RecreateFileSystemWatchersConsumer, TriggerIndexingFinished>("WorkflowManagementEvents");
            builder.AddPubSubConsumer<RecreateFileSystemWatchersConsumer, TriggersDeleted>("WorkflowManagementEvents");
            builder.AddPubSubConsumer<RecreateFileSystemWatchersConsumer, BookmarkIndexingFinished>("WorkflowManagementEvents");
            builder.AddPubSubConsumer<RecreateFileSystemWatchersConsumer, BookmarksDeleted>("WorkflowManagementEvents");
            
            builder.Services.AddBookmarkProvider<FileCreatedBookmarkProvider>()
                .AddAutoMapperProfile<FileSystemEventProfile>()
                .AddSingleton<FileSystemWatchersStarter>()
                .AddHostedService<StartFileSystemWatchers>();

            return builder;
        }
    }
}