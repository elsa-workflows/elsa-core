using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Elsa.Services.Triggers
{
    public class TriggerStore : ITriggerStore
    {
        private const string Key = "WorkflowTriggerStore"; 
        private readonly IMemoryCache _memoryCache;

        public TriggerStore(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }
        
        public ValueTask StoreAsync(IEnumerable<WorkflowTrigger> triggers, CancellationToken cancellationToken = default)
        {
            _memoryCache.Set(Key, triggers.ToList());
            return new ValueTask();
        }

        public ValueTask<IEnumerable<WorkflowTrigger>> GetAsync(CancellationToken cancellationToken = default)
        {
            var result = _memoryCache.Get<IEnumerable<WorkflowTrigger>>(Key) ?? Enumerable.Empty<WorkflowTrigger>();
            return new ValueTask<IEnumerable<WorkflowTrigger>>(result);
        }
    }
}