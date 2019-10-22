using Microsoft.Extensions.DependencyInjection;
using Elsa.Persistence.DocumentDb.Services;
using System;

namespace Elsa.Persistence.DocumentDb.Extensions
{
    public static class ServiceConfigurationExtensions
    {
        public static CosmosDbElsaBuilder AddCosmosDbProvider(
            this ElsaBuilder builder,
            string url,
            string authSecret,
            string database,
            string collection,
            DocumentDbStorageOptions options = null)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (string.IsNullOrEmpty(authSecret)) throw new ArgumentNullException(nameof(authSecret));

            var storage = new DocumentDbStorage(url, authSecret, database, collection, options);

            builder.Services
                .AddSingleton(storage);

            return new CosmosDbElsaBuilder(builder.Services);
        }

        public static CosmosDbElsaBuilder AddWorkflowInstanceStore(this CosmosDbElsaBuilder configuration)
        {
            configuration.Services.AddSingleton<IWorkflowInstanceStore, CosmosDbWorkflowInstanceStore>();
            return configuration;
        }

        public static CosmosDbElsaBuilder AddWorkflowDefinitionStore(this CosmosDbElsaBuilder configuration)
        {
            configuration.Services.AddSingleton<IWorkflowDefinitionStore, CosmosDbWorkflowDefinitionStore>();
            return configuration;
        }
    }
}