using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Multitenancy;
using Elsa.Services;
using Elsa.Webhooks.Models;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Elsa.Webhooks.Persistence.MongoDb.Services
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
                using var scope = _scopeFactory.CreateScopeForTenant(tenant);

                var dbContextProvider = scope.ServiceProvider.GetRequiredService<ElsaMongoDbContextProvider>();

                await CreateWebhookDefinitionsIndexes(dbContextProvider, cancellationToken);
            }
        }

        private async Task CreateWebhookDefinitionsIndexes(ElsaMongoDbContextProvider dbContextProvider, CancellationToken cancellationToken)
        {
            var builder = Builders<WebhookDefinition>.IndexKeys;
            var tenantKeysDefinition = builder.Ascending(x => x.TenantId);
            var nameKeysDefinition = builder.Ascending(x => x.Name);
            var pathKeysDefinition = builder.Ascending(x => x.Path);
            var payloadKeysDefinition = builder.Ascending(x => x.PayloadTypeName);
            await CreateIndexesAsync(dbContextProvider.WebhookDefinitions, cancellationToken, tenantKeysDefinition, nameKeysDefinition, pathKeysDefinition, payloadKeysDefinition);
        }

        private async Task CreateIndexesAsync<T>(IMongoCollection<T> collection, CancellationToken cancellationToken, params IndexKeysDefinition<T>[] definitions)
        {
            var models = definitions.Select(x => new CreateIndexModel<T>(x));
            await collection.Indexes.CreateManyAsync(models, cancellationToken);
        }
    }
}