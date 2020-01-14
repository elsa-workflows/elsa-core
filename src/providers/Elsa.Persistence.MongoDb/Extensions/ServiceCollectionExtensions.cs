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
        public static MongoElsaBuilder AddMongoDbProvider(
            this ElsaBuilder elsaBuilder,
            IConfiguration configuration,
            string databaseName,
            string connectionStringName
        )
        {
            NodaTimeSerializers.Register();
            RegisterEnumAsStringConvention();
            BsonSerializer.RegisterSerializer(new JObjectSerializer());

            elsaBuilder.Services
                .AddSingleton(sp => CreateDbClient(configuration, connectionStringName))
                .AddSingleton(sp => CreateDatabase(sp, databaseName));

            return new MongoElsaBuilder(elsaBuilder.Services);
        }

        public static MongoElsaBuilder AddMongoDbStores(
            this ElsaBuilder elsaBuilder,
            IConfiguration configuration,
            string databaseName,
            string connectionStringName)
        {
            return elsaBuilder
                .AddMongoDbProvider(configuration, databaseName, connectionStringName)
                .AddMongoDbWorkflowDefinitionStore()
                .AddMongoDbWorkflowInstanceStore();
        }

        public static MongoElsaBuilder AddMongoDbWorkflowInstanceStore(
            this MongoElsaBuilder configuration)
        {
            configuration.Services
                .AddMongoDbCollection<WorkflowInstance>("WorkflowInstances")
                .AddScoped<IWorkflowInstanceStore, MongoWorkflowInstanceStore>();

            return configuration;
        }

        public static MongoElsaBuilder AddMongoDbWorkflowDefinitionStore(
            this MongoElsaBuilder configuration)
        {
            configuration.Services
                .AddMongoDbCollection<WorkflowDefinitionVersion>("WorkflowDefinitions")
                .AddScoped<IWorkflowDefinitionStore, MongoWorkflowDefinitionStore>();

            return configuration;
        }

        public static IServiceCollection AddMongoDbCollection<T>(
            this IServiceCollection services,
            string collectionName)
        {
            return services.AddSingleton(sp => CreateCollection<T>(sp, collectionName));
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

        private static IMongoClient CreateDbClient(IConfiguration configuration, string connectionStringName)
        {
            var connectionString = configuration.GetConnectionString(connectionStringName);
            return new MongoClient(connectionString);
        }

        private static void RegisterEnumAsStringConvention()
        {
            var pack = new ConventionPack { new EnumRepresentationConvention(BsonType.String) };

            ConventionRegistry.Register("EnumStringConvention", pack, _ => true);
        }
    }
}