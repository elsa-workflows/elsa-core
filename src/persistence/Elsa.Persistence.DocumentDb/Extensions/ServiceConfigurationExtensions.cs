using System;
using Elsa.Persistence.DocumentDb.Services;
using Elsa.Extensions;
using Elsa.Mapping;
using Elsa.Persistence.DocumentDb.Mapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Persistence.DocumentDb.Extensions
{
    public static class ServiceConfigurationExtensions
    {
        public static CosmosDbElsaBuilder AddCosmosDbProvider(
            this ElsaBuilder builder,
            Action<OptionsBuilder<DocumentDbStorageOptions>> options)
        {
            options?.Invoke(builder.Services.AddOptions<DocumentDbStorageOptions>());

            builder.Services
                .AddSingleton<IDocumentDbStorage, DocumentDbStorage>()
                .AddSingleton<IWorkflowInstanceStore, CosmosDbWorkflowInstanceStore>()
                .AddSingleton<IWorkflowDefinitionStore, CosmosDbWorkflowDefinitionStore>()
                .AddMapperProfile<NodaTimeProfile>(ServiceLifetime.Singleton)
                .AddMapperProfile<DocumentProfile>(ServiceLifetime.Singleton);

            return new CosmosDbElsaBuilder(builder.Services);
        }
    }
}