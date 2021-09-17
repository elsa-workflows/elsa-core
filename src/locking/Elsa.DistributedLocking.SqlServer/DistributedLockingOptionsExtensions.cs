using Elsa.Options;
using Medallion.Threading.SqlServer;

namespace Elsa
{
    public static class DistributedLockingOptionsExtensions
    {
        public static DistributedLockingOptionsBuilder UseSqlServerLockProvider(this DistributedLockingOptionsBuilder options, string connectionString)
        {
            options.UseProviderFactory(sp => name => new SqlDistributedLock(name, connectionString));
            return options;
        }
    }
}