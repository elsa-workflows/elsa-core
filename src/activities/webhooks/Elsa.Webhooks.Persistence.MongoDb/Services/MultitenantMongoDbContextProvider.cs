using Elsa.Abstractions.MultiTenancy;
using Elsa.Webhooks.Models;
using Elsa.Webhooks.Persistence.MongoDb.Data;
using MongoDB.Driver;

namespace Elsa.Webhooks.Persistence.MongoDb.Services
{
    public class MultitenantElsaMongoDbContextProvider
    {
        private readonly ITenantProvider _tenantProvider;
        private readonly MultitenantElsaMongoDbContext _context;

        public MultitenantElsaMongoDbContextProvider(MultitenantElsaMongoDbContext context, ITenantProvider tenantProvider)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public IMongoCollection<WebhookDefinition> WebhookDefinitions => GetMongoDatabase().GetCollection<WebhookDefinition>(CollectionNames.WebhookDefinitions);

        private IMongoDatabase GetMongoDatabase()
        {
            var connectionString = _tenantProvider.GetCurrentTenant().ConnectionString;

            return _context.GetDatabase(connectionString);
        }
    }
}
