using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.WorkflowSettings.Persistence.YesSql.Data;
using YesSql;

namespace Elsa.WorkflowSettings.Persistence.YesSql.Services
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
            await _store.InitializeCollectionAsync(CollectionNames.WorkflowSettings);
        }
    }
}
