using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.MongoDb.Extensions
{
    public class MongoServiceConfiguration : ServiceConfiguration
    {
        public MongoServiceConfiguration(IServiceCollection services) : base(services)
        {
        }
    }
}