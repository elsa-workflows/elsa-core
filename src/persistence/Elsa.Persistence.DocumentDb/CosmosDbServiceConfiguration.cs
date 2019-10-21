using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.DocumentDb
{
    public class CosmosDbServiceConfiguration : ServiceConfiguration
    {
        public CosmosDbServiceConfiguration(IServiceCollection services) : base(services)
        {
        }
    }
}