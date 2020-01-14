using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Memory
{
    public static class ServiceCollectionExtensions
    {
        public static MemoryStoreElsaOptions WithMemoryProvider(this ElsaOptions configuration)
        {
            return new MemoryStoreElsaOptions(configuration.Services);
        }

        public static MemoryStoreElsaOptions WithMemoryStores(this ElsaOptions configuration)
        {
            return configuration.WithMemoryProvider().WithWorkflowDefinitionStore().WithWorkflowInstanceStore();
        }

        public static MemoryStoreElsaOptions WithWorkflowInstanceStore(
            this MemoryStoreElsaOptions configuration)
        {
            configuration.Services.AddSingleton<IWorkflowInstanceStore, MemoryWorkflowInstanceStore>();

            return configuration;
        }

        public static MemoryStoreElsaOptions WithWorkflowDefinitionStore(
            this MemoryStoreElsaOptions configuration)
        {
            configuration.Services.AddSingleton<IWorkflowDefinitionStore, MemoryWorkflowDefinitionStore>();

            return configuration;
        }
    }
}