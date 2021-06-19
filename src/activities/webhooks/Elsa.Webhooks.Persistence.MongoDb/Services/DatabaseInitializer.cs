using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Webhooks.Models;
using MongoDB.Driver;

namespace Elsa.Webhooks.Persistence.MongoDb.Services
{
    public class DatabaseInitializer : IStartupTask
    {
        private readonly ElsaMongoDbContext _mongoContext;

        public DatabaseInitializer(ElsaMongoDbContext mongoContext)
        {
            _mongoContext = mongoContext;
        }

        public int Order => 0;

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await CreateWebhookDefinitionsIndexes(cancellationToken);
        }

        private async Task CreateWebhookDefinitionsIndexes(CancellationToken cancellationToken)
        {
            var builder = Builders<WebhookDefinition>.IndexKeys;
            var tenantKeysDefinition = builder.Ascending(x => x.TenantId);
            var nameKeysDefinition = builder.Ascending(x => x.Name);
            var webhookIdDefinition = builder.Ascending(x => x.Id);
            var collection = _mongoContext.WebhookDefinitions;
            await CreateIndexesAsync(_mongoContext.WebhookDefinitions, cancellationToken, tenantKeysDefinition, webhookIdDefinition, nameKeysDefinition);
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<WebhookDefinition>(webhookIdDefinition, new CreateIndexOptions { Unique = true }), cancellationToken: cancellationToken);
        }

        private async Task CreateIndexesAsync<T>(IMongoCollection<T> collection, CancellationToken cancellationToken, params IndexKeysDefinition<T>[] definitions)
        {
            var models = definitions.Select(x => new CreateIndexModel<T>(x));
            await collection.Indexes.CreateManyAsync(models, cancellationToken);
        }
    }
}