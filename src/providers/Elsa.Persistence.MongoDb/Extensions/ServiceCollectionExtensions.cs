using System;
using Elsa;
using Elsa.Models;
using Elsa.Persistence.MongoDb.Serialization;
using Elsa.Persistence.MongoDb.Services;
using MongoDB.Bson;
using MongoDb.Bson.NodaTime;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;

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
                .UseMongoDbWorkflowDefinitionVersionStore(databaseName, connectionString)
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

        public static ElsaOptions UseMongoDbWorkflowDefinitionVersionStore(
            this ElsaOptions options,
            string databaseName,
            string connectionString)
        {
            options
                .AddMongoDbProvider(databaseName, connectionString)
                .UseWorkflowDefinitionVersionStore(sp => sp.GetRequiredService<MongoWorkflowDefinitionVersionStore>())
                .Services
                .AddMongoDbCollection<WorkflowDefinitionVersion>("WorkflowDefinitionVersions")
                .AddScoped<MongoWorkflowDefinitionVersionStore>();

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
                .AddMongoDbCollection<WorkflowDefinition>("WorkflowDefinitions")
                .AddScoped<MongoWorkflowDefinitionStore>();

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

            options.Services
                .AddTransient<JObjectSerializer>()
                .AddTransient<VariableSerializer>()
                .AddSingleton(sp =>
                {
                    NodaTimeSerializers.Register();
                    RegisterEnumAsStringConvention();
                    BsonSerializer.RegisterSerializer(sp.GetRequiredService<JObjectSerializer>());
                    BsonSerializer.RegisterSerializer(sp.GetRequiredService<VariableSerializer>());
                    return CreateDbClient(connectionString);
                })
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