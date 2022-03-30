using Elsa.Activities.Sql.Factory;
using Elsa.Activities.Sql.Services;
using Elsa.Options;
using Elsa.Secrets.Manager;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Sql.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddSqlServerActivities(this ElsaOptionsBuilder elsa)
        {
            elsa.Services
                .AddSingleton<ISqlClientFactory, SqlClientFactory>()
                .AddScoped<ISecretsProvider, SecretsProvider>()
                .AddScoped<ISecretsManager, SecretsManager>();
            elsa.AddActivity<Activities.ExecuteSqlQuery>();
            elsa.AddActivity<Activities.ExecuteSqlCommand>();

            return elsa;
        }
    }
}
