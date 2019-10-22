using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Memory
{
    public static class ServiceCollectionExtensions
    {
        public static MemoryStoreElsaBuilder WithMemoryProvider(this ElsaBuilder configuration)
        {
            return new MemoryStoreElsaBuilder(configuration.Services);
        }

        public static MemoryStoreElsaBuilder WithMemoryStores(this ElsaBuilder configuration)
        {
            return configuration.WithMemoryProvider().WithWorkflowDefinitionStore().WithWorkflowInstanceStore();
        }

        public static MemoryStoreElsaBuilder WithWorkflowInstanceStore(
            this MemoryStoreElsaBuilder configuration)
        {
            configuration.Services.AddSingleton<IWorkflowInstanceStore, MemoryWorkflowInstanceStore>();

            return configuration;
        }

        public static MemoryStoreElsaBuilder WithWorkflowDefinitionStore(
            this MemoryStoreElsaBuilder configuration)
        {
            configuration.Services.AddSingleton<IWorkflowDefinitionStore, MemoryWorkflowDefinitionStore>();

            return configuration;
        }
    }
}