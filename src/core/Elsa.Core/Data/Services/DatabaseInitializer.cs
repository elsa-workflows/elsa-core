using System.Threading;
using System.Threading.Tasks;
using Elsa.Runtime;
using Elsa.Services;
using YesSql;

namespace Elsa.Data.Services
{
    public class DatabaseInitializer : IStartupTask
    {
        private readonly IStore _store;

        public DatabaseInitializer(IStore store)
        {
            _store = store;
        }
        
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await _store.InitializeCollectionAsync(CollectionNames.WorkflowDefinitions);
            await _store.InitializeCollectionAsync(CollectionNames.WorkflowInstances);
        }
    }
}