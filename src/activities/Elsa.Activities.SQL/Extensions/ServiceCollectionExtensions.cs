using Elsa.Activities.Sql.Factory;
using Elsa.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Sql.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddSqlServerActivities(this ElsaOptionsBuilder elsa)
        {
            elsa.Services
                .AddSingleton<ISqlClientFactory, SqlClientFactory>();
            elsa.AddActivity<Activities.ExecuteSqlQuery>();
            elsa.AddActivity<Activities.ExecuteSqlCommand>();

            return elsa;
        }
    }
}
