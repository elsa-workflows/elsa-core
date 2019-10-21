using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Memory
{
    public class MemoryStoreServiceConfiguration : ServiceConfiguration
    {
        public MemoryStoreServiceConfiguration(IServiceCollection services) : base(services)
        {
        }
    }
}