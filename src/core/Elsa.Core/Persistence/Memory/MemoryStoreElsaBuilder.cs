using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Memory
{
    public class MemoryStoreElsaBuilder : ElsaBuilder
    {
        public MemoryStoreElsaBuilder(IServiceCollection services) : base(services)
        {
        }
    }
}