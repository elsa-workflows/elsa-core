using Elsa.Models;
using Elsa.Services;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Stores
{
    public class MongoDbWorkflowInstanceStore : MongoDbStore<WorkflowInstance>, IWorkflowInstanceStore
    {
        public MongoDbWorkflowInstanceStore(IMongoCollection<WorkflowInstance> collection, IIdGenerator idGenerator) : base(collection, idGenerator)
        {
        }
    }
}