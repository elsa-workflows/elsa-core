using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Memory
{
    public static class ServiceCollectionExtensions
    {
        public static MemoryStoreServiceConfiguration WithMemoryProvider(this ServiceConfiguration configuration)
        {
            return new MemoryStoreServiceConfiguration(configuration.Services);
        }
        
        public static MemoryStoreServiceConfiguration WithWorkflowInstanceStore(this MemoryStoreServiceConfiguration configuration)
        {
            configuration.Services.AddSingleton<IWorkflowInstanceStore, MemoryWorkflowInstanceStore>();

            return configuration;
        }

        public static MemoryStoreServiceConfiguration WithWorkflowDefinitionStore(this MemoryStoreServiceConfiguration configuration)
        {
            configuration.Services.AddSingleton<IWorkflowDefinitionStore, MemoryWorkflowDefinitionStore>();

            return configuration;
        }
    }
}