using System.Threading.Tasks;
using Elsa.Caching.Rebus.Messages;
using Rebus.Handlers;

namespace Elsa.Caching.Rebus.Consumers
{
    public class TriggerCacheSignalConsumer : IHandleMessages<TriggerCacheSignal>
    {
        private readonly ICacheSignal _cacheSignal;

        public TriggerCacheSignalConsumer(ICacheSignal cacheSignal) => _cacheSignal = cacheSignal;

        public Task Handle(TriggerCacheSignal message)
        {
            _cacheSignal.TriggerToken(message.Key);
            return Task.CompletedTask;
        }
    }
}