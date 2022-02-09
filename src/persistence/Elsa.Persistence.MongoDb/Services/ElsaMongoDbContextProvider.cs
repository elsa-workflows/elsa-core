using Elsa.Abstractions.MultiTenancy;
using Elsa.Models;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Services
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

        public IMongoCollection<WorkflowDefinition> WorkflowDefinitions => GetDatabase().GetCollection<WorkflowDefinition>(CollectionNames.WorkflowDefinitions);
        public IMongoCollection<WorkflowInstance> WorkflowInstances => GetDatabase().GetCollection<WorkflowInstance>(CollectionNames.WorkflowInstances);
        public IMongoCollection<WorkflowExecutionLogRecord> WorkflowExecutionLog => GetDatabase().GetCollection<WorkflowExecutionLogRecord>(CollectionNames.WorkflowExecutionLog);
        public IMongoCollection<Bookmark> Bookmarks => GetDatabase().GetCollection<Bookmark>(CollectionNames.Bookmarks);
        public IMongoCollection<Trigger> Triggers => GetDatabase().GetCollection<Trigger>(CollectionNames.Triggers);

        private IMongoDatabase GetDatabase()
        {
            var connectionString = _tenantProvider.GetCurrentTenant().ConnectionString;

            return _context.GetDatabase(connectionString);
        }
    }
}
