using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Webhooks.Persistence.YesSql.Data;
using YesSql;

namespace Elsa.Webhooks.Persistence.YesSql.Services
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
            await _store.InitializeCollectionAsync(CollectionNames.WebhookDefinitions);
        }
    }
}
