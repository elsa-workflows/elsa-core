using System;
using Elsa.Models;
using Elsa.Persistence.Memory;
using Elsa.Persistence.MongoDb.Serialization;
using Elsa.Persistence.MongoDb.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDb.Bson.NodaTime;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static MongoServiceConfiguration WithMongoDbProvider(
            this ServiceConfiguration serviceConfiguration,
            IConfiguration configuration,
            string databaseName,
            string connectionStringName
        )
        {
            NodaTimeSerializers.Register();
            RegisterEnumAsStringConvention();
            BsonSerializer.RegisterSerializer(new JObjectSerializer());

            serviceConfiguration.Services
                .AddSingleton(sp => CreateDbClient(configuration, connectionStringName))
                .AddSingleton(sp => CreateDatabase(sp, databaseName));

            return new MongoServiceConfiguration(serviceConfiguration.Services);
        }

        public static MongoServiceConfiguration UseMongoDbStores(
            this ServiceConfiguration serviceConfiguration,
            IConfiguration configuration,
            string databaseName,
            string connectionStringName)
        {
            return serviceConfiguration
                .WithMongoDbProvider(configuration, databaseName, connectionStringName)
                .AddMongoDbWorkflowDefinitionStore()
                .AddMongoDbWorkflowInstanceStore();
        }

        public static MongoServiceConfiguration AddMongoDbWorkflowInstanceStore(
            this MongoServiceConfiguration configuration)
        {
            configuration.Services
                .AddMongoDbCollection<WorkflowInstance>("WorkflowInstances")
                .Replace<IWorkflowInstanceStore, MongoWorkflowInstanceStore>(ServiceLifetime.Scoped);

            return configuration;
        }

        public static MongoServiceConfiguration AddMongoDbWorkflowDefinitionStore(
            this MongoServiceConfiguration configuration)
        {
            configuration.Services
                .AddMongoDbCollection<WorkflowDefinitionVersion>("WorkflowDefinitions")
                .Replace<IWorkflowDefinitionStore, MongoWorkflowDefinitionStore>(ServiceLifetime.Scoped);

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