using Elsa.Data;
using Elsa.Models;

using Microsoft.Extensions.Options;

using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb
{
    public class ElsaMongoDbClient
    {
        private readonly IMongoDatabase _mongoDatabase;

        public ElsaMongoDbClient(IOptions<ElsaMongoDbOptions> options)
        {
            var mongoClient = new MongoClient(options.Value.ConnectionString);
            _mongoDatabase = mongoClient.GetDatabase(options.Value.Db);
        }

        public IMongoCollection<WorkflowDefinition> WorkflowDefinitions => _mongoDatabase.GetCollection<WorkflowDefinition>(CollectionNames.WorkflowDefinitions);
        public IMongoCollection<WorkflowInstance> WorkflowInstances => _mongoDatabase.GetCollection<WorkflowInstance>(CollectionNames.WorkflowInstances);
    }
}
