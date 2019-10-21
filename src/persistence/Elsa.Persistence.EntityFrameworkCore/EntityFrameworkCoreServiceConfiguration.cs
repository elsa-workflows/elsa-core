using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore
{
    public class EntityFrameworkCoreServiceConfiguration : ServiceConfiguration
    {
        public EntityFrameworkCoreServiceConfiguration(IServiceCollection services) : base(services)
        {
        }
    }
}