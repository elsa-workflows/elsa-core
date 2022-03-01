using Elsa.Abstractions.Multitenancy;
using Elsa.WorkflowSettings.Models;
using Elsa.WorkflowSettings.Persistence.MongoDb.Data;
using MongoDB.Driver;

namespace Elsa.WorkflowSettings.Persistence.MongoDb.Services
{
    public class ElsaMongoDbContextProvider
    {
        private readonly ElsaMongoDbContext _context;
        private readonly ITenantProvider _tenantProvider;

        public ElsaMongoDbContextProvider(ElsaMongoDbContext context, ITenantProvider tenantProvider)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public IMongoCollection<WorkflowSetting> WorkflowSettings => GetMongoDatabase().GetCollection<WorkflowSetting>(CollectionNames.WorkflowSettings);

        private IMongoDatabase GetMongoDatabase()
        {
            var tenant = _tenantProvider.GetCurrentTenantAsync().GetAwaiter().GetResult();
            var connectionString = tenant.GetDatabaseConnectionString();

            return _context.GetDatabase(connectionString);
        }
    }
}
