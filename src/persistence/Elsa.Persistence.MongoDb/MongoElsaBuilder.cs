using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.MongoDb.Extensions
{
    public class MongoElsaBuilder : ElsaBuilder
    {
        public MongoElsaBuilder(IServiceCollection services) : base(services)
        {
        }
    }
}