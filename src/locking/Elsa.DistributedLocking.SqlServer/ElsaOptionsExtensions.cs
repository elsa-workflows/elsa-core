using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaConfigurationsOptions UseSqlServerLockProvider(this ElsaConfigurationsOptions options, string connectionString)
        {
            return options.UseDistributedLockProvider(sp => new SqlLockProvider(connectionString, sp.GetRequiredService<ILogger<SqlLockProvider>>()));
        }
    }
}