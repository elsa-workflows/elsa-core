using System;
using Elsa;
using Elsa.Activities.File;
using Elsa.Activities.File.Bookmarks;
using Elsa.Activities.File.Consumers;
using Elsa.Activities.File.MapperProfiles;
using Elsa.Activities.File.Services;
using Elsa.Activities.File.StartupTasks;
using Elsa.Events;
using Elsa.Multitenancy;
using Elsa.Options;
using Elsa.Runtime;
using Elsa.Services;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFileActivities(this IServiceCollection services)
        {
            return services;
        }

        public static ElsaOptionsBuilder AddFileActivities(this ElsaOptionsBuilder builder, Action<MultitenancyOptions>? configure)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (configure != null)
                builder.Services.Configure(configure);
            else
                builder.Services.AddOptions<MultitenancyOptions>();

            builder.AddActivity<DeleteFile>()
                .AddActivity<EnumerateFiles>()
                .AddActivity<FileExists>()
                .AddActivity<OutFile>()
                .AddActivity<ReadFile>()
                .AddActivity<TempFile>()
                .AddActivity<WatchDirectory>();

            builder.AddPubSubConsumer<RecreateFileSystemWatchersConsumer, WorkflowDefinitionPublished>("WorkflowDefinitionEvents");
            builder.AddPubSubConsumer<RecreateFileSystemWatchersConsumer, WorkflowDefinitionRetracted>("WorkflowDefinitionEvents");
            builder.AddPubSubConsumer<RecreateFileSystemWatchersConsumer, WorkflowDefinitionDeleted>("WorkflowDefinitionEvents");

            builder.Services.AddBookmarkProvider<FileCreatedBookmarkProvider>()
                .AddAutoMapperProfile<FileSystemEventProfile>()
                .AddSingleton(CreateFileSystemWatchersStarter)
                .AddSingleton<Scoped<IWorkflowLaunchpad>>()
                .AddStartupTask<StartFileSystemWatchers>();

            return builder;
        }

        public static IFileSystemWatchersStarter CreateFileSystemWatchersStarter(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<MultitenancyOptions>>().Value;
            var multitenancyEnabled = options.MultitenancyEnabled;

            if (multitenancyEnabled)
                return ActivatorUtilities.GetServiceOrCreateInstance<MultitenantFileSystemWatchersStarter>(serviceProvider);
            else
                return ActivatorUtilities.GetServiceOrCreateInstance<FileSystemWatchersStarter>(serviceProvider);
        }
    }
}