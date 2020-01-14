using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Memory
{
    public class MemoryStoreElsaOptions : ElsaOptions
    {
        public MemoryStoreElsaOptions(IServiceCollection services) : base(services)
        {
        }
    }
}