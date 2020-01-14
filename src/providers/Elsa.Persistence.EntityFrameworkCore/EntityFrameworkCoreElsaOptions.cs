using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore
{
    public class EntityFrameworkCoreElsaOptions : ElsaOptions
    {
        public EntityFrameworkCoreElsaOptions(IServiceCollection services) : base(services)
        {
        }
    }
}