using System.Threading.Tasks;
using Elsa.Caching;
using Microsoft.Extensions.Primitives;

namespace Elsa.Services
{
    public class RedisCacheSignal : ICacheSignal
    {
        private readonly ICacheSignal _cacheSignal;
        private readonly RedisBus _redisBus;

        public RedisCacheSignal(ICacheSignal cacheSignal, RedisBus redisBus)
        {
            _cacheSignal = cacheSignal;
            _redisBus = redisBus;
        }

        public IChangeToken GetToken(string key) => _cacheSignal.GetToken(key);
        public void TriggerToken(string key) => _cacheSignal.TriggerToken(key);

        public async ValueTask TriggerTokenAsync(string key)
        {
            await _cacheSignal.TriggerTokenAsync(key);
            await _redisBus.PublishAsync(nameof(CacheSignal), key);
        }
    }
}