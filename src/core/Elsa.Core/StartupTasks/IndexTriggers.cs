using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.StartupTasks
{
    public class IndexTriggers : IStartupTask
    {
        private readonly ITriggerIndexer _triggerIndexer;

        public IndexTriggers(ITriggerIndexer triggerIndexer)
        {
            _triggerIndexer = triggerIndexer;
        }
        
        public int Order => 1000;
        
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await _triggerIndexer.IndexTriggersAsync(cancellationToken);
        }
    }
}