using System.Threading;
using System.Threading.Tasks;
using Elsa.Caching;
using Elsa.Services;

namespace Elsa.StartupTasks
{
    public class SubscribeToRedisCacheSignals : IStartupTask
    {
        private readonly RedisBus _redisBus;
        private readonly ICacheSignal _cacheSignal;

        public SubscribeToRedisCacheSignals(RedisBus redisBus, ICacheSignal cacheSignal)
        {
            _redisBus = redisBus;
            _cacheSignal = cacheSignal;
        }
        
        public int Order => 0;
        public async Task ExecuteAsync(CancellationToken cancellationToken = default) => await _redisBus.SubscribeAsync(nameof(CacheSignal), (channel, message) => _cacheSignal.TriggerToken(message));
    }
}