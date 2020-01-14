using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.YesSql
{
    public class YesSqlElsaOptions : ElsaOptions
    {
        public YesSqlElsaOptions(IServiceCollection services) : base(services)
        {
        }
    }
}