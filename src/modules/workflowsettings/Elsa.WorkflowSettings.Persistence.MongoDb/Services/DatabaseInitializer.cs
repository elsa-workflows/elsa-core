using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.WorkflowSettings.Models;
using MongoDB.Driver;

namespace Elsa.WorkflowSettings.Persistence.MongoDb.Services
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
            await CreateWorkflowSettingsIndexes(cancellationToken);
        }

        private async Task CreateWorkflowSettingsIndexes(CancellationToken cancellationToken)
        {
            var builder = Builders<WorkflowSetting>.IndexKeys;
            var workflowBlueprintIdKeysDefinition = builder.Ascending(x => x.WorkflowBlueprintId);
            var keyKeysDefinition = builder.Ascending(x => x.Key);
            var valueKeysDefinition = builder.Ascending(x => x.Value);
            await CreateIndexesAsync(_mongoContext.WorkflowSettings, cancellationToken, workflowBlueprintIdKeysDefinition, keyKeysDefinition, valueKeysDefinition);
        }

        private async Task CreateIndexesAsync<T>(IMongoCollection<T> collection, CancellationToken cancellationToken, params IndexKeysDefinition<T>[] definitions)
        {
            var models = definitions.Select(x => new CreateIndexModel<T>(x));
            await collection.Indexes.CreateManyAsync(models, cancellationToken);
        }
    }
}