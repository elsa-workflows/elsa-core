using Elsa.Activities.Sql.Factory;
using Elsa.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Sql.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddSqlServerActivities(this ElsaOptionsBuilder elsa)
        {
            return AddSqlServerActivities<SqlClientFactory>(elsa);
        }

        public static ElsaOptionsBuilder AddSqlServerActivities<T>(this ElsaOptionsBuilder elsa) where T : class, ISqlClientFactory
        {
            elsa.Services.AddSingleton<ISqlClientFactory, T>();
            elsa.AddActivity<Activities.ExecuteSqlQuery>();
            elsa.AddActivity<Activities.ExecuteSqlCommand>();

            elsa.AddSqlScripting();

            return elsa;
        }
    }
}
