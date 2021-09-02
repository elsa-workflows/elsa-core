using System;
using Elsa.Persistence.MongoDb.Options;
using Elsa.Runtime;
using Elsa.WorkflowSettings.Persistence.MongoDb.Services;
using Elsa.WorkflowSettings.Persistence.MongoDb.Stores;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowSettings.Persistence.MongoDb.Extensions
{
    public static class WorkflowSettingsServiceCollectionExtensions
    {
        public static WorkflowSettingsOptionsBuilder UseWorkflowSettingsMongoDbPersistence(this WorkflowSettingsOptionsBuilder workflowSettingsOptions, Action<ElsaMongoDbOptions> configureOptions) => UseWorkflowSettingsMongoDbPersistence<ElsaMongoDbContext>(workflowSettingsOptions, configureOptions);

        public static WorkflowSettingsOptionsBuilder UseWorkflowSettingsMongoDbPersistence<TDbContext>(this WorkflowSettingsOptionsBuilder workflowSettingsOptions, Action<ElsaMongoDbOptions> configureOptions) where TDbContext : ElsaMongoDbContext
        {
            AddCore<TDbContext>(workflowSettingsOptions);
            workflowSettingsOptions.Services.Configure(configureOptions);
            return workflowSettingsOptions;
        }

        public static WorkflowSettingsOptionsBuilder UseWorkflowSettingsMongoDbPersistence(this WorkflowSettingsOptionsBuilder workflowSettingsOptions, IConfiguration configuration) => UseWorkflowSettingsMongoDbPersistence<ElsaMongoDbContext>(workflowSettingsOptions, configuration);

        public static WorkflowSettingsOptionsBuilder UseWorkflowSettingsMongoDbPersistence<TDbContext>(this WorkflowSettingsOptionsBuilder workflowSettingsOptions, IConfiguration configuration) where TDbContext : ElsaMongoDbContext
        {
            AddCore<TDbContext>(workflowSettingsOptions);
            workflowSettingsOptions.Services.Configure<ElsaMongoDbOptions>(configuration);
            return workflowSettingsOptions;
        }

        private static void AddCore<TDbContext>(WorkflowSettingsOptionsBuilder workflowSettingsOptions) where TDbContext : ElsaMongoDbContext
        {
            workflowSettingsOptions.Services
                .AddSingleton<MongoDbWorkflowSettingsStore>()
                .AddSingleton<TDbContext>()
                .AddSingleton<ElsaMongoDbContext, TDbContext>()
                .AddSingleton(sp => sp.GetRequiredService<TDbContext>().WorkflowSettings)
                .AddStartupTask<DatabaseInitializer>();

            workflowSettingsOptions.UseWorkflowSettingsStore(sp => sp.GetRequiredService<MongoDbWorkflowSettingsStore>());

            DatabaseRegister.RegisterMapsAndSerializers();
        }
    }
}
