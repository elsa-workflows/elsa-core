using System.Threading;
using System.Threading.Tasks;

using Elsa.Models;
using Elsa.Services;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Services
{
    public class DatabaseInitializer : IStartupTask
    {
        private readonly ElsaMongoDbClient _workflowEngineMongoDbClient;

        public DatabaseInitializer(ElsaMongoDbClient workflowEngineMongoDbClient)
        {
            _workflowEngineMongoDbClient = workflowEngineMongoDbClient;
        }

        /// <summary>
        /// Create Indexies
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await CreateWorkflowInstancesIndexies(cancellationToken);
            await CreateWorkflowDefinitionsIndexies(cancellationToken);
        }
       

        private async Task CreateWorkflowInstancesIndexies(CancellationToken cancellationToken)
        {
            var indexKeysDefinition = Builders<WorkflowInstance>.IndexKeys.Ascending(x => x.WorkflowInstanceId);
            await _workflowEngineMongoDbClient.WorkflowInstances.Indexes.CreateOneAsync(new CreateIndexModel<WorkflowInstance>(indexKeysDefinition),cancellationToken: cancellationToken);
        }

        private async Task CreateWorkflowDefinitionsIndexies(CancellationToken cancellationToken)
        {
            var indexKeysDefinition = Builders<WorkflowDefinition>.IndexKeys.Ascending(x => x.WorkflowDefinitionId).Descending(x => x.Version);
            await _workflowEngineMongoDbClient.WorkflowDefinitions.Indexes.CreateOneAsync(new CreateIndexModel<WorkflowDefinition>(indexKeysDefinition), cancellationToken: cancellationToken);
        }

    }
}
