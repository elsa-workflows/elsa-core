using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.YesSql.Data;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using YesSql;

namespace Elsa.Persistence.YesSql.Services
{
    public class DatabaseInitializer : IStartupTask
    {
        protected readonly IServiceScopeFactory _scopeFactory;

        public DatabaseInitializer(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public int Order => 0;

        public virtual async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await ExecuteInternalAsync();
        }

        protected async Task ExecuteInternalAsync(IServiceScope? serviceScope = default)
        {
            using var scope = serviceScope ?? _scopeFactory.CreateScope();

            var store = scope.ServiceProvider.GetRequiredService<IStore>();

            await store.InitializeCollectionAsync(CollectionNames.WorkflowDefinitions);
            await store.InitializeCollectionAsync(CollectionNames.WorkflowInstances);
            await store.InitializeCollectionAsync(CollectionNames.WorkflowExecutionLog);
            await store.InitializeCollectionAsync(CollectionNames.Bookmarks);
        }
    }
}
