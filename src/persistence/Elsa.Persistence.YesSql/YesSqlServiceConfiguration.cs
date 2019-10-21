using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.YesSql
{
    public class YesSqlServiceConfiguration : ServiceConfiguration
    {
        public YesSqlServiceConfiguration(IServiceCollection services) : base(services)
        {
        }
    }
}