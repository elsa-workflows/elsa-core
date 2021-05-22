using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Elsa.Extensions
{
    public static class RedisServiceCollectionExtensions
    {
        public static IServiceCollection AddRedis(this IServiceCollection services, string connectionString)
        {
            return services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(connectionString));
        }
    }
}