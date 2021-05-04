using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Models;
using Elsa.Services;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Services
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
            await CreateWorkflowInstancesIndexes(cancellationToken);
            await CreateWorkflowDefinitionsIndexes(cancellationToken);
            await CreateWorkflowExecutionLogIndexes(cancellationToken);
            await CreateBookmarkIndexes(cancellationToken);
        }

        private async Task CreateWorkflowInstancesIndexes(CancellationToken cancellationToken)
        {
            var tenantKeysDefinition = Builders<WorkflowInstance>.IndexKeys.Ascending(x => x.TenantId);
            var versionKeysDefinition = Builders<WorkflowInstance>.IndexKeys.Ascending(x => x.Version);
            var workflowStatusKeysDefinition = Builders<WorkflowInstance>.IndexKeys.Ascending(x => x.WorkflowStatus);
            var workflowNameKeysDefinition = Builders<WorkflowInstance>.IndexKeys.Ascending(x => x.Name);

            await CreateIndexesAsync(_mongoContext.WorkflowInstances, cancellationToken, tenantKeysDefinition, versionKeysDefinition, workflowStatusKeysDefinition, workflowNameKeysDefinition);
        }

        private async Task CreateWorkflowDefinitionsIndexes(CancellationToken cancellationToken)
        {
            var tenantKeysDefinition = Builders<WorkflowDefinition>.IndexKeys.Ascending(x => x.TenantId);
            var definitionIdKeysDefinition = Builders<WorkflowDefinition>.IndexKeys.Ascending(x => x.DefinitionId);
            var versionKeysDefinition = Builders<WorkflowDefinition>.IndexKeys.Ascending(x => x.Version);
            var nameKeysDefinition = Builders<WorkflowDefinition>.IndexKeys.Ascending(x => x.Name);
            var tagKeysDefinition = Builders<WorkflowDefinition>.IndexKeys.Ascending(x => x.Tag);

            await CreateIndexesAsync(_mongoContext.WorkflowDefinitions, cancellationToken, tenantKeysDefinition, definitionIdKeysDefinition, versionKeysDefinition, nameKeysDefinition, tagKeysDefinition);
        }

        private async Task CreateWorkflowExecutionLogIndexes(CancellationToken cancellationToken)
        {
            var tenantKeysDefinition = Builders<WorkflowExecutionLogRecord>.IndexKeys.Ascending(x => x.TenantId);
            var workflowInstanceIdKeysDefinition = Builders<WorkflowExecutionLogRecord>.IndexKeys.Ascending(x => x.WorkflowInstanceId);
            var timestampKeysDefinition = Builders<WorkflowExecutionLogRecord>.IndexKeys.Ascending(x => x.Timestamp);

            await CreateIndexesAsync(_mongoContext.WorkflowExecutionLog, cancellationToken, tenantKeysDefinition, workflowInstanceIdKeysDefinition, timestampKeysDefinition);
        }

        private async Task CreateBookmarkIndexes(CancellationToken cancellationToken)
        {
            var tenantKeysDefinition = Builders<Bookmark>.IndexKeys.Ascending(x => x.TenantId);
            var activityTypeKeysDefinition = Builders<Bookmark>.IndexKeys.Ascending(x => x.ActivityType);
            var hashKeysDefinition = Builders<Bookmark>.IndexKeys.Ascending(x => x.Hash);
            var workflowInstanceIdKeysDefinition = Builders<Bookmark>.IndexKeys.Ascending(x => x.WorkflowInstanceId);

            await CreateIndexesAsync(_mongoContext.Bookmarks, cancellationToken, tenantKeysDefinition, activityTypeKeysDefinition, hashKeysDefinition, workflowInstanceIdKeysDefinition);
        }

        private async Task CreateIndexesAsync<T>(IMongoCollection<T> collection, CancellationToken cancellationToken, params IndexKeysDefinition<T>[] definitions)
        {
            var models = definitions.Select(x => new CreateIndexModel<T>(x));
            await collection.Indexes.CreateManyAsync(models, cancellationToken);
        }
    }
}