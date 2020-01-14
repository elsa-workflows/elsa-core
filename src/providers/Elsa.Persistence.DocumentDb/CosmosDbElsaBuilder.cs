using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.DocumentDb
{
    public class CosmosDbElsaBuilder : ElsaBuilder
    {
        public CosmosDbElsaBuilder(IServiceCollection services) : base(services)
        {
        }
    }
}