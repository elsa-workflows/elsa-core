using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.MultiTenancy;
using Elsa.Services;
using Elsa.WorkflowSettings.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Elsa.WorkflowSettings.Persistence.MongoDb.Services
{
    public class DatabaseInitializer : IStartupTask
    {
        private readonly ITenantStore _tenantStore;
        private readonly IServiceScopeFactory _scopeFactory;

        public DatabaseInitializer(IServiceScopeFactory scopeFactory, ITenantStore tenantStore)
        {
            _scopeFactory = scopeFactory;
            _tenantStore = tenantStore;
        }

        public int Order => 0;

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            foreach (var tenant in _tenantStore.GetTenants())
            {
                var scope = _scopeFactory.CreateScopeForTenant(tenant);

                await CreateWorkflowSettingsIndexes(cancellationToken, scope);
            }
        }

        private async Task CreateWorkflowSettingsIndexes(CancellationToken cancellationToken, IServiceScope serviceScope)
        {
            using var scope = serviceScope;

            var mongoContext = scope.ServiceProvider.GetRequiredService<ElsaMongoDbContextProvider>();

            var builder = Builders<WorkflowSetting>.IndexKeys;
            var workflowBlueprintIdKeysDefinition = builder.Ascending(x => x.WorkflowBlueprintId);
            var keyKeysDefinition = builder.Ascending(x => x.Key);
            var valueKeysDefinition = builder.Ascending(x => x.Value);
            await CreateIndexesAsync(mongoContext.WorkflowSettings, cancellationToken, workflowBlueprintIdKeysDefinition, keyKeysDefinition, valueKeysDefinition);
        }

        private async Task CreateIndexesAsync<T>(IMongoCollection<T> collection, CancellationToken cancellationToken, params IndexKeysDefinition<T>[] definitions)
        {
            var models = definitions.Select(x => new CreateIndexModel<T>(x));
            await collection.Indexes.CreateManyAsync(models, cancellationToken);
        }
    }
}