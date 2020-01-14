using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.DistributedLocking.SqlServer
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaBuilder AddSqlServerLockProvider(this ElsaBuilder configuration, string connectionString)
        {
            var services = configuration.Services;

            services.AddSingleton<IDistributedLockProvider>(sp => new SqlLockProvider(connectionString, sp.GetRequiredService<ILogger<SqlLockProvider>>()));
            return configuration;
        }
    }
}