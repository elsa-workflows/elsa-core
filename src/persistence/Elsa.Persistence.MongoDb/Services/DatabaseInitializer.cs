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
        private readonly ElsaMongoDbClient _mongoClient;

        public DatabaseInitializer(ElsaMongoDbClient mongoClient)
        {
            _mongoClient = mongoClient;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await CreateWorkflowInstancesIndexes(cancellationToken);
            await CreateWorkflowDefinitionsIndexes(cancellationToken);
            await CreateSuspendedWorkflowsIndexes(cancellationToken);
        }

        private async Task CreateWorkflowInstancesIndexes(CancellationToken cancellationToken)
        {
            var tenantKeysDefinition = Builders<WorkflowInstance>.IndexKeys.Ascending(x => x.TenantId);
            var versionKeysDefinition = Builders<WorkflowInstance>.IndexKeys.Ascending(x => x.Version);
            var workflowStatusKeysDefinition = Builders<WorkflowInstance>.IndexKeys.Ascending(x => x.WorkflowStatus);

            await CreateIndexesAsync(_mongoClient.WorkflowInstances, cancellationToken, tenantKeysDefinition, versionKeysDefinition, workflowStatusKeysDefinition);
        }

        private async Task CreateWorkflowDefinitionsIndexes(CancellationToken cancellationToken)
        {
            var tenantKeysDefinition = Builders<WorkflowDefinition>.IndexKeys.Ascending(x => x.TenantId);
            var definitionVersionIdKeysDefinition = Builders<WorkflowDefinition>.IndexKeys.Ascending(x => x.DefinitionVersionId);
            var versionKeysDefinition = Builders<WorkflowDefinition>.IndexKeys.Ascending(x => x.Version);
            var nameKeysDefinition = Builders<WorkflowDefinition>.IndexKeys.Ascending(x => x.Name);

            await CreateIndexesAsync(_mongoClient.WorkflowDefinitions, cancellationToken, tenantKeysDefinition, definitionVersionIdKeysDefinition, versionKeysDefinition, nameKeysDefinition);
        }

        private async Task CreateSuspendedWorkflowsIndexes(CancellationToken cancellationToken)
        {
            var tenantKeysDefinition = Builders<SuspendedWorkflowBlockingActivity>.IndexKeys.Ascending(x => x.TenantId);
            var versionKeysDefinition = Builders<SuspendedWorkflowBlockingActivity>.IndexKeys.Ascending(x => x.Version);
            var activityTypeKeysDefinition = Builders<SuspendedWorkflowBlockingActivity>.IndexKeys.Ascending(x => x.ActivityType);

            await CreateIndexesAsync(_mongoClient.SuspendedWorkflows, cancellationToken, tenantKeysDefinition, versionKeysDefinition, activityTypeKeysDefinition);
        }

        private async Task CreateIndexesAsync<T>(IMongoCollection<T> collection, CancellationToken cancellationToken, params IndexKeysDefinition<T>[] definitions)
        {
            var models = definitions.Select(x => new CreateIndexModel<T>(x));
            await collection.Indexes.CreateManyAsync(models, cancellationToken);
        }
    }
}