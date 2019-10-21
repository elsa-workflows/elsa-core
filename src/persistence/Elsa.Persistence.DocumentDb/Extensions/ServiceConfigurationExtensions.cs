using Microsoft.Extensions.DependencyInjection;
using Elsa.Persistence.DocumentDb.Services;
using System;

namespace Elsa.Persistence.DocumentDb.Extensions
{
    public static class ServiceConfigurationExtensions
    {
        public static CosmosDbServiceConfiguration WithCosmosDbProvider(
            this ServiceConfiguration configuration,
            string url,
            string authSecret,
            string database,
            string collection,
            DocumentDbStorageOptions options = null)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (string.IsNullOrEmpty(authSecret)) throw new ArgumentNullException(nameof(authSecret));

            var storage = new DocumentDbStorage(url, authSecret, database, collection, options);

            configuration.Services
                .AddSingleton(storage);

            return new CosmosDbServiceConfiguration(configuration.Services);
        }

        public static CosmosDbServiceConfiguration WithWorkflowInstanceStore(this CosmosDbServiceConfiguration configuration)
        {
            configuration.Services.AddSingleton<IWorkflowInstanceStore, CosmosDbWorkflowInstanceStore>();
            return configuration;
        }

        public static CosmosDbServiceConfiguration WithWorkflowDefinitionStore(this CosmosDbServiceConfiguration configuration)
        {
            configuration.Services.AddSingleton<IWorkflowDefinitionStore, CosmosDbWorkflowDefinitionStore>();
            return configuration;
        }
    }
}