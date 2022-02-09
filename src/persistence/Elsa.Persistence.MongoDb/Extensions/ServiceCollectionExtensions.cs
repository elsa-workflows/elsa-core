using Elsa.Models;
using Elsa.Options;
using Elsa.Persistence.MongoDb.Options;
using Elsa.Persistence.MongoDb.Services;
using Elsa.Persistence.MongoDb.Stores;
using Elsa.Runtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;

namespace Elsa.Persistence.MongoDb
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder UseMongoDbPersistence(this ElsaOptionsBuilder elsa) => UseMongoDbPersistence<ElsaMongoDbContext>(elsa);
        public static ElsaOptionsBuilder UseMongoDbPersistence<TDbContext>(this ElsaOptionsBuilder elsa) where TDbContext : ElsaMongoDbContext
        {
            AddCore<TDbContext>(elsa);

            return elsa;
        }

        public static ElsaOptionsBuilder UseMongoDbPersistence(this ElsaOptionsBuilder elsa, Action<ElsaMongoDbOptions> configureOptions) => UseMongoDbPersistence<ElsaMongoDbContext>(elsa, configureOptions);

        public static ElsaOptionsBuilder UseMongoDbPersistence<TDbContext>(this ElsaOptionsBuilder elsa, Action<ElsaMongoDbOptions> configureOptions) where TDbContext : ElsaMongoDbContext
        {
            AddCore<TDbContext>(elsa);
            elsa.Services.Configure(configureOptions);

            return elsa;
        }

        public static ElsaOptionsBuilder UseMongoDbPersistence(this ElsaOptionsBuilder elsa, IConfiguration configuration) => UseMongoDbPersistence<ElsaMongoDbContext>(elsa, configuration);

        public static ElsaOptionsBuilder UseMongoDbPersistence<TDbContext>(this ElsaOptionsBuilder elsa, IConfiguration configuration) where TDbContext : ElsaMongoDbContext
        {
            AddCore<TDbContext>(elsa);
            elsa.Services.Configure<ElsaMongoDbOptions>(configuration);
            return elsa;
        }

        private static void AddCore<TDbContext>(ElsaOptionsBuilder elsa) where TDbContext : ElsaMongoDbContext
        {
            elsa.Services
                .AddScoped<MongoDbWorkflowDefinitionStore>()
                .AddScoped<MongoDbWorkflowInstanceStore>()
                .AddScoped<MongoDbWorkflowExecutionLogStore>()
                .AddScoped<MongoDbBookmarkStore>()
                .AddScoped<MongoDbTriggerStore>()
                .AddSingleton<TDbContext>()
                .AddSingleton<ElsaMongoDbContext, TDbContext>()
                .AddScoped<ElsaMongoDbContextProvider>()
                .AddScoped<Func<IMongoCollection<WorkflowDefinition>>>(sp => () => sp.GetRequiredService<ElsaMongoDbContextProvider>().WorkflowDefinitions)
                .AddScoped<Func<IMongoCollection<WorkflowInstance>>>(sp => () => sp.GetRequiredService<ElsaMongoDbContextProvider>().WorkflowInstances)
                .AddScoped<Func<IMongoCollection<WorkflowExecutionLogRecord>>>(sp => () => sp.GetRequiredService<ElsaMongoDbContextProvider>().WorkflowExecutionLog)
                .AddScoped<Func<IMongoCollection<Bookmark>>>(sp => () => sp.GetRequiredService<ElsaMongoDbContextProvider>().Bookmarks)
                .AddScoped<Func<IMongoCollection<Trigger>>>(sp => () => sp.GetRequiredService<ElsaMongoDbContextProvider>().Triggers)
                .AddStartupTask<DatabaseInitializer>();

            elsa
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<MongoDbWorkflowDefinitionStore>())
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<MongoDbWorkflowInstanceStore>())
                .UseWorkflowExecutionLogStore(sp => sp.GetRequiredService<MongoDbWorkflowExecutionLogStore>())
                .UseWorkflowTriggerStore(sp => sp.GetRequiredService<MongoDbBookmarkStore>())
                .UseWorkflowBookmarkTriggerStore(sp => sp.GetRequiredService<MongoDbTriggerStore>());

            DatabaseRegister.RegisterMapsAndSerializers();
        }
    }
}
