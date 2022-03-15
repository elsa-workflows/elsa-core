
using Elsa.Activities.ExecuteSqlServerQuery.Activities;
using Elsa.Activities.ExecuteSqlServerQuery.Factory;
using Elsa.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.ExecuteSqlServerQuery.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddSqlServerActivities(this ElsaOptionsBuilder elsa)
        {
            elsa.Services
                .AddSingleton<ISqlClientFactory, SqlClientFactory>();

            return elsa.AddActivity<Activities.ExecuteSqlServerQuery>();
        }
    }
}
