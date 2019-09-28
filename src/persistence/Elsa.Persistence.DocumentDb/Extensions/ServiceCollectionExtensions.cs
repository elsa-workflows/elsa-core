using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Persistence.DocumentDb.Services;
using System;

namespace Elsa.Persistence.DocumentDb.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection UseAzureCosmosDbAsWorkflowStore(
            this IServiceCollection services,
            string url,
            string authSecret,
            string database,
            string collection,
            DocumentDbStorageOptions options = null)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            if (string.IsNullOrEmpty(authSecret)) throw new ArgumentNullException(nameof(authSecret));

            var storage = new DocumentDbStorage(url, authSecret, database, collection, options);

            return services
                .AddSingleton(storage);
        }

        public static IServiceCollection AddCosmosDbWorkflowInstanceStore(this IServiceCollection services)
        {
            return services.Replace<IWorkflowInstanceStore, CosmosDbWorkflowInstanceStore>(ServiceLifetime.Transient);
        }

        public static IServiceCollection AddCosmosDbWorkflowDefinitionStore(this IServiceCollection services)
        {
            return services.Replace<IWorkflowDefinitionStore, CosmosDbWorkflowDefinitionStore>(
                ServiceLifetime.Transient);
        }
    }
}