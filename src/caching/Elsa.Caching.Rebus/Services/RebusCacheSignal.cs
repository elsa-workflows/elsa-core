using System.Threading.Tasks;
using Elsa.Caching.Rebus.Messages;
using Elsa.Services;
using Microsoft.Extensions.Primitives;

namespace Elsa.Caching.Rebus.Services
{
    public class RebusCacheSignal : ICacheSignal
    {
        private readonly ICacheSignal _cacheSignal;
        private readonly IEventPublisher _eventPublisher;

        public RebusCacheSignal(ICacheSignal cacheSignal, IEventPublisher eventPublisher)
        {
            _cacheSignal = cacheSignal;
            _eventPublisher = eventPublisher;
        }

        public IChangeToken GetToken(string key) => _cacheSignal.GetToken(key);

        public void TriggerToken(string key) => _cacheSignal.TriggerToken(key);

        public async ValueTask TriggerTokenAsync(string key)
        {
            await _cacheSignal.TriggerTokenAsync(key);
            await _eventPublisher.PublishAsync(new TriggerCacheSignal(key));
        }
    }
}