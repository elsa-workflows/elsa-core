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

        public static ElsaOptionsBuilder UseMongoDbPersistenceWithMultitenancy(this ElsaOptionsBuilder elsa) => UseMongoDbPersistenceWithMultitenancy<MultitenantElsaMongoDbContext>(elsa);

        public static ElsaOptionsBuilder UseMongoDbPersistenceWithMultitenancy<TDbContext>(this ElsaOptionsBuilder elsa) where TDbContext : MultitenantElsaMongoDbContext
        {
            AddCoreForMultitenancy<TDbContext>(elsa);
            return elsa;
        }

        private static void AddCore<TDbContext>(ElsaOptionsBuilder elsa) where TDbContext : ElsaMongoDbContext
        {
            elsa.Services
                .AddSingleton<MongoDbWorkflowDefinitionStore>()
                .AddSingleton<MongoDbWorkflowInstanceStore>()
                .AddSingleton<MongoDbWorkflowExecutionLogStore>()
                .AddSingleton<MongoDbBookmarkStore>()
                .AddSingleton<TDbContext>()
                .AddSingleton<ElsaMongoDbContext, TDbContext>()
                .AddSingleton<Func<IMongoCollection<WorkflowDefinition>>>(sp => () => sp.GetRequiredService<ElsaMongoDbContext>().WorkflowDefinitions)
                .AddSingleton<Func<IMongoCollection<WorkflowInstance>>>(sp => () => sp.GetRequiredService<ElsaMongoDbContext>().WorkflowInstances)
                .AddSingleton<Func<IMongoCollection<WorkflowExecutionLogRecord>>>(sp => () => sp.GetRequiredService<ElsaMongoDbContext>().WorkflowExecutionLog)
                .AddSingleton<Func<IMongoCollection<Bookmark>>>(sp => () => sp.GetRequiredService<ElsaMongoDbContext>().Bookmarks)
                .AddStartupTask<DatabaseInitializer>();

            elsa
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<MongoDbWorkflowDefinitionStore>())
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<MongoDbWorkflowInstanceStore>())
                .UseWorkflowExecutionLogStore(sp => sp.GetRequiredService<MongoDbWorkflowExecutionLogStore>())
                .UseWorkflowTriggerStore(sp => sp.GetRequiredService<MongoDbBookmarkStore>());
            
            DatabaseRegister.RegisterMapsAndSerializers();
        }

        private static void AddCoreForMultitenancy<TDbContext>(ElsaOptionsBuilder elsa) where TDbContext : MultitenantElsaMongoDbContext
        {
            elsa.Services
                .AddScoped<MongoDbWorkflowDefinitionStore>()
                .AddScoped<MongoDbWorkflowInstanceStore>()
                .AddScoped<MongoDbWorkflowExecutionLogStore>()
                .AddScoped<MongoDbBookmarkStore>()
                .AddSingleton<TDbContext>()
                .AddSingleton<MultitenantElsaMongoDbContext, TDbContext>()
                .AddScoped<MultitenantElsaMongoDbContextProvider>()
                .AddScoped<Func<IMongoCollection<WorkflowDefinition>>>(sp => () => sp.GetRequiredService<MultitenantElsaMongoDbContextProvider>().WorkflowDefinitions)
                .AddScoped<Func<IMongoCollection<WorkflowInstance>>>(sp => () => sp.GetRequiredService<MultitenantElsaMongoDbContextProvider>().WorkflowInstances)
                .AddScoped<Func<IMongoCollection<WorkflowExecutionLogRecord>>>(sp => () => sp.GetRequiredService<MultitenantElsaMongoDbContextProvider>().WorkflowExecutionLog)
                .AddScoped<Func<IMongoCollection<Bookmark>>>(sp => () => sp.GetRequiredService<MultitenantElsaMongoDbContextProvider>().Bookmarks)
                .AddStartupTask<MultitenantDatabaseInitializer>();

            elsa
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<MongoDbWorkflowDefinitionStore>())
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<MongoDbWorkflowInstanceStore>())
                .UseWorkflowExecutionLogStore(sp => sp.GetRequiredService<MongoDbWorkflowExecutionLogStore>())
                .UseWorkflowTriggerStore(sp => sp.GetRequiredService<MongoDbBookmarkStore>());

            DatabaseRegister.RegisterMapsAndSerializers();
        }
    }
}
