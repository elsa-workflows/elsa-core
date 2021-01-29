using System;
using Elsa.Persistence.MongoDb.Options;
using Elsa.Persistence.MongoDb.Services;
using Elsa.Persistence.MongoDb.Stores;
using Elsa.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.MongoDb.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptions UseMongoDbPersistence(this ElsaOptions elsa, Action<ElsaMongoDbOptions> configureOptions)
        {
            AddCore(elsa);
            elsa.Services.Configure(configureOptions);

            return elsa;
        }

        public static ElsaOptions UseMongoDbPersistence(this ElsaOptions elsa, IConfiguration options)
        {
            AddCore(elsa);
            elsa.Services.Configure<ElsaMongoDbOptions>(options);
            return elsa;
        }

        private static void AddCore(ElsaOptions elsa)
        {
            elsa.Services
                .AddSingleton<MongoDbWorkflowDefinitionStore>()
                .AddSingleton<MongoDbWorkflowInstanceStore>()
                .AddSingleton<MongoDbWorkflowExecutionLogStore>()
                .AddSingleton<MongoDbBookmarkStore>()
                .AddSingleton<ElsaMongoDbContext>()
                .AddSingleton(sp => sp.GetRequiredService<ElsaMongoDbContext>().WorkflowDefinitions)
                .AddSingleton(sp => sp.GetRequiredService<ElsaMongoDbContext>().WorkflowInstances)
                .AddSingleton(sp => sp.GetRequiredService<ElsaMongoDbContext>().WorkflowExecutionLog)
                .AddSingleton(sp => sp.GetRequiredService<ElsaMongoDbContext>().Bookmarks)
                .AddStartupTask<DatabaseInitializer>();

            elsa
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<MongoDbWorkflowDefinitionStore>())
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<MongoDbWorkflowInstanceStore>())
                .UseWorkflowExecutionLogStore(sp => sp.GetRequiredService<MongoDbWorkflowExecutionLogStore>())
                .UseWorkflowTriggerStore(sp => sp.GetRequiredService<MongoDbBookmarkStore>());
            
            DatabaseRegister.RegisterMapsAndSerializers();
        }
    }
}
