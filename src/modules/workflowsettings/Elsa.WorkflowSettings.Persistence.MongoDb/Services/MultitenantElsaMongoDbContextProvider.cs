using Elsa.Abstractions.MultiTenancy;
using Elsa.WorkflowSettings.Models;
using Elsa.WorkflowSettings.Persistence.MongoDb.Data;
using MongoDB.Driver;

namespace Elsa.WorkflowSettings.Persistence.MongoDb.Services
{
    public class MultitenantElsaMongoDbContextProvider
    {
        private readonly MultitenantElsaMongoDbContext _context;
        private readonly ITenantProvider _tenantProvider;

        public MultitenantElsaMongoDbContextProvider(MultitenantElsaMongoDbContext context, ITenantProvider tenantProvider)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public IMongoCollection<WorkflowSetting> WorkflowSettings => GetMongoDatabase().GetCollection<WorkflowSetting>(CollectionNames.WorkflowSettings);

        private IMongoDatabase GetMongoDatabase()
        {
            var connectionString = _tenantProvider.GetCurrentTenant().ConnectionString;

            return _context.GetDatabase(connectionString);
        }
    }
}
