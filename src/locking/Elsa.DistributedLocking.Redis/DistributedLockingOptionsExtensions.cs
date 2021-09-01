using Elsa.Options;
using Medallion.Threading.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Elsa
{
    public static class DistributedLockingOptionsExtensions
    {
        public static DistributedLockingOptionsBuilder UseRedisLockProvider(this DistributedLockingOptionsBuilder options, string connectionString)
        {
            options.UseProviderFactory(sp =>
            {
                var connectionMultiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
                var database = connectionMultiplexer.GetDatabase();
                return name => new RedisDistributedLock(name, database);
            });
            return options;
        }
    }
}