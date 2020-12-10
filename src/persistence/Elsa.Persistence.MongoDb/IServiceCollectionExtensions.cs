using System;

using Elsa.Persistence.MongoDb.Services;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Configuration;

using Elsa.Repositories;
using Elsa.Runtime;

namespace Elsa.Persistence.MongoDb
{
    public static class IServiceCollectionExtensions
    {

        public static IServiceCollection AddElsaPersistenceMongoDb(this IServiceCollection services, Action<ElsaMongoDbOptions> configureOptions)
        {
            AddCore(services);
            services.Configure<ElsaMongoDbOptions>(configureOptions);

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
            services.AddScoped<IWorkflowDefinitionRepository, MongoDbWorkflowDefinitionRepository>();
            services.AddScoped<IWorkflowInstanceRepository, MongoDbWorkflowInstanceRepository>();
            services.AddScoped<WorkflowEngineMongoDbClient>();
            services.AddStartupTask<DatabaseInitializer>();

        }
    }
}
