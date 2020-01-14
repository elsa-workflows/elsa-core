using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.DistributedLocking.SqlServer
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions AddSqlServerLockProvider(this ElsaOptions options, string connectionString)
        {
            return options.UseDistributedLockProvider(sp => new SqlLockProvider(connectionString, sp.GetRequiredService<ILogger<SqlLockProvider>>()));
        }
    }
}