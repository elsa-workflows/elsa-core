using System;
using Elsa.Persistence.DocumentDb.Services;
using Elsa.Extensions;
using Elsa.Mapping;
using Elsa.Persistence.DocumentDb.Helpers;
using Elsa.Persistence.DocumentDb.Mapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Persistence.DocumentDb.Extensions
{
    public static class ServiceConfigurationExtensions
    {
        public static CosmosDbElsaBuilder AddCosmosDbProvider<T>(this ElsaBuilder builder, Type cosmosDBStoreHelperType,
            Action<OptionsBuilder<DocumentDbStorageOptions>> options) where T : class, ITenantProvider
        {
            options?.Invoke(builder.Services.AddOptions<DocumentDbStorageOptions>());

            builder.Services
                .AddSingleton<IDocumentDbStorage, DocumentDbStorage>()
                .AddSingleton<IWorkflowInstanceStore, CosmosDbWorkflowInstanceStore>()
                .AddSingleton<IWorkflowDefinitionStore, CosmosDbWorkflowDefinitionStore>()
                .AddMapperProfile<NodaTimeProfile>(ServiceLifetime.Singleton)
                .AddMapperProfile<DocumentProfile>(ServiceLifetime.Singleton)
                .AddTransient<ITenantProvider, T>()
                .AddTransient(typeof(ICosmosDbStoreHelper<>), cosmosDBStoreHelperType);

            return new CosmosDbElsaBuilder(builder.Services);
        }

        public static CosmosDbElsaBuilder AddCosmosDbProvider<T>(this ElsaBuilder builder,
            Action<OptionsBuilder<DocumentDbStorageOptions>> options) where T : class, ITenantProvider
        {
            return builder.AddCosmosDbProvider<T>(typeof(CosmosDbStoreHelper<>), options);
        }

        public static CosmosDbElsaBuilder AddCosmosDbProvider(this ElsaBuilder builder,
            Action<OptionsBuilder<DocumentDbStorageOptions>> options)
        {
            return builder.AddCosmosDbProvider<TenantProvider>(options);
        }
    }
}