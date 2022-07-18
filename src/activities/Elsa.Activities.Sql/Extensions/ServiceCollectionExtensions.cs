using Elsa.Activities.Sql.Factory;
using Elsa.Options;
using Elsa.Secrets.Manager;
using Elsa.Secrets.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Sql.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddSqlServerActivities(this ElsaOptionsBuilder elsa)
        {
            elsa.Services
                .AddSingleton<ISqlClientFactory, SqlClientFactory>()
                .AddScoped<ISecretsManager, SecretsManager>()
                .AddScoped<ISecretsProvider, SecretsProvider>();
            elsa.AddActivity<Activities.ExecuteSqlQuery>();
            elsa.AddActivity<Activities.ExecuteSqlCommand>();

            elsa.AddSqlScripting();

            return elsa;
        }
    }
}
