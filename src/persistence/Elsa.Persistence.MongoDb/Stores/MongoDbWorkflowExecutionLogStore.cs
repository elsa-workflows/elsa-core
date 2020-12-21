using Elsa.Models;
using Elsa.Services;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Stores
{
    public class MongoDbWorkflowExecutionLogStore : MongoDbStore<WorkflowExecutionLogRecord>, IWorkflowExecutionLogStore
    {
        public MongoDbWorkflowExecutionLogStore(IMongoCollection<WorkflowExecutionLogRecord> collection, IIdGenerator idGenerator) : base(collection, idGenerator)
        {
        }
    }
}