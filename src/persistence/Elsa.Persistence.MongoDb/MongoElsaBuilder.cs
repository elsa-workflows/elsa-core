using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.MongoDb
{
    public class MongoElsaBuilder : ElsaBuilder
    {
        public MongoElsaBuilder(IServiceCollection services) : base(services)
        {
        }
    }
}