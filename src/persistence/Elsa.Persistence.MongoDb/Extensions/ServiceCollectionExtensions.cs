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
                .AddSingleton<ElsaMongoDbClient>()
                .AddSingleton(sp => sp.GetRequiredService<ElsaMongoDbClient>().WorkflowDefinitions)
                .AddSingleton(sp => sp.GetRequiredService<ElsaMongoDbClient>().WorkflowInstances)
                .AddSingleton(sp => sp.GetRequiredService<ElsaMongoDbClient>().WorkflowExecutionLog)
                .AddStartupTask<DatabaseInitializer>();

            elsa
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<MongoDbWorkflowDefinitionStore>())
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<MongoDbWorkflowInstanceStore>())
                .UseWorkflowExecutionLogStore(sp => sp.GetRequiredService<MongoDbWorkflowExecutionLogStore>());
            
            DatabaseRegister.RegisterMapsAndSerializers();
        }
    }
}
