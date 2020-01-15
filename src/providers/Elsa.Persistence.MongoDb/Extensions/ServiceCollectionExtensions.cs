using System;
using Elsa;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.MongoDb;
using Elsa.Persistence.MongoDb.Serialization;
using Elsa.Persistence.MongoDb.Services;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDb.Bson.NodaTime;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptions UseMongoDbWorkflowStores(
            this ElsaOptions options,
            string databaseName,
            string connectionString)
        {
            return options
                .AddMongoDbProvider(databaseName, connectionString)
                .UseMongoDbWorkflowDefinitionStore(databaseName, connectionString)
                .UseMongoDbWorkflowInstanceStore(databaseName, connectionString);
        }

        public static ElsaOptions UseMongoDbWorkflowInstanceStore(
            this ElsaOptions options,
            string databaseName,
            string connectionString)
        {
            options
                .AddMongoDbProvider(databaseName, connectionString)
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<MongoWorkflowInstanceStore>())
                .Services
                .AddMongoDbCollection<WorkflowInstance>("WorkflowInstances")
                .AddScoped<MongoWorkflowInstanceStore>();

            return options;
        }

        public static ElsaOptions UseMongoDbWorkflowDefinitionStore(
            this ElsaOptions options,
            string databaseName,
            string connectionString)
        {
            options
                .AddMongoDbProvider(databaseName, connectionString)
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<MongoWorkflowDefinitionStore>())
                .Services
                .AddMongoDbCollection<WorkflowDefinitionVersion>("WorkflowDefinitions")
                .AddScoped<IWorkflowDefinitionStore, MongoWorkflowDefinitionStore>();

            return options;
        }

        public static IServiceCollection AddMongoDbCollection<T>(
            this IServiceCollection services,
            string collectionName)
        {
            return services.AddSingleton(sp => CreateCollection<T>(sp, collectionName));
        }

        private static ElsaOptions AddMongoDbProvider(
            this ElsaOptions options,
            string databaseName,
            string connectionString
        )
        {
            if (options.Services.HasService<IMongoClient>())
                return options;
            
            NodaTimeSerializers.Register();
            RegisterEnumAsStringConvention();
            BsonSerializer.RegisterSerializer(new JObjectSerializer());

            options.Services
                .AddSingleton(sp => CreateDbClient(connectionString))
                .AddSingleton(sp => CreateDatabase(sp, databaseName));

            return options;
        }
        
        private static IMongoCollection<T> CreateCollection<T>(IServiceProvider serviceProvider, string collectionName)
        {
            var database = serviceProvider.GetRequiredService<IMongoDatabase>();
            return database.GetCollection<T>(collectionName);
        }

        private static IMongoDatabase CreateDatabase(IServiceProvider serviceProvider, string databaseName)
        {
            var client = serviceProvider.GetRequiredService<IMongoClient>();
            return client.GetDatabase(databaseName);
        }

        private static IMongoClient CreateDbClient(string connectionString) => new MongoClient(connectionString);

        private static void RegisterEnumAsStringConvention()
        {
            var pack = new ConventionPack { new EnumRepresentationConvention(BsonType.String) };

            ConventionRegistry.Register("EnumStringConvention", pack, _ => true);
        }
    }
}