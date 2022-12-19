using Elsa.Options;
using Elsa.Persistence.MongoDb.Options;
using Elsa.Persistence.MongoDb.Services;
using Elsa.Persistence.MongoDb.Stores;
using Elsa.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Elsa.Persistence.MongoDb
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder UseMongoDbPersistence(this ElsaOptionsBuilder elsa, Action<ElsaMongoDbOptions> configureOptions) => UseMongoDbPersistence<ElsaMongoDbContext>(elsa, configureOptions);

        public static ElsaOptionsBuilder UseMongoDbPersistence<TDbContext>(this ElsaOptionsBuilder elsa, Action<ElsaMongoDbOptions> configureOptions) where TDbContext : ElsaMongoDbContext
        {
            var tempConfig = new ElsaMongoDbOptions();
            configureOptions(tempConfig);
            elsa.Services.Configure(configureOptions);
            AddCore<TDbContext>(elsa, tempConfig);

            return elsa;
        }

        public static ElsaOptionsBuilder UseMongoDbPersistence(this ElsaOptionsBuilder elsa, IConfiguration configuration) => UseMongoDbPersistence<ElsaMongoDbContext>(elsa, configuration);

        public static ElsaOptionsBuilder UseMongoDbPersistence<TDbContext>(this ElsaOptionsBuilder elsa, IConfiguration configuration) where TDbContext : class, IElsaMongoDbContext
        {
            elsa.Services.Configure<ElsaMongoDbOptions>(configuration);
            var tempConfig = configuration.Get<ElsaMongoDbOptions>();
            AddCore<TDbContext>(elsa, tempConfig);
            return elsa;
        }

        private static void AddCore<TDbContext>(ElsaOptionsBuilder elsa, ElsaMongoDbOptions mongoDbOptions) where TDbContext : class, IElsaMongoDbContext
        {
            elsa.Services
                .AddSingleton<MongoDbWorkflowDefinitionStore>()
                .AddSingleton<MongoDbWorkflowInstanceStore>()
                .AddSingleton<MongoDbWorkflowExecutionLogStore>()
                .AddSingleton<MongoDbBookmarkStore>()
                .AddSingleton<MongoDbTriggerStore>()
                .AddSingleton<TDbContext>()
                .AddSingleton<IElsaMongoDbContext, TDbContext>()
                .AddSingleton(sp => sp.GetRequiredService<TDbContext>().WorkflowDefinitions)
                .AddSingleton(sp => sp.GetRequiredService<TDbContext>().WorkflowInstances)
                .AddSingleton(sp => sp.GetRequiredService<TDbContext>().WorkflowExecutionLog)
                .AddSingleton(sp => sp.GetRequiredService<TDbContext>().Bookmarks)
                .AddSingleton(sp => sp.GetRequiredService<TDbContext>().Triggers)
                .AddStartupTask<DatabaseInitializer>();

            elsa
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<MongoDbWorkflowDefinitionStore>())
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<MongoDbWorkflowInstanceStore>())
                .UseWorkflowExecutionLogStore(sp => sp.GetRequiredService<MongoDbWorkflowExecutionLogStore>())
                .UseBookmarkStore(sp => sp.GetRequiredService<MongoDbBookmarkStore>())
                .UseTriggerStore(sp => sp.GetRequiredService<MongoDbTriggerStore>());

            DatabaseRegister.RegisterMapsAndSerializers(mongoDbOptions);
        }
    }
}
