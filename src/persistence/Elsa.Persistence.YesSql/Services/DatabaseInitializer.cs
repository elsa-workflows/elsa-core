using System.Threading;
using System.Threading.Tasks;

using Elsa.Data;
using Elsa.Services;

using YesSql;

namespace Elsa.Persistence.YesSql.Services
{
    public class DatabaseInitializer : IStartupTask
    {
        private readonly IStore _store;
        private readonly ISession _session;

        public DatabaseInitializer(IStore store, ISession session)
        {
            _store = store;
            _session = session;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await _store.InitializeCollectionAsync(CollectionNames.WorkflowDefinitions);
            await _store.InitializeCollectionAsync(CollectionNames.WorkflowInstances);
            await _store.InitializeCollectionAsync(CollectionNames.SuspendedWorkflows);
        }
    }
}
