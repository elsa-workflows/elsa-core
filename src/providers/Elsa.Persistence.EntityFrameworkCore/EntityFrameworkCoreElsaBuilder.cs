using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore
{
    public class EntityFrameworkCoreElsaBuilder : ElsaBuilder
    {
        public EntityFrameworkCoreElsaBuilder(IServiceCollection services) : base(services)
        {
        }
    }
}