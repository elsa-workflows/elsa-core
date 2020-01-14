using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.MongoDb
{
    public class MongoElsaOptions : ElsaOptions
    {
        public MongoElsaOptions(IServiceCollection services) : base(services)
        {
        }
    }
}