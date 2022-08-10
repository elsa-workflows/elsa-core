using System;
using Autofac;
using Autofac.Multitenant;
using Elsa.Extensions;
using Elsa.Multitenancy;
using Elsa.Multitenancy.Extensions;
using Elsa.Options;
using Elsa.Persistence.MongoDb.Options;
using Elsa.Persistence.MongoDb.Services;
using Elsa.Persistence.MongoDb.Stores;
using Elsa.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.MongoDb
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder UseMongoDbPersistence(this ElsaOptionsBuilder elsa) => UseMongoDbPersistence<ElsaMongoDbContext>(elsa);
        
        public static ElsaOptionsBuilder UseMongoDbPersistence<TDbContext>(this ElsaOptionsBuilder elsa) where TDbContext: ElsaMongoDbContext
        {
            AddCore<TDbContext>(elsa);

            return elsa;
        }

        public static ElsaOptionsBuilder UseMongoDbPersistence(this ElsaOptionsBuilder elsa, IConfiguration configuration) => UseMongoDbPersistence<ElsaMongoDbContext>(elsa, configuration);

        public static ElsaOptionsBuilder UseMongoDbPersistence<TDbContext>(this ElsaOptionsBuilder elsa, IConfiguration configuration) where TDbContext: ElsaMongoDbContext
        {
            AddCore<TDbContext>(elsa);
            elsa.Services.Configure<ElsaMongoDbOptions>(configuration);
            return elsa;
        }

        private static void AddCore<TDbContext>(ElsaOptionsBuilder elsa) where TDbContext : ElsaMongoDbContext
        {
            if (elsa.ContainerBuilder == null)
                throw new ArgumentNullException("Cannot setup Entity Framework persistence for multitenancy when ContainerBuilder is null");

            elsa.ContainerBuilder
               .Register(cc =>
               {
                   var tenant = cc.Resolve<ITenant>();
                   return new ElsaMongoDbOptions() { ConnectionString = tenant!.GetDatabaseConnectionString()! };
               }).IfNotRegistered(typeof(ElsaMongoDbOptions)).InstancePerTenant();

            elsa.ContainerBuilder
                .AddMultiton<MongoDbWorkflowDefinitionStore>()
                .AddMultiton<MongoDbWorkflowInstanceStore>()
                .AddMultiton<MongoDbWorkflowExecutionLogStore>()
                .AddMultiton<MongoDbBookmarkStore>()
                .AddMultiton<MongoDbTriggerStore>()
                .AddMultiton<TDbContext>()
                .AddMultiton<ElsaMongoDbContext, TDbContext>()
                .AddMultiton(sp => sp.GetRequiredService<TDbContext>().WorkflowDefinitions)
                .AddMultiton(sp => sp.GetRequiredService<TDbContext>().WorkflowInstances)
                .AddMultiton(sp => sp.GetRequiredService<TDbContext>().WorkflowExecutionLog)
                .AddMultiton(sp => sp.GetRequiredService<TDbContext>().Bookmarks)
                .AddMultiton(sp => sp.GetRequiredService<TDbContext>().Triggers)
                .AddStartupTask<DatabaseInitializer>();

            elsa
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<MongoDbWorkflowDefinitionStore>())
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<MongoDbWorkflowInstanceStore>())
                .UseWorkflowExecutionLogStore(sp => sp.GetRequiredService<MongoDbWorkflowExecutionLogStore>())
                .UseBookmarkStore(sp => sp.GetRequiredService<MongoDbBookmarkStore>())
                .UseTriggerStore(sp => sp.GetRequiredService<MongoDbTriggerStore>());

            DatabaseRegister.RegisterMapsAndSerializers();
        }
    }
}
