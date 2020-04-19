using Elsa.Persistence.DocumentDb.Services;
using Elsa.AutoMapper.Extensions;
using Elsa.AutoMapper.Extensions.NodaTime;
using Elsa.Extensions;
using Elsa.Persistence.DocumentDb.Mapping;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.DocumentDb.Extensions
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions UseCosmosDbWorkflowDefinitionStore(this ElsaOptions options, DocumentDbStorageOptions dbOptions)
        {
            options
                .AddCosmosDbProvider(dbOptions)
                .UseWorkflowDefinitionStore(sp => sp.GetRequiredService<CosmosDbWorkflowDefinitionStore>());
            
            options.Services.AddSingleton<CosmosDbWorkflowDefinitionStore>();
            return options;
        }
        
        public static ElsaOptions UseCosmosDbWorkflowInstanceStore(this ElsaOptions options, DocumentDbStorageOptions dbOptions)
        {
            options
                .AddCosmosDbProvider(dbOptions)
                .UseWorkflowInstanceStore(sp => sp.GetRequiredService<CosmosDbWorkflowInstanceStore>());
            
            options.Services.AddSingleton<IWorkflowInstanceStore, CosmosDbWorkflowInstanceStore>();
            return options;
        }

        private static ElsaOptions AddCosmosDbProvider(
            this ElsaOptions options,
            DocumentDbStorageOptions documentDbOptions)
        {
            if (options.HasService<DocumentDbStorage>())
                return options;
            
            var storage = new DocumentDbStorage(documentDbOptions);

            options.Services
                .AddSingleton(storage)
                .AddAutoMapperProfile<NodaTimeProfile>(ServiceLifetime.Singleton)
                .AddAutoMapperProfile<DocumentProfile>(ServiceLifetime.Singleton);

            return options;
        }
    }
}