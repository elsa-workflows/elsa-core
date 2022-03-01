using Elsa.Abstractions.Multitenancy;
using Elsa.Webhooks.Models;
using Elsa.Webhooks.Persistence.MongoDb.Data;
using MongoDB.Driver;

namespace Elsa.Webhooks.Persistence.MongoDb.Services
{
    public class ElsaMongoDbContextProvider
    {
        private readonly ITenantProvider _tenantProvider;
        private readonly ElsaMongoDbContext _context;

        public ElsaMongoDbContextProvider(ElsaMongoDbContext context, ITenantProvider tenantProvider)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public IMongoCollection<WebhookDefinition> WebhookDefinitions => GetMongoDatabase().GetCollection<WebhookDefinition>(CollectionNames.WebhookDefinitions);

        private IMongoDatabase GetMongoDatabase()
        {
            var tenant = _tenantProvider.GetCurrentTenantAsync().GetAwaiter().GetResult();
            var connectionString = tenant.GetDatabaseConnectionString();

            return _context.GetDatabase(connectionString);
        }
    }
}
