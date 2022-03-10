using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.YesSql.Data;
using Elsa.Services;
using YesSql;

namespace Elsa.Persistence.YesSql.Services
{
    public class DatabaseInitializer : IStartupTask
    {
        private readonly IStore _store;

        public DatabaseInitializer(IStore store)
        {
            _store = store;
        }

        public int Order => 0;

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await _store.InitializeCollectionAsync(CollectionNames.WorkflowDefinitions);
            await _store.InitializeCollectionAsync(CollectionNames.WorkflowInstances);
            await _store.InitializeCollectionAsync(CollectionNames.WorkflowExecutionLog);
            await _store.InitializeCollectionAsync(CollectionNames.Bookmarks);
            await _store.InitializeCollectionAsync(CollectionNames.Triggers);
        }
    }
}
