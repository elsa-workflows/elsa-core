using Medallion.Threading.SqlServer;

namespace Elsa
{
    public static class DistributedLockingOptionsExtensions
    {
        public static DistributedLockingOptions UseSqlServerLockProvider(this DistributedLockingOptions options, string connectionString)
        {
            options.DistributedLockProviderFactory = sp => name => new SqlDistributedLock(name, connectionString);
            return options;
        }
    }
}