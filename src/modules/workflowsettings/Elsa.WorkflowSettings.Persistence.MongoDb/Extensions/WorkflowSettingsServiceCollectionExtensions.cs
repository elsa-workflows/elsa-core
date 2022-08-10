using Autofac;
using Autofac.Multitenant;
using Elsa.Extensions;
using Elsa.Multitenancy;
using Elsa.Multitenancy.Extensions;
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
        public static WorkflowSettingsOptionsBuilder UseWorkflowSettingsMongoDbPersistence(this WorkflowSettingsOptionsBuilder workflowSettingsOptions) => UseWorkflowSettingsMongoDbPersistence<ElsaMongoDbContext>(workflowSettingsOptions);

        public static WorkflowSettingsOptionsBuilder UseWorkflowSettingsMongoDbPersistence<TDbContext>(this WorkflowSettingsOptionsBuilder workflowSettingsOptions) where TDbContext : ElsaMongoDbContext
        {
            AddCore<TDbContext>(workflowSettingsOptions);

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
            workflowSettingsOptions.ContainerBuilder
              .Register(cc =>
              {
                  var tenant = cc.Resolve<ITenant>();
                  return new ElsaMongoDbOptions() { ConnectionString = tenant!.GetDatabaseConnectionString()! };
              }).IfNotRegistered(typeof(ElsaMongoDbOptions)).InstancePerTenant();

            workflowSettingsOptions.ContainerBuilder
                .AddMultiton<MongoDbWorkflowSettingsStore>()
                .AddMultiton<TDbContext>()
                .AddMultiton<ElsaMongoDbContext, TDbContext>()
                .AddMultiton(sp => sp.GetRequiredService<TDbContext>().WorkflowSettings)
                .AddStartupTask<DatabaseInitializer>();

            workflowSettingsOptions.UseWorkflowSettingsStore(sp => sp.GetRequiredService<MongoDbWorkflowSettingsStore>());

            DatabaseRegister.RegisterMapsAndSerializers();
        }
    }
}
