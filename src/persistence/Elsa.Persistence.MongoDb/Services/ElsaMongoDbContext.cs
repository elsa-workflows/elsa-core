using Elsa.Models;
using Elsa.Persistence.MongoDb.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Services
{
    public class ElsaMongoDbContext
    {
        private readonly IMongoDatabase _mongoDatabase;

        public ElsaMongoDbContext(IOptions<ElsaMongoDbOptions> options)
        {
            var mongoClient = new MongoClient(options.Value.ConnectionString);
            _mongoDatabase = mongoClient.GetDatabase(options.Value.Db);
        }

        public IMongoCollection<WorkflowDefinition> WorkflowDefinitions => _mongoDatabase.GetCollection<WorkflowDefinition>(CollectionNames.WorkflowDefinitions);
        public IMongoCollection<WorkflowInstance> WorkflowInstances => _mongoDatabase.GetCollection<WorkflowInstance>(CollectionNames.WorkflowInstances);
        public IMongoCollection<WorkflowExecutionLogRecord> WorkflowExecutionLog => _mongoDatabase.GetCollection<WorkflowExecutionLogRecord>(CollectionNames.WorkflowExecutionLog);
        public IMongoCollection<Bookmark> Bookmarks => _mongoDatabase.GetCollection<Bookmark>(CollectionNames.Bookmarks);
    }
}
