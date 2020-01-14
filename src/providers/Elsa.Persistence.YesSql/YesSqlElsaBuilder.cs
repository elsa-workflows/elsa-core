using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.YesSql
{
    public class YesSqlElsaBuilder : ElsaBuilder
    {
        public YesSqlElsaBuilder(IServiceCollection services) : base(services)
        {
        }
    }
}