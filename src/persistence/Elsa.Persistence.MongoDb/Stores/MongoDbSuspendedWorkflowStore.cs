using Elsa.Models;
using Elsa.Services;
using MongoDB.Driver;

namespace Elsa.Persistence.MongoDb.Stores
{
    public class MongoDbSuspendedWorkflowStore : MongoDbStore<SuspendedWorkflowBlockingActivity>, ISuspendedWorkflowStore
    {
        public MongoDbSuspendedWorkflowStore(IMongoCollection<SuspendedWorkflowBlockingActivity> collection, IIdGenerator idGenerator) : base(collection, idGenerator)
        {
        }
    }
}