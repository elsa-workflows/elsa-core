using System;
using Elsa.Persistence.MongoDb.Services;
using Elsa.Runtime;
using Elsa.WorkflowProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Elsa.Persistence.MongoDb
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddElsaPersistenceMongoDb(this IServiceCollection services, Action<ElsaMongoDbOptions> configureOptions)
        {
            AddCore(services);
            services.Configure(configureOptions);

            return services;
        }

        public static IServiceCollection AddElsaPersistenceMongoDb(this IServiceCollection services, IConfiguration options)
        {
            AddCore(services);
            services.Configure<ElsaMongoDbOptions>(options);
            return services;
        }

        private static void AddCore(IServiceCollection services)
        {
            services.AddScoped<IWorkflowDefinitionStore, MongoDbWorkflowDefinitionStore>()
                .AddScoped<IWorkflowInstanceStore, MongoDbWorkflowInstanceStore>()            
                .AddScoped<ElsaMongoDbClient>()
                .AddScoped(sp => sp.GetRequiredService<ElsaMongoDbClient>().WorkflowDefinitions)
                .AddScoped(sp => sp.GetRequiredService<ElsaMongoDbClient>().WorkflowInstances)
                .AddWorkflowProvider<DatabaseWorkflowProvider>()
                .AddStartupTask<DatabaseInitializer>();

            DatabaseRegister.RegisterMapsAndSerializers();
        }
    }
}
