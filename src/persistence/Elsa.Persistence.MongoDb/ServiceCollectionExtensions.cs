using System;
using Elsa.Extensions;
using Elsa.Persistence.MongoDb.Services;
using Elsa.Runtime;
using Elsa.WorkflowProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Elsa.Persistence.MongoDb
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
                .AddScoped<MongoDbWorkflowDefinitionStore>()
                .AddScoped<MongoDbWorkflowInstanceStore>()            
                .AddScoped<ElsaMongoDbClient>()
                .AddScoped(sp => sp.GetRequiredService<ElsaMongoDbClient>().WorkflowDefinitions)
                .AddScoped(sp => sp.GetRequiredService<ElsaMongoDbClient>().WorkflowInstances)
                .AddStartupTask<DatabaseInitializer>();

            elsa
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<MongoDbWorkflowDefinitionStore>())
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<MongoDbWorkflowInstanceStore>());
            
            DatabaseRegister.RegisterMapsAndSerializers();
        }
    }
}
